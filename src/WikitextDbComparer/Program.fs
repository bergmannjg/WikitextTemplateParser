
open RouteInfo
open StationsOfRoute
open StationsOfInfobox
open DbData
open Comparer
open Types
open ResultsOfMatch
open Wikidata
open Ast

let markersOfStop = [| "BHF"; "DST"; "HST" |]

let excludes (link:string) =
    link.StartsWith "Bahnstrecke" || link.StartsWith "Datei" || link.Contains "#" || link.Contains ":" || link.Contains "&"

let getStationLinksOfTemplates (title:string) = 
    loadTemplates title
    |> findLinks markersOfStop
    |> List.filter (fun (link,_) -> not (excludes link))
    |> List.map (fun (link,_) -> 
        fprintfn stderr "link: %s" link
        link)
    
let getStationLinks () = 
    DataAccess.Templates.queryKeys()
    |> List.collect getStationLinksOfTemplates
    |> List.distinct
    |> List.sort
    |> List.iter (fun s -> printfn "%s" s)

let classifyBsDatenStreckenNr showDetails title  =
    let templates = loadTemplatesForWikiTitle title showDetails
    match findRouteInfoInTemplates templates title with
    | Some strecken ->
        strecken|>Array.iter (printRouteInfo showDetails) 
    | None -> 
        ()

let classifyBsDatenStreckenNrTitles () =
    DataAccess.Templates.queryKeys()
    |> List.iter (classifyBsDatenStreckenNr false)

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

let comparetitle showDetails title =
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
        compare title route routeMatched wikiStations dbStations stationsOfInfobox
        |> printResult title routeMatched wikiStations stationsOfInfobox showDetails)

let take numLines (lines: List<string>) =
    let numLines0 =
        if numLines > lines.Length then lines.Length else numLines

    lines
    |> List.take numLines0

let comparetitles numLines =
    DataAccess.Templates.queryKeys()
    |> take numLines
    |> List.iter (comparetitle false)

[<EntryPoint>]
let main argv =
    Serializer.addConverters ([| |])
    match argv with
    | [| "-dropCollection"; collection |] -> DataAccess.dropCollection collection |> ignore
    | [| "-getStationLinks" |] -> getStationLinks ()
    | [| "-comparetitle"; title |] -> comparetitle  false title
    | [| "-verbose"; "-comparetitle"; title |] -> comparetitle  true title
    | [| "-comparetitles"; strNumLines |] -> 
        match System.Int32.TryParse  strNumLines with
        | (true, numLines) -> comparetitles numLines
        | _, _ -> fprintfn stdout "integers expected: %s" strNumLines
    | [| "-comparetitles" |] -> comparetitles System.Int32.MaxValue
    | [| "-showCompareResults" |] -> showResults()
    | [| "-classify" |] -> classifyBsDatenStreckenNrTitles ()
    | [| "-showClassifyResults" |] -> showRouteInfoResults()
    | [| "-showMatchKindStatistics" |] -> showMatchKindStatistics()
    | _ -> fprintfn stderr "usage: [-verbose] -comparetitle title | -showCompareResults | -classify title | -showClassifyResults | -showMatchKindStatistics"   
    0
