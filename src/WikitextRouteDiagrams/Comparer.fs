namespace WikitextRouteDiagrams

/// compare wiki data with db data
module Comparer =

    let private dump
        (title: string)
        (strecke: RouteInfo)
        (precodedStations: OpPointOfInfobox [])
        (stations: WkOpPointOfRoute [])
        results
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
        |> Array.iter
            (fun result ->
                match result with
                | ResultOfOpPoint.Success (db, wk, mk) ->
                    sprintf "find db station '%s' %.1f for wk station '%s', matchkind %A" db.name db.km wk.name mk
                    |> lines.Add
                | ResultOfOpPoint.Failure p ->
                    sprintf "*** failed to find station for position '%s' %A" p.name p.km
                    |> lines.Add)

        let s = String.concat "\n" lines

        System.IO.File.WriteAllText(
            "./dump/"
            + title
            + "-"
            + strecke.nummer.ToString()
            + ".txt",
            s
        )

    let private isDbRouteComplete results (dbStations: DbOpPointOfRoute []) =
        let dbFirst = dbStations.[0]
        let dbLast = dbStations.[dbStations.Length - 1]

        let foundFirst =
            results
            |> ResultOfOpPoint.exists (fun (db, _, _) -> db.km = dbFirst.km)

        let foundLast =
            results
            |> ResultOfOpPoint.exists (fun (db, _, _) -> db.km = dbLast.km)

        foundFirst && foundLast

    let private existsWkStationInResultSuccess (station: string) results =
        results
        |> Array.exists
            (fun result ->
                match result with
                | ResultOfOpPoint.Success (_, wk, _) when wk.name = station -> true
                | _ -> false)

    let private maybeReplaceResultkind (strecke: RouteInfo) (resultOfRoute: ResultOfRoute) =
        let candidate =
            AdhocReplacements.adhocResultKindChanges
            |> Array.tryFind (fun (title, route, _) -> title = strecke.title && route = strecke.nummer)

        match candidate with
        | Some (_, _, rk) -> { resultOfRoute with resultKind = rk }
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
        |> Seq.filter
            (fun s ->
                isStation s.symbols checkIsShutdownBhfSymbolTypes
                && isShutdownStation s.symbols)
        |> Seq.length

    let private countActiveStations (stationsOfInfobox: seq<OpPointOfInfobox>) =
        stationsOfInfobox
        |> Seq.filter
            (fun s ->
                isStation s.symbols checkIsActiveBhfSymbolTypes
                && not (isShutdownStation s.symbols))
        |> Seq.length

    let private compareOpPointsOfRoute
        (title: string)
        (routesInTitle: int)
        (routeInfoOrig: RouteInfo)
        (routeInfoMatched: RouteInfo)
        (wikiStations: WkOpPointOfRoute [])
        (dbStations: DbOpPointOfRoute [])
        (stationsOfInfobox: OpPointOfInfobox [])
        =
        let resultsOfMatch =
            if wikiStations.Length > 0 && dbStations.Length > 0 then
                OpPointsMatch.matchOpPointsOfRoute routeInfoMatched wikiStations dbStations
            else
                Array.empty

        ResultOfRoute.dump title routeInfoMatched.nummer resultsOfMatch

        let countWikiStops = wikiStations.Length
        let countDbStops = dbStations.Length
        let countDbStopsFound = ResultOfOpPoint.countSuccess resultsOfMatch
        let countDbStopsNotFound = ResultOfOpPoint.countFailures resultsOfMatch
        let minmaxkm = (OpPointsMatch.getSuccessMinMaxDbKm resultsOfMatch)

        let isCompleteDbRoute =
            dbStations.Length > 0
            && isDbRouteComplete resultsOfMatch dbStations

        let resultkind =
            ResultKind.getResultKind
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

    let private printResult
        (title: string)
        (streckeMatched: RouteInfo)
        (wikiStations: WkOpPointOfRoute [])
        (stationsOfInfobox: OpPointOfInfobox [])
        showDetails
        (resultOfRoute, resultsOfMatch)
        =
        if (showDetails) then
            dump title streckeMatched stationsOfInfobox wikiStations resultsOfMatch

            printfn
                "countActiveStations %d countShutdownStations %d"
                (countActiveStations stationsOfInfobox)
                (countShutdownStations stationsOfInfobox)

            printfn "see dumps ./dump/%s-%d.txt" title streckeMatched.nummer

        ResultOfRoute.printResultOfRoute showDetails resultOfRoute

    let private findRouteInfoInTemplatesWithParameter (templates: seq<Template>) title showDetails =
        match RouteInfo.findRouteInfoInTemplates templates title with
        | Some routeInfos ->
            if routeInfos.Length = 0 then
                ResultOfRoute.printResultOfRoute showDetails (ResultOfRoute.create title 0 0 ResultKind.RouteParameterNotParsed)

            routeInfos
        | None ->
            ResultOfRoute.printResultOfRoute showDetails (ResultOfRoute.create title 0 0 ResultKind.RouteParameterEmpty)
            List.empty

    let private difference (ri0: list<RouteInfo>) (ri1: list<RouteInfo>) =
        Set.difference (Set ri0) (Set ri1) |> Set.toList

    let private isPassengerRoute (routeInfo: RouteInfo) =
        not (
            routeInfo.railwayGuide.IsSome
            && routeInfo.railwayGuide.Value.Contains "nur Güterverkehr"
        )
        && DbData.checkPersonenzugStreckenutzung routeInfo.nummer

    let private findPassengerRouteInfoInTemplates
        (routeInfosFromParameter: List<RouteInfo>)
        title
        showDetails
        (loadRoute: int -> DbOpPointOfRoute [])
        =
        let passengerRoutes =
            routeInfosFromParameter
            |> List.filter isPassengerRoute

        if routeInfosFromParameter.Length > passengerRoutes.Length then
            difference routeInfosFromParameter passengerRoutes
            |> Seq.iter
                (fun route ->
                    ResultOfRoute.printResultOfRoute
                        showDetails
                        (ResultOfRoute.create
                            title
                            route.nummer
                            routeInfosFromParameter.Length
                            ResultKind.RouteIsNoPassengerTrain)

                    let dbStations = loadRoute route.nummer
                    ResultOfRoute.dump title route.nummer (ResultOfRoute.toResultOfStation dbStations))

        passengerRoutes

    let compare showDetails title (loadRoute: int -> DbOpPointOfRoute []) templates =
        let stationsOfInfobox =
            templates
            |> Array.map OpPointOfInfobox.findStationOfInfobox
            |> Array.choose id

        OpPointOfInfobox.dump title stationsOfInfobox

        let routeInfosFromParameter =
            findRouteInfoInTemplatesWithParameter templates title showDetails

        let routeInfos =
            findPassengerRouteInfoInTemplates routeInfosFromParameter title showDetails loadRoute

        routeInfos
        |> Seq.iter
            (fun route ->
                let routeMatched =
                    OpPointOfRoute.matchRouteInfoOpPoints route stationsOfInfobox (routeInfos.Length = 1)

                let dbStations = loadRoute routeMatched.nummer

                let wikiStations =
                    if dbStations.Length > 0 then
                        OpPointOfRoute.findOpPointsOfRoute routeMatched stationsOfInfobox
                    else
                        Array.empty

                OpPointOfRoute.dump title routeMatched wikiStations

                compareOpPointsOfRoute
                    title
                    routeInfosFromParameter.Length
                    route
                    routeMatched
                    wikiStations
                    dbStations
                    stationsOfInfobox
                |> printResult title routeMatched wikiStations stationsOfInfobox showDetails)
