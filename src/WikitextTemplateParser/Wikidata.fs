module Wikidata

open FSharp.Data
open System.Web
open System.Text.RegularExpressions

// SPARQL query for wikipedia articles of german railway routes, LIMIT needs value
// wdt:P31 wd:Q728937 'ist eine Eisenbahnstrecke'
// wdt:P17 wd:Q183 'Staat Deutschland'
// wdt:P1671 'Streckennummer'
let query = """
SELECT ?railway ?railwayLabel ?article WHERE {
  SERVICE wikibase:label { bd:serviceParam wikibase:language "[AUTO_LANGUAGE],de". }
  ?railway wdt:P31 wd:Q728937;
    wdt:P17 wd:Q183.
  ?article schema:about ?railway;
    schema:isPartOf <https://de.wikipedia.org/>.
}
LIMIT
"""

let getWikipediaArticles (limit: int) =
    let q =
        HttpUtility.UrlEncode(query + " " + limit.ToString())

    let results =
        HtmlDocument.Load("https://query.wikidata.org/sparql?query=" + q)

    let urls =
        results.Descendants [ "binding" ]
        |> Seq.filter (fun x -> x.HasAttribute("name", "article"))
        |> Seq.map (fun x -> HttpUtility.UrlDecode(HttpUtility.UrlDecode(x.InnerText())))
        |> Seq.toArray

    urls

let private getTitle (url: string) =
    let index = url.LastIndexOf('/')
    if index > 0
    then Some(url.Substring(index + 1, url.Length - index - 1))
    else None

let getTitles (urls: string []) = urls |> Array.map getTitle

let findWikiTitle (railwaynumber: string) =
    let results =
        HtmlDocument.Load
            ("http://www.google.de/search?q=Bahnstrecke+"
             + railwaynumber
             + "+site%3Ade.wikipedia.org")

    let links =
        results.Descendants [ "a" ]
        |> Seq.choose (fun x ->
            x.TryGetAttribute("href")
            |> Option.map (fun a -> x.InnerText(), a.Value()))

    let searchResults =
        links
        |> Seq.filter (fun (name, url) ->
            name
            <> "Cached"
            && name <> "Similar"
            && url.StartsWith("/url?"))
        |> Seq.map (fun (name, url) -> name, url.Substring(0, url.IndexOf("&sa=")).Replace("/url?q=", ""))
        |> Seq.toArray

    if searchResults.Length > 0 then
        match searchResults.[0] with
        | (a, b) ->
            let url =
                HttpUtility.UrlDecode(HttpUtility.UrlDecode b)

            getTitle url
    else
        None

let private tryFindEndOfTemplates (s: string) =
    let mutable last = s.IndexOf("{{BS-Trenner}}")
    if last < 0 then last <- s.IndexOf("|} |}")
    if last < 0 then last <- s.IndexOf("|} ")
    last

let rec private loadTemplatesOnline (wikiTitle: string) =
    let url =
        "https://de.wikipedia.org/wiki/Spezial:Exportieren/"
        + wikiTitle

    printfn "load url: %s " url

    let results = HtmlDocument.Load(url)

    let vorlage =
        results.Descendants(fun node -> node.HasName "text")
        |> Seq.toArray

    let mutable result = None
    if vorlage.Length = 1 then
        let s = (vorlage.[0].ToString())
        let start = s.IndexOf("{{BS")
        let last = tryFindEndOfTemplates s
        if start > 0 && last > 0 then
            result <- Some(s.Substring(start, last - start))
        else
            let regex =
                Regex(@"#weiterleitung\s*\[\[([^\]]+)\]\]", RegexOptions.IgnoreCase)

            let m = regex.Match s
            if m.Success && m.Groups.Count = 2
            then result <- loadTemplatesOnline m.Groups.[1].Value
    result

let loadTemplates (reload: bool) (wikiTitle: string) =
    match (reload,
           DataAccess.Wikitext.query wikiTitle
           |> List.tryHead) with
    | (false, Some text) -> Some text
    | _ ->
        match loadTemplatesOnline wikiTitle with
        | Some text ->
            DataAccess.Wikitext.insert wikiTitle text
            |> ignore
            Some text
        | None -> None
