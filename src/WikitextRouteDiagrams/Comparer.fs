/// compare wiki data with db data
module Comparer

open Types
open RouteInfo
open StationsOfInfobox
open StationsOfRoute
open DbData
open StationMatch
open ResultsOfMatch
open Templates

let compareMatch ((db0, wk0, mk0): DbStationOfRoute * StationOfRoute * MatchKind)
                 ((db1, wk1, mk1): DbStationOfRoute * StationOfRoute * MatchKind)
                 =
    if wk0.kms.Length = 0 || wk1.kms.Length = 0 then
        if mk0 = mk1 then 0
        else if mk0 < mk1 then -1
        else 1
    else
        let diff0 =
            wk0.kms
            |> Array.map (fun k -> abs (db0.km - k))
            |> Array.min

        let diff1 =
            wk1.kms
            |> Array.map (fun k -> abs (db1.km - k))
            |> Array.min

        if diff0 = diff1 then 0
        else if diff0 < diff1 then -1
        else 1

let getBestMatch (matches: (DbStationOfRoute * StationOfRoute * MatchKind) []) =
    if matches.Length > 1 then
        let sorted = matches |> Array.sortWith compareMatch
        sorted.[0]
    else
        matches.[0]

let findStation (wikiStations: StationOfRoute []) (dbStation: DbStationOfRoute) =
    let res =
        wikiStations
        |> Array.map (fun b -> matchesWkStationWithDbStation b dbStation)
        |> Array.choose id

    if res.Length = 0 then Failure(dbStation) else Success(getBestMatch res)

let maybeReplaceDbStation (strecke: RouteInfo) (station: DbStationOfRoute) =
    match AdhocReplacements.replacementsInDbStation
          |> Array.tryFind (fun (t, r, s, _) ->
              t = strecke.title
              && r = strecke.nummer
              && s = station.name) with
    | Some (_, _, _, s) -> { station with name = s }
    | None -> station

let checkDbDataInWikiData (strecke: RouteInfo) (wikiStations: StationOfRoute []) (dbStations: DbStationOfRoute []) =
    let results =
        dbStations
        |> Array.map (fun p -> findStation wikiStations (maybeReplaceDbStation strecke p))

    results |> filterResultsOfRoute strecke

let countResultFailures results =
    results
    |> Array.filter (fun result ->
        match result with
        | Failure _ -> true
        | Success _ -> false)
    |> Array.length

let countResultSuccess results =
    results
    |> Array.filter (fun result ->
        match result with
        | Failure _ -> false
        | Success _ -> true)
    |> Array.length

let dump (title: string)
         (strecke: RouteInfo)
         (precodedStations: StationOfInfobox [])
         (stations: StationOfRoute [])
         (results: ResultOfStation [])
         =
    let lines = ResizeArray<string>()
    sprintf "fromTo: %A" [| strecke.von; strecke.bis |]
    |> lines.Add
    sprintf "precodedStations:" |> lines.Add
    precodedStations
    |> Seq.iter (sprintf "%A" >> lines.Add)
    sprintf "stations:" |> lines.Add
    stations |> Seq.iter (sprintf "%A" >> lines.Add)
    sprintf "results:" |> lines.Add
    results
    |> Array.iter (fun result ->
        match result with
        | Success (db, wk, mk) ->
            sprintf "find db station '%s' %.1f for wk station '%s', matchkind %A" db.name db.km wk.name mk
            |> lines.Add
        | Failure p ->
            sprintf "*** failed to find station for position '%s' %A" p.name p.km
            |> lines.Add)
    let s = String.concat "\n" lines
    System.IO.File.WriteAllText
        ("./dump/"
         + title
         + "-"
         + strecke.nummer.ToString()
         + ".txt",
         s)

