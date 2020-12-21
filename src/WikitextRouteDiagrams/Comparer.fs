/// compare wiki data with db data
module Comparer

open Types
open RouteInfo
open OpPointsOfInfobox
open OpPointsOfRoute
open OpPointMatch
open ResultsOfMatch
open Templates

let getBestMatch (matches: (DbOpPointOfRoute * WkOpPointOfRoute * MatchKind) []) =
    if matches.Length > 1 then
        let sorted = matches |> Array.sortWith compareMatch
        sorted.[0]
    else
        matches.[0]

let findStation isPhase1 (wikiStations: WkOpPointOfRoute []) (dbStation: DbOpPointOfRoute) =
    let res =
        wikiStations
        |> Array.map (fun b ->
            if isPhase1
            then matchesWkStationWithDbStationPhase1 b dbStation
            else matchesWkStationWithDbStationPhase2 b dbStation)
        |> Array.choose id

    if res.Length = 0 then Failure(dbStation) else Success(getBestMatch res)

let private checkDbDataInWikiDataPhase1 (route: RouteInfo)
                                        (wikiStations: WkOpPointOfRoute [])
                                        (dbStations: DbOpPointOfRoute [])
                                        =
    dbStations
    |> Array.map (fun p -> findStation true wikiStations p)
    |> removeDoubleWkStations

let private checkDbDataInWikiDataPhase2 (route: RouteInfo)
                                        (wikiStations: WkOpPointOfRoute [])
                                        (results: ResultOfOpPoint [])
                                        =
    let restOfWikiStations =
        wikiStations
        |> Array.filter (fun p ->
            not
                (results
                 |> Array.exists (fun r ->
                     match r with
                     | Success (_, wk, _) -> wk.name = p.name && wk.kms = p.kms
                     | Failure p -> false)))

    results
    |> Array.map (fun r ->
        match r with
        | Success _ -> r
        | Failure p -> findStation false restOfWikiStations p)
    |> removeDoubleWkStations

let private mapReplacements (route: RouteInfo)
                            (wikiStations: WkOpPointOfRoute [])
                            (replacements: (string * int * string * string) [])
                            (results: ResultOfOpPoint [])
                            =
    results
    |> Array.map (fun result ->
        match result with
        | Failure db
        | Success (db, _, _) ->
            match (replacements
                   |> Array.tryFind (fun (t, r, dbname, _) ->
                       route.title = t
                       && route.nummer = r
                       && dbname = db.name)) with
            | Some (_, _, _, wkname) ->
                let kms =
                    match wikiStations
                          |> Array.tryFind (fun s -> s.name = wkname) with
                    | Some s -> s.kms
                    | None -> [||]

                Success
                    (db,
                     { kms = kms
                       name = wkname
                       shortname = "" },
                     MatchKind.SpecifiedMatch)
            | None -> result)

let private mapNonexistentStationsInFailures (route: RouteInfo)
                                             (nonexistentStations: (string * int * string) [])
                                             (mk: MatchKind)
                                             (results: ResultOfOpPoint [])
                                             =
    results
    |> Array.map (fun result ->
        match result with
        | Failure db ->
            match (nonexistentStations
                   |> Array.tryFind (fun (t, r, s) -> route.title = t && route.nummer = r && s = db.name)) with
            | Some (t, r, s) ->
                Success
                    (db,
                     { kms = [||]
                       name = "---"
                       shortname = "" },
                     mk)
            | None -> result
        | _ -> result)

let private mapNonexistentPrefixInFailures (route: RouteInfo)
                                           (prefix: string)
                                           (stelleArt:string)
                                           (mk: MatchKind)
                                           (results: ResultOfOpPoint [])
                                           =
    results
    |> Array.map (fun result ->
        match result with
        | Failure db when db.name.StartsWith prefix
                          || db.STELLE_ART = stelleArt ->
            Success
                (db,
                 { kms = [||]
                   name = "---"
                   shortname = "" },
                 mk)
        | _ -> result)

