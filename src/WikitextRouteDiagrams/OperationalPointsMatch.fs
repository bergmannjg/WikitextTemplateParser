namespace WikitextRouteDiagrams

/// match wiki and db operational points
module OpPointsMatch =
    let private getBestMatch (matches: (DbOpPointOfRoute * WkOpPointOfRoute * MatchKind) []) =
        if matches.Length > 1 then
            let sorted =
                matches
                |> Array.sortWith OpPointMatch.compareMatch

            sorted.[0]
        else
            matches.[0]

    let private findStation phase (wikiStations: WkOpPointOfRoute []) (dbStation: DbOpPointOfRoute) =
        let res =
            wikiStations
            |> Array.map
                (fun b ->
                    if phase = 1 then
                        OpPointMatch.matchesWkStationWithDbStationPhase1 b dbStation
                    else if phase = 2 then
                        OpPointMatch.matchesWkStationWithDbStationPhase2 b dbStation
                    else
                        OpPointMatch.matchesWkStationWithDbStationPhase3 b dbStation)
            |> Array.choose id

        if res.Length = 0 then
            ResultOfOpPoint.Failure(dbStation)
        else
            ResultOfOpPoint.Success(getBestMatch res)

    let private getCandidate (l: ((DbOpPointOfRoute * WkOpPointOfRoute * MatchKind) [])) =
        let sorted =
            l
            |> Array.sortWith (OpPointMatch.compareMatchForDistance 0.1)

        sorted.[0]

    let private souldMapToFailure
        (db: DbOpPointOfRoute)
        (wk: WkOpPointOfRoute)
        (candidates: (DbOpPointOfRoute * WkOpPointOfRoute * MatchKind) [])
        =
        match candidates
              |> Array.tryFind (fun (_, c_wk, _) -> wk.name = c_wk.name) with
        | Some (c_db, _, _) -> db.name <> c_db.name
        | None -> false

    /// each WkStation should match with at most one DbStation
    /// if there are more than one, choose the matching with the best MatchKind
    let private removeDoubleWkStations (results: ResultOfOpPoint []) =
        let candidates =
            results
            |> ResultOfOpPoint.filteredMap (fun (db, _, _) -> db.STELLE_ART <> RInfData.StelleArtGrenze) id
            |> Array.groupBy (fun (db, wk, mk) -> (wk.name, wk.kms))
            |> Array.filter (fun ((s, kms), v) -> s.Length > 0 && v.Length > 1)
            |> Array.map (fun (k, v) -> getCandidate v)

        if candidates.Length = 0 then
            results
        else
            results
            |> ResultOfOpPoint.mapToFailure (fun (db, wk, _) -> souldMapToFailure db wk candidates)


    let private checkDbDataInWikiDataPhase1
        (route: RouteInfo)
        (wikiStations: WkOpPointOfRoute [])
        (dbStations: DbOpPointOfRoute [])
        =
        dbStations
        |> Array.map (fun p -> findStation 1 wikiStations p)
        |> removeDoubleWkStations


    let getRestOfWikiStations results (wikiStations: WkOpPointOfRoute []) =
        wikiStations
        |> Array.filter (fun p -> not (ResultOfOpPoint.exists (fun (_, wk, _) -> wk.name = p.name && wk.kms = p.kms) results))

    let private checkDbDataInWikiDataPhase2 (route: RouteInfo) (wikiStations: WkOpPointOfRoute []) results =
        let restOfWikiStations =
            getRestOfWikiStations results wikiStations

        results
        |> Array.map
            (fun r ->
                match r with
                | ResultOfOpPoint.Success _ -> r
                | ResultOfOpPoint.Failure p -> findStation 2 restOfWikiStations p)
        |> removeDoubleWkStations

    let private checkDbDataInWikiDataPhase3 (route: RouteInfo) (wikiStations: WkOpPointOfRoute []) results =
        let restOfWikiStations =
            getRestOfWikiStations results wikiStations

        results
        |> ResultOfOpPoint.mapFailureWithMatchKindContext
            (function
            | Some (mk1), Some (mk2) ->
                OpPointMatch.isDistanceMatchKind mk1
                && OpPointMatch.isDistanceMatchKind mk2
            | Some (mk1), None -> OpPointMatch.isDistanceMatchKind mk1
            | _ -> false)
            (fun p -> findStation 3 restOfWikiStations p)
        |> removeDoubleWkStations

    let private tryFindDbOpPointReplacement
        name
        (route: RouteInfo)
        (replacements: (string * int * string * string) [])
        =
        match (replacements
               |> Array.tryFind
                   (fun (t, r, dbname, _) ->
                       route.title = t
                       && route.nummer = r
                       && dbname = name)) with
        | Some (_, _, _, wkname) -> Some wkname
        | None -> None

    let private create db wkname (wikiStations: WkOpPointOfRoute []) =
        let kms =
            match wikiStations
                  |> Array.tryFind (fun s -> s.name = wkname) with
            | Some s -> s.kms
            | None -> [||]

        ResultOfOpPoint.Success(
            db,
            { kms = kms
              name = wkname
              shortname = "" },
            MatchKind.SpecifiedMatch
        )

    /// replace in entries with dbname entry of WkOpPointOfRoute with wkname
    /// replacements(title,route,dbname,wkname)
    let private mapReplacements route wikiStations (replacements: (string * int * string * string) []) results =
        results
        |> ResultOfOpPoint.mapDbOpPoint
            (fun db -> tryFindDbOpPointReplacement db.name route replacements)
            (fun db wkname -> create db wkname wikiStations)

    let private emptyWkOpPoint =
        { kms = [||]
          name = "---"
          shortname = "" }

    let private mapNonexistentStationsInFailures
        (route: RouteInfo)
        (nonexistentStations: (string * int * string) [])
        (mk: MatchKind)
        results
        =
        results
        |> ResultOfOpPoint.mapToSuccess
            (fun db ->
                nonexistentStations
                |> Array.exists (fun (t, r, s) -> route.title = t && route.nummer = r && s = db.name))
            (fun _ -> (emptyWkOpPoint, mk))

    let private mapNonexistentPrefixInFailures
        (route: RouteInfo)
        (prefix: string)
        (stelleArt: string)
        (mk: MatchKind)
        results
        =
        results
        |> ResultOfOpPoint.mapToSuccess
            (fun db ->
                db.name.StartsWith prefix
                || db.STELLE_ART = stelleArt)
            (fun _ -> (emptyWkOpPoint, mk))

    let private filterResultsOfRouteWithRouteInfo (route: RouteInfo) (results: ResultOfOpPoint []) =
        let maybefrom =
            results
            |> ResultOfOpPoint.tryFindIndex (fun (_, wk, _) -> wk.name = route.von)

        let maybeto =
            results
            |> ResultOfOpPoint.tryFindIndex (fun (_, wk, _) -> wk.name = route.bis)

        match maybefrom, maybeto with
        | Some (indexFrom), Some (indexTo)
        | Some (indexTo), Some (indexFrom) when indexFrom < indexTo ->
            results
            |> Array.mapi
                (fun i v ->
                    if i >= indexFrom && i <= indexTo then
                        Some v
                    else
                        None)
            |> Array.choose id
        | _ -> Array.empty

    let private getMinMax (floats: seq<float>) =
        if Seq.isEmpty floats then
            [| 0.0; 0.0 |]
        else
            let dbMin = floats |> Seq.min
            let dbMax = floats |> Seq.max
            [| dbMin; dbMax |]

    let getSuccessMinMaxDbKm (results: array<ResultOfOpPoint>) =
        results
        |> ResultOfOpPoint.filteredMap (fun (_, _, mk) -> mk <> SameSubstringNotDistance) (fun (db, _, _) -> db.km)
        |> getMinMax

    let private filterResultsOfRouteWithKm (route: RouteInfo) (results: ResultOfOpPoint []) =
        let fromToKm = getSuccessMinMaxDbKm results // assumes start/stop of route is in success array

        match fromToKm with
        | [| fromKm; toKm |] when fromKm = toKm -> results
        | [| fromKm; toKm |] ->
            results
            |> Array.filter
                (fun result ->
                    match result with
                    | ResultOfOpPoint.Failure s -> (s.km) >= fromKm && (s.km) <= toKm
                    | _ -> true)
        | _ -> Array.empty

    /// filter results outside of current route
    let private filterResultsOfRoute (route: RouteInfo) (results: ResultOfOpPoint []) =
        let r =
            filterResultsOfRouteWithRouteInfo route results // assumes start/stop of route is in success array

        if r.Length = 0 then
            filterResultsOfRouteWithKm route results
        else
            r

    let matchOpPointsOfRoute (route: RouteInfo) (wikiStations: WkOpPointOfRoute []) (dbStations: DbOpPointOfRoute []) =
        dbStations
        |> checkDbDataInWikiDataPhase1 route wikiStations
        |> checkDbDataInWikiDataPhase2 route wikiStations
        |> checkDbDataInWikiDataPhase3 route wikiStations
        |> filterResultsOfRoute route
        |> mapReplacements route wikiStations AdhocReplacements.Comparer.matchingsOfDbWkOpPoints
        |> mapNonexistentPrefixInFailures route "StrUeb" "Weiche" IgnoredDbOpPoint
        |> mapNonexistentStationsInFailures route AdhocReplacements.Comparer.nonexistentWkOpPoints IgnoredWkOpPoint