let private isDbRouteComplete (results: ResultOfStation []) (dbStations: DbStationOfRoute []) =
    let dbFirst = dbStations.[0]
    let dbLast = dbStations.[dbStations.Length - 1]

    let foundFirst =
        results
        |> existsInDbSuccessResults (fun db -> db.km = dbFirst.km)

    let foundLast =
        results
        |> existsInDbSuccessResults (fun db -> db.km = dbLast.km)

    foundFirst && foundLast

let private existsWkStationInResultSuccess (station: string) results =
    results
    |> Array.exists (fun result ->
        match result with
        | Success (_, wk, _) when wk.name = station -> true
        | _ -> false)

let private maybeReplaceResultkind (strecke: RouteInfo) (resultOfRoute: ResultOfRoute) =
    let candidate =
        AdhocReplacements.adhocResultKindChanges
        |> Array.tryFind (fun (title, route, rkWrong, rk) ->
            title = strecke.title
            && route = strecke.nummer
            && rkWrong = resultOfRoute.resultKind)

    match candidate with
    | Some (_, _, _, rk) -> { resultOfRoute with resultKind = rk }
    | None -> resultOfRoute

let private checkIsActiveBhfSymbolTypes = [| "BHF"; "ÜST"; "HST"; "BST" |]

let private checkIsShutdownBhfSymbolTypes =
    [| "BHF"
       "DST"
       "ABZ"
       "ÜST"
       "HST"
       "BST" |]

let private isStation (symbols: string []) (symbolsToCheck: string []) =
    symbols
    |> Array.exists (fun s -> symbolsToCheck |> Array.exists s.Contains)

let private isShutdownStation (symbols: string []) =
    symbols
    |> Array.exists (fun s -> s.StartsWith "x" || s.StartsWith "e")

let private countShutdownStations (stationsOfInfobox: seq<StationOfInfobox>) =
    stationsOfInfobox
    |> Seq.filter (fun s ->
        isStation s.symbols checkIsShutdownBhfSymbolTypes
        && isShutdownStation s.symbols)
    |> Seq.length

let private countActiveStations (stationsOfInfobox: seq<StationOfInfobox>) =
    stationsOfInfobox
    |> Seq.filter (fun s ->
        isStation s.symbols checkIsActiveBhfSymbolTypes
        && not (isShutdownStation s.symbols))
    |> Seq.length

let private compareStations (title: string)
                            (routesInTitle: int)
                            (routeInfoOrig: RouteInfo)
                            (routeInfoMatched: RouteInfo)
                            (wikiStations: StationOfRoute [])
                            (dbStations: DbStationOfRoute [])
                            (stationsOfInfobox: StationOfInfobox [])
                            =
    let resultsOfMatch =
        if wikiStations.Length > 0 && dbStations.Length > 0
        then checkDbDataInWikiData routeInfoMatched wikiStations dbStations
        else Array.empty

    ResultsOfMatch.dump title routeInfoMatched.nummer resultsOfMatch

    let countWikiStops = wikiStations.Length
    let countDbStops = dbStations.Length
    let countDbStopsFound = countResultSuccess resultsOfMatch
    let countDbStopsNotFound = countResultFailures resultsOfMatch
    let minmaxkm = (getSuccessMinMaxDbKm resultsOfMatch)

    let isCompleteDbRoute =
        dbStations.Length > 0
        && isDbRouteComplete resultsOfMatch dbStations

    let resultkind =
        getResultKind
            countWikiStops
            countDbStops
            countDbStopsFound
            countDbStopsNotFound
            routeInfoMatched.railwayGuide
            (routeInfoMatched.routenameKind = Unmatched)
            (countActiveStations stationsOfInfobox)
            (countShutdownStations stationsOfInfobox)

    let resultOfRoute =
        { route = routeInfoMatched.nummer
          title = title
          routesInTitle = routesInTitle
          fromToNameOrig =
              [| routeInfoOrig.von
                 routeInfoOrig.bis |]
          fromToNameMatched =
              [| routeInfoMatched.von
                 routeInfoMatched.bis |]
          fromToKm = minmaxkm
          countWikiStops = countWikiStops
          countDbStops = countDbStops
          countDbStopsFound = countDbStopsFound
          countDbStopsNotFound = countDbStopsNotFound
          resultKind = resultkind
          railwayGuide =
              match routeInfoMatched.railwayGuide with
              | Some s -> s
              | None -> ""
          isCompleteDbRoute = isCompleteDbRoute }
        |> maybeReplaceResultkind routeInfoMatched

    (resultOfRoute, resultsOfMatch)

