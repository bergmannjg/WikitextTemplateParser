
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

let findValidRouteInfoInTemplates (templates: Template []) title showDetails =
    let streckenAlle = match findRouteInfoInTemplates templates title with
                       | Some strecken ->
                            if strecken.Length = 0  then 
                                printResult (createResult title 0 RouteParameterNotParsed) showDetails
                            strecken
                       | None -> 
                            printResult (createResult title 0 RouteParameterEmpty) showDetails
                            Array.empty

    let strecken = streckenAlle |> Array.filter (fun s -> s.nummer>=1000 && checkPersonenzugStreckenutzung s.nummer)
    if streckenAlle.Length > 0 && streckenAlle.Length > strecken.Length then 
        let streckenOhne = streckenAlle|>Array.map (fun s -> s.nummer)|>Array.filter (fun nr -> not (strecken|>Array.exists (fun s -> s.nummer=nr)))
        streckenOhne |> Array.iter (fun nr -> 
            printResult (createResult title nr RouteIsNoPassengerTrain) showDetails
            let dbStations = loadDBStations nr
            ResultsOfMatch.dump title nr (ResultsOfMatch.toResultOfStation dbStations)
            )
        if showDetails then fprintfn stderr "%s, keine Fernbahnnutzung %A" title streckenOhne 
    strecken

let comparetitle title showDetails =
    let templates = loadTemplatesForWikiTitle title showDetails
    let strecken = findValidRouteInfoInTemplates templates title showDetails
    if strecken.Length>0 then
        let stationsOfInfobox = templates |> Array.map findStationOfInfobox |> Array.choose id
        StationsOfInfobox.dump title stationsOfInfobox
        strecken
        |> Array.iter (fun route -> 
            let routeMatched = findRouteInfoStations route stationsOfInfobox (strecken.Length = 1)
            let dbStations = loadDBStations routeMatched.nummer
            DbData.dump title routeMatched.nummer dbStations
            let wikiStations = if dbStations.Length > 0 then filterStations routeMatched stationsOfInfobox else [||]
            StationsOfRoute.dump title routeMatched wikiStations
            compare title route routeMatched wikiStations dbStations stationsOfInfobox showDetails)

[<EntryPoint>]
let main argv =
    Serializer.addConverters ([| |])
    match argv with
    | [| "-comparetitle"; title |] -> comparetitle title false
    | [| "-verbose"; "-comparetitle"; title |] -> comparetitle title true
    | [| "-showCompareResults" |] -> showResults()
    | [| "-classify"; title |] -> classifyBsDatenStreckenNr title false
    | [| "-verbose"; "-classify"; title |] -> classifyBsDatenStreckenNr title true
    | [| "-showClassifyResults" |] -> showRouteInfoResults()
    | _ -> fprintfn stderr "usage: [-verbose] -comparetitle title | -showCompareResults | -classify title | -showClassifyResults path"   
    0
