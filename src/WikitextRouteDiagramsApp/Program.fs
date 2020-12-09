
open RouteInfo
open Comparer
open ResultsOfMatch
open Wikidata
open ParserProcessing
open StationsOfInfobox

let loadTemplatesOfRoutesFromFile filename =
    System.IO.File.ReadAllLines filename
    |> loadTemplatesOfRoutes false

let loadTemplatesOfStopsFromFile filename  =
    System.IO.File.ReadAllLines filename
    |> loadTemplatesOfStops false

let classifyRouteInfo showDetails title  =
    let templates = loadTemplatesForWikiTitle title  
    match findRouteInfoInTemplates templates title with
    | Some strecken ->
        strecken|>List.iter (printRouteInfo showDetails) 
    | None -> 
        ()

let classifyRouteInfos () =
    DataAccess.TemplatesOfRoute.queryKeys()
    |> List.iter (classifyRouteInfo false)

let comparetitle showDetails title =
    loadTemplatesForWikiTitle title  
    |> compare showDetails title

let comparetitles () =
    DataAccess.TemplatesOfRoute.queryKeys()
    |> List.iter (comparetitle false)

[<EntryPoint>]
let main argv =
    Serializer.addConverters ([||])
    match argv with
    | [| "-loadroute"; route |] -> loadTemplatesOfRoutes true [| route |]
    | [| "-loadroutes"; filename |] -> loadTemplatesOfRoutesFromFile filename
    | [| "-parseroute"; route |] -> parseTemplatesOfRoute route
    | [| "-parseroutes" |] -> parseTemplatesOfRoutes ()
    | [| "-loadstop"; stop |] -> loadTemplatesOfStops true [| stop |]
    | [| "-loadstops"; filename |] -> loadTemplatesOfStopsFromFile filename
    | [| "-parsestop"; stop |] -> parseTemplatesOfStop stop
    | [| "-parsestops" |] -> parseTemplatesOfStops ()
    | [| "-showtitles" |] ->
        getWikipediaArticles 10000
        |> Seq.iter (fun t -> printfn "%s" t)
    | [| "-showstations" |] ->
        getWikipediaStations 10000
        |> Seq.iter (fun t -> printfn "%A" t)
    | [| "-dropCollection"; collection |] -> DataAccess.dropCollection collection |> ignore
    | [| "-getStationLinks" |] -> getStationLinks ()
    | [| "-comparetitle"; title |] -> comparetitle false title
    | [| "-verbose"; "-comparetitle"; title |] -> comparetitle true title
    | [| "-comparetitles" |] -> comparetitles ()
    | [| "-showComparisonResults" |] -> showComparisonResults ()
    | [| "-classifyRouteInfos" |] -> classifyRouteInfos ()
    | [| "-showRouteInfoResults" |] -> showRouteInfoResults ()
    | [| "-showMatchKindStatistics" |] -> showMatchKindStatistics ()
    | [| "-showNotFoundStatistics" |] -> showNotFoundStatistics ()
    | [| "-queryName"; name |] -> StationsOfInfobox.queryName name
    | _ -> fprintfn stderr "usage: -loadroutes | -parseroutes | -comparetitles"
    0


