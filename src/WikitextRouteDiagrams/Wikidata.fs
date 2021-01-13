namespace WikitextRouteDiagrams

open System.Web
open System.Text.RegularExpressions

open FSharp.Data

type WikiType =
    | Bahnstrecke
    | Station

/// load Wikidata templates from db and eval ParserFunctions #ifeq and #switch
module Wikidata =

    // SPARQL query for wikipedia articles of german railway routes, LIMIT needs value
    // wdt:P31 wd:Q55488 'ist ein Bahnhof'
    // wdt:P31 wd:Q928830 'ist eine Metrostation'
    // wdt:P17 wd:Q183 'Staat Deutschland'
    let queryStations = """
    SELECT ?BahnhofLabel ?BahnhofCode WHERE {
      SERVICE wikibase:label { bd:serviceParam wikibase:language "[AUTO_LANGUAGE],de". }
      ?Bahnhof wdt:P31 wd:Q55488.
      FILTER NOT EXISTS { ?Bahnhof wdt:P31 wd:Q928830. }
      ?Bahnhof wdt:P296 ?BahnhofCode.
      ?Bahnhof wdt:P17 wd:Q183.
    }
    LIMIT
    """

    // SPARQL query for wikipedia articles of german railway routes, LIMIT needs value
    // wdt:P31 wd:Q728937 'ist eine Eisenbahnstrecke'
    // wdt:P17 wd:Q183 'Staat Deutschland'
    // wdt:P1671 'Streckennummer'
    let queryArticles = """
    SELECT ?railway ?railwayLabel ?article WHERE {
      SERVICE wikibase:label { bd:serviceParam wikibase:language "[AUTO_LANGUAGE],de". }
      ?railway wdt:P31 wd:Q728937;
        wdt:P17 wd:Q183.
      ?article schema:about ?railway;
        schema:isPartOf <https://de.wikipedia.org/>.
    }
    LIMIT
    """

    let private getTitle (url: string) =
        let index = url.LastIndexOf('/')
        if index > 0
        then Some(url.Substring(index + 1, url.Length - index - 1))
        else None

    let getWikipediaArticles (limit: int) =
        let q =
            HttpUtility.UrlEncode(queryArticles + " " + limit.ToString())

        let results =
            HtmlDocument.Load("https://query.wikidata.org/sparql?query=" + q)

        results.Descendants [ "binding" ]
        |> Seq.filter (fun x -> x.HasAttribute("name", "article"))
        |> Seq.map
            ((fun x -> HttpUtility.UrlDecode(HttpUtility.UrlDecode(x.InnerText())))
             >> getTitle)
        |> Seq.choose id

    let getWikipediaStations (limit: int) =
        let q =
            HttpUtility.UrlEncode(queryStations + " " + limit.ToString())

        let results =
            HtmlDocument.Load("https://query.wikidata.org/sparql?query=" + q)

        results.Descendants [ "result" ]
        |> Seq.map
            ((fun r ->
                r.Descendants [ "binding" ]
                |> Seq.map (fun b -> (b.AttributeValue("name"), HttpUtility.UrlDecode(HttpUtility.UrlDecode(b.InnerText()))))
                |> Seq.toList)
             >> (fun r ->
                 match r with
                 | [ ("BahnhofCode", c); ("BahnhofLabel", l) ] -> Some(l, c)
                 | _ -> None))
        |> Seq.choose id

    let private tryFindStartOfTemplates (s: string) (wikiType: WikiType) =
        match wikiType with
        | WikiType.Bahnstrecke -> s.IndexOf("{{BS")
        | WikiType.Station -> s.IndexOf("{{Infobox Bahnhof")

    let private tryFindEndOfTemplates (s: string) (wikiType: WikiType) (startIndex: int) =
        if startIndex < 0 then
            -1
        else
            match wikiType with
            | WikiType.Bahnstrecke ->
                let mutable last = s.IndexOf("{{BS-Trenner}}", startIndex)
                if last < 0 then last <- s.IndexOf("|} |}", startIndex)
                if last < 0 then last <- s.IndexOf("|} ", startIndex)
                last
            | WikiType.Station ->
                let mutable index = startIndex + 2
                let mutable indexOfSep = -1
                let mutable level = 0
                while indexOfSep < 0 && index < s.Length - 1 do
                    if s.[index] = '{' && s.[index + 1] = '{'
                    then level <- level + 1

                    if s.[index] = '}' && s.[index + 1] = '}'
                    then if level > 0 then level <- level - 1 else indexOfSep <- index + 2
                    index <- index + 1
                indexOfSep

    let private getDescendants (node: HtmlNode) name =
        node.Descendants(fun node -> node.HasName name)
        |> Seq.toArray

    let private getDescendantsOfDoc (node: HtmlDocument) name =
        node.Descendants(fun node -> node.HasName name)
        |> Seq.toArray

    let private regexRedirection =
        Regex(@"#(\S+)\s*\[\[([^\]]+)\]\]", RegexOptions.IgnoreCase)

    let private getRedirection text =
        let m = regexRedirection.Match text
        if m.Success
           && m.Groups.Count = 3
           && (m.Groups.[1].Value.ToUpper() = "REDIRECT"
               || m.Groups.[1].Value.ToUpper() = "WEITERLEITUNG") then
            Some m.Groups.[2].Value
        else
            None

    let rec private asyncLoadTemplates (wikiType: WikiType) (wikiTitles: string []) =
        let pages =
            wikiTitles
            |> Seq.reduce (fun text texts -> text + "%0A" + texts)

        let url =
            "https://de.wikipedia.org/wiki/Spezial:Exportieren?pages="
            + pages

        printfn "loading url: %s " url

        async {
            let! response = HtmlDocument.AsyncLoad(url)
            let pages = getDescendantsOfDoc response "page"
            let results = ResizeArray()

            for page in pages do
                let title =
                    match getDescendants page "title" with
                    | [| t |] -> t.InnerText()
                    | _ -> ""

                let texts = getDescendants page "text"
                if texts.Length = 1 && title.Length > 0 then
                    let s = (texts.[0].ToString())
                    let start = tryFindStartOfTemplates s wikiType

                    let last = tryFindEndOfTemplates s wikiType start

                    if start > 0 && last > start then
                        results.Add(title, System.Web.HttpUtility.HtmlDecode(s.Substring(start, last - start)))
                    else
                        match getRedirection s with
                        | Some v ->
                            match! asyncLoadTemplates wikiType [| v |] with
                            | [| (_, v) |] -> results.Add(title, v)
                            | _ -> ()
                        | None -> ()

            return results.ToArray()
        }

    let private parallelThrottle throttle workflows = Async.Parallel(workflows, throttle)

    let private loadTemplatesOfType verbose
                                    (wikiTitles: string [])
                                    (wikiType: WikiType)
                                    (insert: string -> string -> System.Guid)
                                    =
        let chunksize = 10 // titles per request
        let maxParallelRequests = 3 // max parallel requests
        wikiTitles
        |> Array.chunkBySize chunksize
        |> Array.map (fun t ->
            async { return! asyncLoadTemplates wikiType t }
            |> Async.Catch)
        |> parallelThrottle maxParallelRequests
        |> Async.RunSynchronously
        |> Array.map (function
            | Choice1Of2 r -> r
            | Choice2Of2 exn ->
                printfn "%s" exn.Message
                Array.empty)
        |> Array.collect id
        |> Array.iter (fun (title, text) ->
            printfn "loadTemplates.insert %s, %d chars" title text.Length
            if verbose then printfn "text '%s'" text
            insert title text |> ignore)

    let loadTemplatesOfRoutes verbose (wikiTitles: string []) =
        loadTemplatesOfType verbose wikiTitles WikiType.Bahnstrecke DataAccess.WikitextOfRoute.insert

    let loadTemplatesOfStops verbose (wikiTitles: string []) =
        loadTemplatesOfType verbose wikiTitles WikiType.Station DataAccess.WikitextOfStop.insert

    let loadTemplates (title: string) =
        fprintfn stderr "loading: '%s'" title

        match DataAccess.TemplatesOfRoute.query title |> List.tryHead with
        | Some row -> row
        | None ->
            fprintfn stderr "loadTemplates: title not found: %s" title
            list.Empty

    let private substituteTemplateParameter (fpl: list<FunctionParameter>) (tpl: list<Parameter>) =
        match fpl, tpl with
        | (FunctionParameter.String ("1") :: _, Parameter.String (_, hd) :: _) -> hd /// todo
        | _ ->
            fprintfn stderr "unexpected template parameter: %A" fpl
            ""

    let private evalIfeqInTemplate (n: string) (fpl: list<FunctionParameter>) (pl: list<Parameter>) (tpl: list<Parameter>) =
        match pl with
        | Parameter.String (_, s2) :: Parameter.String (_, s3) :: Parameter.String (_, s4) :: _ ->
            let v = substituteTemplateParameter fpl tpl
            if v = s2 then Composite.String(s2) else Composite.String(s4) /// todo
        | Parameter.String (_, s2) :: Parameter.String (_, s3) :: _ ->
            let v = substituteTemplateParameter fpl tpl
            if v = s2 then Composite.String(s2) else Composite.String("") /// todo
        | _ ->
            fprintfn stderr "unexpected ifeq parameters: %A" pl
            Composite.Template(n, List.empty, pl)

    let private evalSwitchInTemplate (n: string) (fpl: list<FunctionParameter>) (pl: list<Parameter>) (tpl: list<Parameter>) =
        match pl with
        | Parameter.String (sw1, v1) :: Parameter.String (sw2, v2) :: _ -> /// todo
            let v = substituteTemplateParameter fpl tpl
            if v = sw1 then Composite.String(v1)
            else if v = sw2 then Composite.String(v2) /// todo
            else Composite.String("")
        | _ ->
            fprintfn stderr "unexpected switch parameters: %A" pl
            Composite.Template(n, List.empty, pl)

    let private evalFunctionInComposites (composites: list<Composite>) (tpl: list<Parameter>) =
        List.foldBack (fun (c: Composite) st ->
            let newc =
                match c with
                | Composite.Template (n, fpl, pl) when n.StartsWith "#ifeq" -> evalIfeqInTemplate n fpl pl tpl
                | Composite.Template (n, fpl, pl) when n.StartsWith "#switch" -> evalSwitchInTemplate n fpl pl tpl
                | _ -> c

            newc :: st) composites List.empty

    let private evalFunctionInParameters (parameters: list<Parameter>) (tpl: list<Parameter>) =
        List.foldBack (fun (p: Parameter) st ->
            let newp =
                match p with
                | Parameter.Composite (s, cl) -> Parameter.Composite(s, evalFunctionInComposites cl tpl)
                | _ -> p

            p :: st) parameters List.empty

    let private evalFunctionInTemplates (templates: list<Template>) (tpl: list<Parameter>): list<Template> =
        List.foldBack (fun ((n, _, pl): Template) st ->
            let newt =
                (n, List.empty, evalFunctionInParameters pl tpl)

            newt :: st) templates List.empty

    let private evalTemplate (title: string) (tpl: list<Parameter>) =
        evalFunctionInTemplates (loadTemplates title) tpl

    let private evalTemplates (templates: list<Template>) =
        List.foldBack (fun (t: Template) st ->
            match t with
            | (n, _, pl) when n.StartsWith "Bahnstrecke" -> (evalTemplate ("Vorlage:" + n) pl) @ st
            | _ -> t :: st) templates List.empty

    let loadTemplatesForWikiTitle (title: string) =
        loadTemplates title
        |> evalTemplates
        |> List.toArray

    let private markersOfStop = [| "BHF"; "DST"; "HST" |]

    let private excludes (link:string) =
        link.StartsWith "Bahnstrecke" || link.StartsWith "Datei" || link.Contains "#" || link.Contains ":" || link.Contains "&"

    let private getStationLinksOfTemplates (title:string) = 
        loadTemplates title
        |> Templates.findLinks markersOfStop
        |> List.filter (fun (link,_) -> not (excludes link))
        |> List.map (fun (link,_) -> link)
        
    let getStationLinks () = 
        DataAccess.TemplatesOfRoute.queryKeys()
        |> List.collect getStationLinksOfTemplates
        |> List.distinct
        |> List.sort
        |> List.iter (fun s -> printfn "%s" s)

