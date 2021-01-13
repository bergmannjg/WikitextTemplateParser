open FSharp.Collections

open WikitextRouteDiagrams

let loadTemplatesOfRoutesFromFile filename =
    System.IO.File.ReadAllLines filename
    |> Wikidata.loadTemplatesOfRoutes false

let loadTemplatesOfStopsFromFile filename =
    System.IO.File.ReadAllLines filename
    |> Wikidata.loadTemplatesOfStops false

let classifyRouteInfo showDetails title =
    let templates = Wikidata.loadTemplatesForWikiTitle title

    match RouteInfo.findRouteInfoInTemplates templates title with
    | Some strecken -> strecken |> List.iter (RouteInfo.printRouteInfo showDetails)
    | None -> ()

let classifyRouteInfos () =
    DataAccess.TemplatesOfRoute.queryKeys ()
    |> List.iter (classifyRouteInfo false)

let private chooseRouteLoader () =
    if System.IO.Directory.Exists "./dbdata/RINF" then
        RInfData.loadRoute
    else
        DbData.loadRoute

let comparetitle showDetails loadRoute title =
     Wikidata.loadTemplatesForWikiTitle title
    |> Comparer.compare showDetails title loadRoute

let comparetitles () =
    let loadRoute = chooseRouteLoader ()

    DataAccess.TemplatesOfRoute.queryKeys ()
    |> List.iter (comparetitle false loadRoute)

let loadOsmData (route: int) =
    let id = OsmData.loadRelationId (route)
    printfn "%A" id

let queryName (name: string) =
    let results =
        Serializer.Deserialize<ResultOfRoute []>(RouteInfo.Data.queryAll ())

    [ for r in results do
        if r.resultKind = ResultKind.WikidataFoundInDbData
           || r.resultKind = ResultKind.WikidataNotFoundInDbData then
            yield r.title ]
    |> List.distinct
    |> List.iter
        (fun t ->
            let head =
                OpPointOfInfobox.Data.query t
                |> List.tryHead

            match head with
            | Some stations ->
                stations
                |> Array.filter (fun s -> s.name.Contains name)
                |> Array.iter (fun s -> printfn "'%s', '%s'" t s.name)
            | None -> ())

[<EntryPoint>]
let main argv =
    Serializer.addConverters ([||])

    match argv with
    | [| "-loadroute"; route |] -> Wikidata.loadTemplatesOfRoutes true [| route |]
    | [| "-loadroutes"; filename |] -> loadTemplatesOfRoutesFromFile filename
    | [| "-parseroute"; route |] -> ParserProcessing.parseTemplatesOfRoute route
    | [| "-parseroutes" |] -> ParserProcessing.parseTemplatesOfRoutes ()
    | [| "-loadstop"; stop |] -> Wikidata.loadTemplatesOfStops true [| stop |]
    | [| "-loadstops"; filename |] -> loadTemplatesOfStopsFromFile filename
    | [| "-parsestop"; stop |] -> ParserProcessing.parseTemplatesOfStop stop
    | [| "-parsestops" |] -> ParserProcessing.parseTemplatesOfStops ()
    | [| "-showtitles" |] ->
        Wikidata.getWikipediaArticles 10000
        |> Seq.iter (fun t -> printfn "%s" t)
    | [| "-showstations" |] ->
        Wikidata.getWikipediaStations 10000
        |> Seq.iter (fun t -> printfn "%A" t)
    | [| "-dropCollection"; collection |] -> DataAccess.dropCollection collection |> ignore
    | [| "-getStationLinks" |] -> Wikidata.getStationLinks ()
    | [| "-comparetitle"; title |] -> comparetitle false (chooseRouteLoader ()) title
    | [| "-verbose"; "-comparetitle"; title |] -> comparetitle true (chooseRouteLoader ()) title
    | [| "-comparetitles" |] -> comparetitles ()
    | [| "-showComparisonResults" |] -> ResultOfRoute.showComparisonResults ()
    | [| "-classifyRouteInfos" |] -> classifyRouteInfos ()
    | [| "-showRouteInfoResults" |] -> RouteInfo.showRouteInfoResults ()
    | [| "-verbose"; "-showMatchKindStatistics" |] -> ResultOfRoute.showMatchKindStatistics true
    | [| "-showMatchKindStatistics" |] -> ResultOfRoute.showMatchKindStatistics false
    | [| "-showNotFoundStatistics" |] -> ResultOfRoute.showNotFoundStatistics ()
    | [| "-queryName"; name |] -> queryName name
    | [| "-loadOsmData"; route |] -> loadOsmData (route |> int)
    | [| "-compareDbDataRoute"; route |] ->
        RInfData.compareDbDataRoute (route |> int)
        |> ignore
    | [| "-compareDbDataRoutes" |] -> RInfData.compareDbDataRoutes ()
    | [| "-compareDbDataOps" |] -> RInfData.compareDbDataOps ()
    | [| "-loadSoL"; route |] -> RInfData.loadRoute (route |> int) |> ignore
    | [| "-matchStationName"; wkname; dbname |] -> printfn "%A" (OpPointMatch.matchStationName wkname dbname false)
    | [| "-diffMatchPatch"; s1; s2 |] -> ResultOfRoute.loadDiffMatchPatch s1 s2
    | [| "-getBRouterUrl"; rt; s1; s2 |] -> printfn "%A" (RInfData.getBRouterUrlOfSol (rt |> int) s1 s2)
    | _ -> fprintfn stderr "usage: -loadroutes | -parseroutes | -comparetitles"

    0
