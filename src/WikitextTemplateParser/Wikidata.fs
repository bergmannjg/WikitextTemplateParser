module Wikidata

open FSharp.Data
open System.Web
open System.Text.RegularExpressions

type WikiType =
    | Bahnstrecke
    | Station

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
    loadTemplatesOfType verbose wikiTitles WikiType.Bahnstrecke DataAccess.Wikitext.insert

let loadTemplatesOfStops verbose (wikiTitles: string []) =
    loadTemplatesOfType verbose wikiTitles WikiType.Station DataAccess.WikitextOfStop.insert
