namespace WikitextRouteDiagrams

open System.Text.RegularExpressions

open Microsoft.FSharp.Reflection

open DiffMatchPatch

// fsharplint:disable RecordFieldNames

/// result of route match
type ResultOfRoute =
    { route: int
      title: string
      routesInTitle: int
      fromToNameOrig: string []
      fromToNameMatched: string []
      fromToKm: float []
      resultKind: ResultKind
      countWikiStops: int
      countDbStops: int
      countDbStopsFound: int
      countDbStopsNotFound: int
      railwayGuide: string
      isCompleteDbRoute: bool }

/// view of ResultOfOpPoint.Success
type DbWkOpPointOfRoute =
    { dbname: string
      dbkm: float
      wkname: string
      wkkms: float []
      matchkind: MatchKind }

module ResultOfRoute =

    module DataRoute =
        let private toMap (title: string) (route: int) =
            Map
                .empty
                .Add("title", title)
                .Add("route", route.ToString())

        let collection = "ResultOfRoute"

        let insert title route value =
            DataAccess.typedCollectionInsert<ResultOfRoute> collection (toMap title route) value

        let delete title =
            DataAccess.collectionDelete collection (Map.empty.Add("title", title))

        let query title route =
            DataAccess.typedCollectionQuery<ResultOfRoute> collection (toMap title route)

        let queryAll () =
            DataAccess.toJsonArray (DataAccess.collectionQuery collection Map.empty)

    module DataOpPoints =
        let private toMap (title: string) (route: int) =
            Map
                .empty
                .Add("title", title)
                .Add("route", route.ToString())

        let collection = "DbWkStationOfRoute"

        let insert title route value =
            DataAccess.typedCollectionInsert<list<DbWkOpPointOfRoute>> collection (toMap title route) value

        let query title route =
            DataAccess.typedCollectionQuery<list<DbWkOpPointOfRoute>> collection (toMap title route)

        let querysAsStrings title route =
            DataAccess.collectionQuery collection (toMap title route)

    let create title route routesInTitle resultKind =
        { route = route
          title = title
          routesInTitle = routesInTitle
          fromToNameOrig = [||]
          fromToNameMatched = [||]
          fromToKm = [||]
          countWikiStops = 0
          countDbStops = 0
          countDbStopsFound = 0
          countDbStopsNotFound = 0
          resultKind = resultKind
          railwayGuide = ""
          isCompleteDbRoute = false }

    let showComparisonResults () =
        let results =
            Serializer.Deserialize<ResultOfRoute []>(DataRoute.queryAll ())

        printfn
            "distinct routes count: %d"
            (results |> Array.countBy (fun r -> r.route))
                .Length

        printfn
            "articles count : %d"
            (results |> Array.countBy (fun r -> r.title))
                .Length

        for case in FSharpType.GetUnionCases typeof<ResultKind> do
            let rk =
                FSharpValue.MakeUnion(case, [||]) :?> ResultKind

            printfn
                "ResultKind: %s %d"
                case.Name
                (results
                 |> Array.filter (fun r -> r.resultKind = rk))
                    .Length

    let private showMatchKindStatistic (mk: MatchKind) (l: List<MatchKind * int>) =
        match l |> List.tryFind (fun (mk0, _) -> mk = mk0) with
        | Some (mk, len) -> printfn "%A %d" mk len
        | None -> ()

    let showNotFoundStatistics () =
        let groupsMatchingsOfDbWkOpPoints =
            AdhocReplacements.Comparer.matchingsOfDbWkOpPoints
            |> Array.groupBy (fun (_, r, _, _) -> r)
            |> Array.map (fun (k, _) -> k)

        printfn "matchingsOfDbWkOpPoints, entries %d" AdhocReplacements.Comparer.matchingsOfDbWkOpPoints.Length
        printfn "matchingsOfDbWkOpPoints, routes %d" groupsMatchingsOfDbWkOpPoints.Length

        let groupsNonexistentWkOpPoints =
            AdhocReplacements.Comparer.nonexistentWkOpPoints
            |> Array.groupBy (fun (_, r, _) -> r)
            |> Array.map (fun (k, _) -> k)

        printfn "nonexistentWkOpPoints, entries %d" AdhocReplacements.Comparer.nonexistentWkOpPoints.Length
        printfn "nonexistentWkOpPoints, routes %d" groupsNonexistentWkOpPoints.Length

        let routes =
            Array.concat [ groupsMatchingsOfDbWkOpPoints
                           groupsNonexistentWkOpPoints ]
            |> Array.distinct

        printfn "total routes %d" routes.Length

    type SubstringMatch =
        { title: string
          route: int
          dbname: string
          dbkm: float
          wkname: string
          matchkind: MatchKind } // ds100

    let loadDiffMatchPatch s1 s2 =
        let d = diff_match_patch ()
        let diffs = d.diff_main (s1, s2)
        d.diff_cleanupSemantic (diffs)
        diffs |> Seq.iter (fun s -> printfn "diffs: %A" s)

    let loadRoutesOfResultKind (rk: ResultKind option) =
        let results =
            Serializer.Deserialize<ResultOfRoute []>(DataRoute.queryAll ())

        [ for r in results do
            if rk.IsNone || r.resultKind = rk.Value then
                yield r.route ]
        |> List.distinct
        |> List.sort

    let loadSubstringMatches () =
        let results =
            Serializer.Deserialize<ResultOfRoute []>(DataRoute.queryAll ())

        let groups =
            [ SameSubstring
              SameSubstringNotDistance
              EqualWithoutIgnoredNotDistance
              EqualWithoutIgnored
              EqualWithoutParentheses
              EqualOrderChanged
              StartsWith
              StartsWithNotDistance
              EndsWith
              EndsWithNotDistance ]

        [ for r in results do
            if r.resultKind = ResultKind.WikidataFoundInDbData
               || r.resultKind = ResultKind.WikidataNotFoundInDbData then
                for s in DataOpPoints.query r.title r.route do
                    yield! s |> List.map (fun s -> (r.title, r.route, s)) ]
        |> List.filter (fun (_, _, op) -> groups |> List.exists ((=) op.matchkind))
        |> List.map
            (fun (t, r, op) ->
                { title = t
                  route = r
                  dbname = op.dbname
                  dbkm = op.dbkm
                  wkname = op.wkname
                  matchkind = op.matchkind })
        |> Serializer.Serialize

    let showMatchKindStatistics verbose =
        let results =
            Serializer.Deserialize<ResultOfRoute []>(DataRoute.queryAll ())

        let routesAndStations =
            [ for r in results do
                if r.resultKind = ResultKind.WikidataFoundInDbData
                   || r.resultKind = ResultKind.WikidataNotFoundInDbData then
                    for s in DataOpPoints.query r.title r.route do
                        yield! s |> List.map (fun s -> (r.route, s)) ]

        let numExamplesPerRoute = 3

        let examples =
            routesAndStations
            |> List.groupBy (fun (r, s) -> s.matchkind)
            |> List.map
                (fun (k, l) ->
                    if verbose then
                        printfn
                            "kind %A %A"
                            k
                            (if numExamplesPerRoute > l.Length then
                                 List.empty
                             else
                                 ((List.take numExamplesPerRoute l)
                                  |> List.map (fun (r, s) -> (r, s.dbname, s.wkname))))

                    (k, l.Length))

        printfn "MatchKindStatistics, total: %d" routesAndStations.Length

        for case in FSharpType.GetUnionCases typeof<MatchKind> do
            let mk =
                FSharpValue.MakeUnion(case, [||]) :?> MatchKind

            showMatchKindStatistic mk examples

        let groups =
            [ [ EqualShortNames
                EqualShortNamesNotDistance ]
              [ EqualNames; EqualtNamesNotDistance ]
              [ SameSubstring
                SameSubstringNotDistance
                EqualWithoutIgnoredNotDistance
                EqualWithoutIgnored
                EqualWithoutParentheses
                EqualOrderChanged
                StartsWith
                StartsWithNotDistance
                EndsWith
                EndsWithNotDistance
                EqualDistanceShortSubstring ]
              [ EqualBorder; EqualBorderNotDistance ]
              [ IgnoredDbOpPoint
                IgnoredWkOpPoint
                SpecifiedMatch ] ]

        printfn "MatchKindStatistics of groups"

        for g in groups do
            let sum =
                examples
                |> List.filter (fun (mk, _) -> g |> List.exists ((=) mk))
                |> List.fold (fun s (_, len) -> s + len) 0

            printfn "%A %d" g.[0] sum

        let titlesAndRoutesAndStations =
            [ for r in results do
                if r.resultKind = ResultKind.WikidataFoundInDbData
                   || r.resultKind = ResultKind.WikidataNotFoundInDbData then
                    for s in DataOpPoints.query r.title r.route do
                        yield! s |> List.map (fun s -> (r.title, r.route, s)) ]

        let routesOnlyEqualNames =
            titlesAndRoutesAndStations
            |> List.groupBy (fun (t, r, s) -> (t, r))
            |> List.filter
                (fun (r, l) ->
                    l
                    |> List.forall
                        (fun (_, _, s) ->
                            match s.matchkind with
                            | EqualNames
                            | EqualtNamesNotDistance
                            | EqualShortNames
                            | EqualShortNamesNotDistance -> true
                            | _ -> false))

        printfn "routesOnlyEqualNames: count %d" routesOnlyEqualNames.Length

    let toResultOfStation (stations: seq<DbOpPointOfRoute>) =
        stations |> Seq.map (fun db -> ResultOfOpPoint.Failure(db))

    let printResultOfRoute showDetails (resultOfRoute: ResultOfRoute) =
        if (showDetails) then
            if resultOfRoute.fromToNameOrig.Length = 2
               && resultOfRoute.resultKind = ResultKind.StartStopOpPointsNotFound then
                printfn
                    "(\"%s\", %d, \"%s\", \"\")"
                    resultOfRoute.title
                    resultOfRoute.route
                    resultOfRoute.fromToNameOrig.[0]

                printfn
                    "(\"%s\", %d, \"%s\", \"\")"
                    resultOfRoute.title
                    resultOfRoute.route
                    resultOfRoute.fromToNameOrig.[1]

            printfn
                "%A"
                { resultOfRoute with
                      railwayGuide = "..." }

        DataRoute.insert resultOfRoute.title resultOfRoute.route resultOfRoute
        |> ignore


    let dump (title: string) (route: int) (results: seq<ResultOfOpPoint>) =
        let both =
            results
            |> Seq.map
                (fun result ->
                    match result with
                    | ResultOfOpPoint.Failure db ->
                        { dbname = db.name
                          dbkm = db.km
                          wkname = ""
                          wkkms = [||]
                          matchkind = MatchKind.Failed }
                    | ResultOfOpPoint.Success (db, wk, mk) ->
                        { dbname = db.name
                          dbkm = db.km
                          wkname = wk.name
                          wkkms = wk.kms
                          matchkind = mk })
            |> Seq.toList

        DataOpPoints.insert title route both
        |> ignore
