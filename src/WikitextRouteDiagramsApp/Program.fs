
open RouteInfo
open Comparer
open ResultsOfMatch
open Wikidata
open ParserProcessing
open OpPointMatch

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

let private chooseRouteLoader () =
    if System.IO.Directory.Exists "./dbdata/RINF" then RInfData.loadRoute else DbData.loadRoute

let comparetitle showDetails loadRoute title =
    loadTemplatesForWikiTitle title  
    |> compare showDetails title loadRoute

let comparetitles () =
    let loadRoute = chooseRouteLoader ()
    DataAccess.TemplatesOfRoute.queryKeys()
    |> List.iter (comparetitle false loadRoute)

let loadOsmData (route :int) = 
    let id = OsmData.loadRelationId (route)
    printfn "%A" id

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
    | [| "-comparetitle"; title |] -> comparetitle false (chooseRouteLoader()) title
    | [| "-verbose"; "-comparetitle"; title |] -> comparetitle true (chooseRouteLoader()) title
    | [| "-comparetitles" |] -> comparetitles ()
    | [| "-showComparisonResults" |] -> showComparisonResults ()
    | [| "-classifyRouteInfos" |] -> classifyRouteInfos ()
    | [| "-showRouteInfoResults" |] -> showRouteInfoResults ()
    | [| "-verbose"; "-showMatchKindStatistics" |] -> showMatchKindStatistics true
    | [| "-showMatchKindStatistics" |] -> showMatchKindStatistics false
    | [| "-showNotFoundStatistics" |] -> showNotFoundStatistics ()
    | [| "-queryName"; name |] -> OpPointsOfInfobox.queryName name
    | [| "-loadOsmData"; route |] -> loadOsmData (route |> int)
    | [| "-compareDbDataRoute"; route |] -> RInfData.compareDbDataRoute (route |> int) |> ignore
    | [| "-compareDbDataRoutes" |] -> RInfData.compareDbDataRoutes ()
    | [| "-loadSoL"; route|] -> RInfData.loadRoute (route |> int) |> ignore
    | [| "-matchStationName"; wkname; dbname |] -> printfn "%A" (matchStationName wkname dbname false)
    | _ -> fprintfn stderr "usage: -loadroutes | -parseroutes | -comparetitles"
    0


