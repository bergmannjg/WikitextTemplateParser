
open RouteInfo
open StationsOfRoute
open StationsOfInfobox
open DbData
open Comparer
open Types
open ResultsOfMatch
open Wikidata
open Ast

let classifyBsDatenStreckenNr title showDetails =
    let templates = loadTemplatesForWikiTitle title showDetails
    match findRouteInfoInTemplates templates title with
    | Some strecken ->
        strecken|>Array.iter (printRouteInfo showDetails) 
    | None -> 
        ()

let findRouteInfoInTemplatesWithParameter (templates: Template []) title showDetails =
    match findRouteInfoInTemplates templates title with
    | Some routeInfos ->
        if routeInfos.Length = 0  then 
            printResultOfRoute showDetails (createResult title 0 RouteParameterNotParsed) 
        routeInfos
    | None -> 
        printResultOfRoute showDetails (createResult title 0 RouteParameterEmpty) 
        Array.empty

let difference (ri0:RouteInfo[]) (ri1:RouteInfo[]) =
    Set.difference (Set ri0) (Set ri1) |> Set.toArray

let findPassengerRouteInfoInTemplates (templates: Template []) title showDetails =
    let routeInfosFromParameter = findRouteInfoInTemplatesWithParameter templates title showDetails

    let passengerRoutes = routeInfosFromParameter |> Array.filter (fun s -> checkPersonenzugStreckenutzung s.nummer)
    if routeInfosFromParameter.Length > passengerRoutes.Length then 
        difference routeInfosFromParameter passengerRoutes
        |> Array.iter (fun route -> 
            printResultOfRoute showDetails (createResult title route.nummer RouteIsNoPassengerTrain)
            let dbStations = loadDBStations route.nummer
            DbData.dump title route.nummer dbStations
            ResultsOfMatch.dump title route.nummer (ResultsOfMatch.toResultOfStation dbStations)
            )
    passengerRoutes

let comparetitle title showDetails =
    let templates = loadTemplatesForWikiTitle title showDetails
    let stationsOfInfobox = templates |> Array.map findStationOfInfobox |> Array.choose id
    StationsOfInfobox.dump title stationsOfInfobox
    let routeInfos = findPassengerRouteInfoInTemplates templates title showDetails
    routeInfos
    |> Array.iter (fun route -> 
        let routeMatched = findRouteInfoStations route stationsOfInfobox (routeInfos.Length = 1)
        let dbStations = loadDBStations routeMatched.nummer
        DbData.dump title routeMatched.nummer dbStations
        let wikiStations = if dbStations.Length > 0 then filterStations routeMatched stationsOfInfobox else [||]
        StationsOfRoute.dump title routeMatched wikiStations
        compare title route routeMatched wikiStations dbStations
        |> printResult title routeMatched wikiStations stationsOfInfobox showDetails)

[<EntryPoint>]
let main argv =
    Serializer.addConverters ([| |])
    match argv with
    | [| "-dropCollection"; collection |] -> DataAccess.dropCollection collection |> ignore
    | [| "-comparetitle"; title |] -> comparetitle title false
    | [| "-verbose"; "-comparetitle"; title |] -> comparetitle title true
    | [| "-showCompareResults" |] -> showResults()
    | [| "-classify"; title |] -> classifyBsDatenStreckenNr title false
    | [| "-verbose"; "-classify"; title |] -> classifyBsDatenStreckenNr title true
    | [| "-showClassifyResults" |] -> showRouteInfoResults()
    | [| "-showMatchKindStatistics" |] -> showMatchKindStatistics()
    | _ -> fprintfn stderr "usage: [-verbose] -comparetitle title | -showCompareResults | -classify title | -showClassifyResults | -showMatchKindStatistics"   
    0
