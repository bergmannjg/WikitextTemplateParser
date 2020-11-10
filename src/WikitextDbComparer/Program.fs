
open RouteInfo
open StationsOfRoute
open StationsOfInfobox
open DbData
open Comparer
open Types
open ResultsOfMatch
open Wikidata

let classifyBsDatenStreckenNr title showDetails =
    let templates = loadTemplatesForWikiTitle title showDetails
    match findBsDatenStreckenNr templates title with
    | Some strecken ->
        strecken|>Array.iter (printRouteInfo showDetails) 
    | None -> 
        ()

let comparetitle title showDetails =
    let templates = loadTemplatesForWikiTitle title showDetails
    let streckenAlle = match findBsDatenStreckenNr templates title with
                       | Some strecken ->
                            if strecken.Length = 0  then 
                                printResult (createResult title 0 RouteParameterNotParsed) showDetails
                                if showDetails then 
                                    printfn "see wikitext ./cache/%s.txt" title
                                    printfn "see templates ./wikidata/%s.txt" title
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
   
    if strecken.Length>0 then
        let precodedStations = templates |> Array.map findStationOfInfobox |> Array.choose id
        StationsOfInfobox.dump title precodedStations
        strecken
        |> Array.iter (fun route -> 
            let routeMatched = getMatchedRouteInfo route precodedStations (strecken.Length = 1)
            let dbStations = loadDBStations routeMatched.nummer
            let wikiStations = match dbStations.Length > 0 with 
                                | true -> filterStations routeMatched precodedStations
                                | _ -> [||]
            StationsOfRoute.dump title route wikiStations
            compare title route routeMatched wikiStations dbStations precodedStations showDetails)

[<EntryPoint>]
let main argv =
    Serializer.addConverters ([| |])
    match argv with
    | [| "-classify"; title |] -> classifyBsDatenStreckenNr title false
    | [| "-comparetitle"; title |] -> comparetitle title false
    | [| "-verbose"; "-comparetitle"; title |] -> comparetitle title true
    | [| "-showCompareResults"; path |] -> showResults(path)
    | [| "-showClassifyResults"; path |] -> showRouteInfoResults(path)
    | _ -> fprintfn stderr "usage: [-verbose] -comparetitle title | -showCompareResults path | -classify title | -showClassifyResults path"   
    0