let private checkDbDataInWikiData (route: RouteInfo)
                                  (wikiStations: WkOpPointOfRoute [])
                                  (dbStations: DbOpPointOfRoute [])
                                  =
    dbStations
    |> checkDbDataInWikiDataPhase1 route wikiStations
    |> checkDbDataInWikiDataPhase2 route wikiStations
    |> filterResultsOfRoute route
    |> mapReplacements route wikiStations AdhocReplacements.Comparer.matchingsOfDbWkOpPoints
    |> mapNonexistentPrefixInFailures route "StrUeb" "Weiche" IgnoredDbOpPoint
    |> mapNonexistentStationsInFailures route AdhocReplacements.Comparer.nonexistentWkOpPoints IgnoredWkOpPoint

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

let private dump (title: string)
                 (strecke: RouteInfo)
                 (precodedStations: OpPointOfInfobox [])
                 (stations: WkOpPointOfRoute [])
                 (results: ResultOfOpPoint [])
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

let private isDbRouteComplete (results: ResultOfOpPoint []) (dbStations: DbOpPointOfRoute []) =
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

let private countShutdownStations (stationsOfInfobox: seq<OpPointOfInfobox>) =
    stationsOfInfobox
    |> Seq.filter (fun s ->
        isStation s.symbols checkIsShutdownBhfSymbolTypes
        && isShutdownStation s.symbols)
    |> Seq.length

let private countActiveStations (stationsOfInfobox: seq<OpPointOfInfobox>) =
    stationsOfInfobox
    |> Seq.filter (fun s ->
        isStation s.symbols checkIsActiveBhfSymbolTypes
        && not (isShutdownStation s.symbols))
    |> Seq.length

let private compareStations (title: string)
                            (routesInTitle: int)
                            (routeInfoOrig: RouteInfo)
                            (routeInfoMatched: RouteInfo)
                            (wikiStations: WkOpPointOfRoute [])
                            (dbStations: DbOpPointOfRoute [])
                            (stationsOfInfobox: OpPointOfInfobox [])
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
                        (wikiStations: WkOpPointOfRoute [])
                        (stationsOfInfobox: OpPointOfInfobox [])
                        showDetails
                        ((resultOfRoute, resultsOfMatch): (ResultOfRoute * ResultOfOpPoint []))
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
    && DbData.checkPersonenzugStreckenutzung routeInfo.nummer

let private findPassengerRouteInfoInTemplates (routeInfosFromParameter: List<RouteInfo>)
                                              title
                                              showDetails
                                              (loadRoute: int -> DbOpPointOfRoute [])
                                              =
    let passengerRoutes =
        routeInfosFromParameter
        |> List.filter isPassengerRoute

    if routeInfosFromParameter.Length > passengerRoutes.Length then
        difference routeInfosFromParameter passengerRoutes
        |> Seq.iter (fun route ->
            printResultOfRoute
                showDetails
                (createResult title route.nummer routeInfosFromParameter.Length Types.ResultKind.RouteIsNoPassengerTrain)
            let dbStations = loadRoute route.nummer
            DbData.dump title route.nummer dbStations
            ResultsOfMatch.dump title route.nummer (ResultsOfMatch.toResultOfStation dbStations))
    passengerRoutes

let compare showDetails title (loadRoute: int -> DbOpPointOfRoute []) templates =
    let stationsOfInfobox =
        templates
        |> Array.map findStationOfInfobox
        |> Array.choose id

    OpPointsOfInfobox.dump title stationsOfInfobox

    let routeInfosFromParameter =
        findRouteInfoInTemplatesWithParameter templates title showDetails

    let routeInfos =
        findPassengerRouteInfoInTemplates routeInfosFromParameter title showDetails loadRoute

    routeInfos
    |> Seq.iter (fun route ->
        let routeMatched =
            findRouteInfoStations route stationsOfInfobox (routeInfos.Length = 1)

        let dbStations = loadRoute routeMatched.nummer
        DbData.dump title routeMatched.nummer dbStations

        let wikiStations =
            if dbStations.Length > 0
            then filterStations routeMatched stationsOfInfobox
            else Array.empty

        OpPointsOfRoute.dump title routeMatched wikiStations
        compareStations
            title
            routeInfosFromParameter.Length
            route
            routeMatched
            wikiStations
            dbStations
            stationsOfInfobox
        |> printResult title routeMatched wikiStations stationsOfInfobox showDetails)
