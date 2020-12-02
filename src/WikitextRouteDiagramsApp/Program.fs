
open RouteInfo
open Comparer
open ResultsOfMatch
open Wikidata
open Templates

/// prepare template string, todo: add to parser
let prepare (s: string) (title:string) =
    s 
    |> StringUtilities.replaceFromRegexToEmpty AdhocReplacements.regexRef
    |> StringUtilities.replaceFromRegexToEmpty AdhocReplacements.regexRefSelfClosed
    |> StringUtilities.replaceFromRegexToEmpty AdhocReplacements.regexComment
    |> StringUtilities.replaceFromList AdhocReplacements.replacements (fun t -> t = title)

/// load Vorlage:Bahnstrecke, ex. template 'Bahnstrecke Köln–Troisdorf' in title 'Siegstrecke'
let loadBahnstreckeTemplate ((name,_,_):Template) (parseTemplates: (string -> unit)) =
    let name = "Vorlage:" + name
    loadTemplatesOfRoutes false [|name|]
    parseTemplates name

let checkBahnstreckeTemplate (templates:Templates) (parseTemplates: (string -> unit)) =
    templates|>List.iter (fun t-> match t with | (s,_,_) when s.StartsWith "Bahnstrecke" -> loadBahnstreckeTemplate t parseTemplates      | _ -> ())

let rec parseTemplatesOfType (maybeCheck: (Templates -> (string -> unit) -> unit) option) (query: string -> list<string>) (insert: string -> Templates -> System.Guid) title =
    fprintfn stdout "parseTemplates: %s" title 
    match (query title |> List.tryHead) with
    | Some text -> 
        match Parser.parse text with
        | FParsec.CharParsers.ParserResult.Success (result, _, _) -> 
            fprintfn stdout "Success: %s, templates Length %d" title result.Length
            insert title result |> ignore
            match maybeCheck with
            | Some check -> check result (parseTemplatesOfType None query insert)
            | None -> ()
        | FParsec.CharParsers.ParserResult.Failure (errorMsg, _, _) -> fprintfn stderr "\n***Parser failure: %s" errorMsg
    | None -> ()

let parseTemplatesOfRoute title =
    parseTemplatesOfType (Some checkBahnstreckeTemplate) DataAccess.WikitextOfRoute.query DataAccess.TemplatesOfRoute.insert title

let parseTemplatesOfRoutes () =
    DataAccess.WikitextOfRoute.queryKeys()
    |> List.iter parseTemplatesOfRoute

let parseTemplatesOfStop title =
    parseTemplatesOfType None DataAccess.WikitextOfStop.query DataAccess.TemplatesOfStop.insert title

let parseTemplatesOfStops () =
    DataAccess.WikitextOfStop.queryKeys()
    |> List.iter parseTemplatesOfStop

let loadTemplatesOfRoutesFromFile filename =
    System.IO.File.ReadAllLines filename
    |> loadTemplatesOfRoutes false

let loadTemplatesOfStopsFromFile filename  =
    System.IO.File.ReadAllLines filename
    |> loadTemplatesOfStops false

let markersOfStop = [| "BHF"; "DST"; "HST" |]

let excludes (link:string) =
    link.StartsWith "Bahnstrecke" || link.StartsWith "Datei" || link.Contains "#" || link.Contains ":" || link.Contains "&"

let getStationLinksOfTemplates (title:string) = 
    loadTemplates title
    |> findLinks markersOfStop
    |> List.filter (fun (link,_) -> not (excludes link))
    |> List.map (fun (link,_) -> link)
    
let getStationLinks () = 
    DataAccess.TemplatesOfRoute.queryKeys()
    |> List.collect getStationLinksOfTemplates
    |> List.distinct
    |> List.sort
    |> List.iter (fun s -> printfn "%s" s)

let classifyBsDatenStreckenNr showDetails title  =
    let templates = loadTemplatesForWikiTitle title  
    match findRouteInfoInTemplates templates title with
    | Some strecken ->
        strecken|>List.iter (printRouteInfo showDetails) 
    | None -> 
        ()

let classifyBsDatenStreckenNrTitles () =
    DataAccess.TemplatesOfRoute.queryKeys()
    |> List.iter (classifyBsDatenStreckenNr false)

let comparetitle showDetails title =
    loadTemplatesForWikiTitle title  
    |> compare showDetails title

let comparetitles () =
    DataAccess.TemplatesOfRoute.queryKeys()
    |> List.iter (comparetitle false)

[<EntryPoint>]
let main argv =
    Serializer.addConverters ([| |])
    match argv with
    | [| "-loadroute"; route |] ->
        loadTemplatesOfRoutes true [|route|]  
    | [| "-loadroutes"; filename |] ->
        loadTemplatesOfRoutesFromFile filename
    | [| "-parseroute"; route |] -> 
        parseTemplatesOfRoute route  
    | [| "-parseroutes" |] -> 
        parseTemplatesOfRoutes ()
    | [| "-loadstop"; stop |] ->
        loadTemplatesOfStops true [|stop|]  
    | [| "-loadstops"; filename |] ->
        loadTemplatesOfStopsFromFile filename
    | [| "-parsestop"; stop |] -> 
        parseTemplatesOfStop stop  
    | [| "-parsestops" |] -> 
        parseTemplatesOfStops ()
    | [| "-showtitles" |] ->
        getWikipediaArticles 10000 
        |> Seq.iter (fun t -> printfn "%s" t)
    | [| "-showstations" |] ->
        getWikipediaStations 10000 
        |> Seq.iter (fun t -> printfn "%A" t)
    | [| "-dropCollection"; collection |] -> DataAccess.dropCollection collection |> ignore
    | [| "-getStationLinks" |] -> getStationLinks ()
    | [| "-comparetitle"; title |] -> comparetitle  false title
    | [| "-verbose"; "-comparetitle"; title |] -> comparetitle  true title
    | [| "-comparetitles" |] -> comparetitles ()
    | [| "-showCompareResults" |] -> showComparisonResults()
    | [| "-classify" |] -> classifyBsDatenStreckenNrTitles ()
    | [| "-showClassifyResults" |] -> showRouteInfoResults()
    | [| "-showMatchKindStatistics" |] -> showMatchKindStatistics()
    | _ -> fprintfn stderr "usage: [-verbose] -comparetitle title | -showCompareResults | -classify title | -showClassifyResults | -showMatchKindStatistics"   
    0