let private printResult (title: string)
                        (streckeMatched: RouteInfo)
                        (wikiStations: StationOfRoute [])
                        (stationsOfInfobox: StationOfInfobox [])
                        showDetails
                        ((resultOfRoute, resultsOfMatch): (ResultOfRoute * ResultOfStation []))
                        =
    if (showDetails) then
        dump title streckeMatched stationsOfInfobox wikiStations resultsOfMatch
        printfn
            "countActiveStations %d countShutdownStations %d"
            (countActiveStations stationsOfInfobox)
            (countShutdownStations stationsOfInfobox)
        printfn "see dumps ./dump/%s-%d.txt" title streckeMatched.nummer

    printResultOfRoute showDetails resultOfRoute

let private findRouteInfoInTemplatesWithParameter (templates: seq<Template>) title showDetails =
    match findRouteInfoInTemplates templates title with
    | Some routeInfos ->
        if routeInfos.Length = 0
        then printResultOfRoute showDetails (createResult title 0 0 Types.ResultKind.RouteParameterNotParsed)
        routeInfos
    | None ->
        printResultOfRoute showDetails (createResult title 0 0 Types.ResultKind.RouteParameterEmpty)
        List.empty

let private difference (ri0: list<RouteInfo>) (ri1: list<RouteInfo>) =
    Set.difference (Set ri0) (Set ri1) |> Set.toList

let private isPassengerRoute (routeInfo: RouteInfo) =
    not
        (routeInfo.railwayGuide.IsSome
         && routeInfo.railwayGuide.Value.Contains "nur Güterverkehr")
    && checkPersonenzugStreckenutzung routeInfo.nummer

let private findPassengerRouteInfoInTemplates (routeInfosFromParameter: List<RouteInfo>) title showDetails =
    let passengerRoutes =
        routeInfosFromParameter
        |> List.filter isPassengerRoute

    if routeInfosFromParameter.Length > passengerRoutes.Length then
        difference routeInfosFromParameter passengerRoutes
        |> Seq.iter (fun route ->
            printResultOfRoute
                showDetails
                (createResult title route.nummer routeInfosFromParameter.Length Types.ResultKind.RouteIsNoPassengerTrain)
            let dbStations = loadDBStations route.nummer
            DbData.dump title route.nummer dbStations
            ResultsOfMatch.dump title route.nummer (ResultsOfMatch.toResultOfStation dbStations))
    passengerRoutes

let compare showDetails title templates =
    let stationsOfInfobox =
        templates
        |> Array.map findStationOfInfobox
        |> Array.choose id

    StationsOfInfobox.dump title stationsOfInfobox

    let routeInfosFromParameter =
        findRouteInfoInTemplatesWithParameter templates title showDetails

    let routeInfos =
        findPassengerRouteInfoInTemplates routeInfosFromParameter title showDetails

    routeInfos
    |> Seq.iter (fun route ->
        let routeMatched =
            findRouteInfoStations route stationsOfInfobox (routeInfos.Length = 1)

        let dbStations = loadDBStations routeMatched.nummer
        DbData.dump title routeMatched.nummer dbStations

        let wikiStations =
            if dbStations.Length > 0
            then filterStations routeMatched stationsOfInfobox
            else Array.empty

        StationsOfRoute.dump title routeMatched wikiStations
        compareStations
            title
            routeInfosFromParameter.Length
            route
            routeMatched
            wikiStations
            dbStations
            stationsOfInfobox
        |> printResult title routeMatched wikiStations stationsOfInfobox showDetails)
