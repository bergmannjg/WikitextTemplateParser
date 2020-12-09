/// type of matching result
module ResultsOfMatch

open Types
open System.Text.RegularExpressions
open Microsoft.FSharp.Reflection

let createResult title route routesInTitle resultKind =
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

let private guessRailwayGuideIsValid (value: string option) =
    match value with
    | Some v -> Regex("\d{3}").Match(v).Success
    | None -> false

let private guessRouteIsShutdown (railwayGuide: string option) =
    match railwayGuide with
    | Some v ->
        v.StartsWith "ehem"
        || v.StartsWith "alt"
        || v.StartsWith "zuletzt"
        || v.StartsWith "ex"
        || Regex("\(\d{4}\)").Match(v).Success
    | None -> false

let getResultKind countWikiStops
                  countDbStops
                  countDbStopsFound
                  countDbStopsNotFound
                  (railwayGuide: string option)
                  (unmatched: bool)
                  (countAciveStations: int)
                  (countShutdownStations: int)
                  =
    let dbStopsWithRoute = countDbStops > 0
    if countWikiStops = 0 && dbStopsWithRoute then
        StartStopStationsNotFound
    else if dbStopsWithRoute && unmatched then
        StartStopStationsNotFound
    else if countDbStopsFound > 0
            && dbStopsWithRoute
            && countDbStopsNotFound = 0 then
        WikidataFoundInDbData
    else if guessRouteIsShutdown railwayGuide then
        RouteIsShutdown
    else if countWikiStops > 0
            && dbStopsWithRoute
            && countDbStopsNotFound > 0 then
        WikidataNotFoundInDbData
    else if countWikiStops = 0 && dbStopsWithRoute then
        WikidataNotFoundInTemplates
    else if not dbStopsWithRoute then
        if countShutdownStations
           >= 2
           && countAciveStations <= 2 then
            RouteIsShutdown
        else if guessRailwayGuideIsValid railwayGuide then
            NoDbDataFoundWithRailwayGuide
        else
            NoDbDataFoundWithoutRailwayGuide
    else
        Undef

let existsInDbSuccessResults (pred: DbStationOfRoute -> bool) (results: seq<ResultOfStation>) =
    results
    |> Seq.exists (fun result ->
        match result with
        | Success (db, _, _) -> pred (db)
        | _ -> false)

let getSuccessMinMaxDbKm (results: seq<ResultOfStation>) =
    let dbkm =
        results
        |> Seq.map (fun result ->
            match result with
            | Success (db, _, mk) when mk <> MatchKind.SameSubstringNotDistance -> Some(db.km)
            | _ -> None)
        |> Seq.choose id

    if Seq.isEmpty dbkm then
        [| 0.0; 0.0 |]
    else
        let dbMin = dbkm |> Seq.min
        let dbMax = dbkm |> Seq.max
        [| dbMin; dbMax |]

let private compareCandidate ((_, _, x): DbStationOfRoute * StationOfRoute * MatchKind)
                             ((_, _, y): DbStationOfRoute * StationOfRoute * MatchKind)
                             =
    if x = y then 0
    else if x < y then -1
    else 1

let private getCandidate (l: ((DbStationOfRoute * StationOfRoute * MatchKind) [])) =
    let sorted = l |> Array.sortWith compareCandidate
    sorted.[0]

/// each WkStation should match with at most one DbStation
/// if there are more than one, choose the matching with the best MatchKind
let private removeDoubleWkStations (results: ResultOfStation []) =
    let candidates =
        results
        |> Array.map (fun r ->
            match r with
            | Success (db, wk, mk) -> Some(db, wk, mk)
            | Failure (_) -> None)
        |> Array.choose id
        |> Array.groupBy (fun (db, wk, mk) -> wk.name)
        |> Array.filter (fun (k, v) -> k.Length > 0 && v.Length > 1)
        |> Array.map (fun (k, v) -> getCandidate v)

    if candidates.Length = 0 then
        results
    else
        results
        |> Array.map (fun r ->
            match r with
            | Success (db, wk, _) ->
                match candidates
                      |> Array.tryFind (fun (c_db, c_wk, c_mk) -> wk.name = c_wk.name) with
                | Some (c_db, c_wk, c_mk) -> if db.name = c_db.name then r else Failure db
                | None -> r
            | Failure (_) -> r)

/// filter results outside of current route
let filterResultsOfRoute (strecke: RouteInfo) (results0: ResultOfStation []) =
    let results = removeDoubleWkStations results0
    let fromToKm = getSuccessMinMaxDbKm results // assumes start/stop of route is in success array
    match fromToKm with
    | [| fromKm; toKm |] when fromKm = toKm -> results
    | [| fromKm; toKm |] ->
        results
        |> Array.filter (fun result ->
            match result with
            | Failure s -> (s.km) >= fromKm && (s.km) <= toKm
            | _ -> true)
    | _ -> Array.empty

let showComparisonResults () =
    let results =
        Serializer.Deserialize<ResultOfRoute []>(DataAccess.ResultOfRoute.queryAll ())

    printfn "distinct routes count: %d" (results |> Array.countBy (fun r -> r.route)).Length
    printfn "articles count : %d" (results |> Array.countBy (fun r -> r.title)).Length

    for case in FSharpType.GetUnionCases typeof<ResultKind> do
        let rk =
            FSharpValue.MakeUnion(case, [||]) :?> ResultKind

        printfn
            "ResultKind: %s %d"
            case.Name
            (results
             |> Array.filter (fun r -> r.resultKind = rk)).Length

let private showMatchKindStatistic (mk: MatchKind) (l: List<MatchKind * int>) =
    match l |> List.tryFind (fun (mk0, _) -> mk = mk0) with
    | Some (mk, len) -> printfn "%A %d" mk len
    | None -> ()

let showNotFoundStatistics () =
    Serializer.Deserialize<ResultOfRoute []>(DataAccess.ResultOfRoute.queryAll ())
    |> Array.filter (fun r ->
        r.resultKind = ResultKind.WikidataNotFoundInDbData)
    |> Array.groupBy (fun r -> r.countDbStopsNotFound)
    |> Array.iter (fun (k, v) -> printfn "notfound %d, count %d" k v.Length)

let showMatchKindStatistics () =
    let results =
        Serializer.Deserialize<ResultOfRoute []>(DataAccess.ResultOfRoute.queryAll ())

    let routesAndStations =
        [ for r in results do
            if r.resultKind = ResultKind.WikidataFoundInDbData
               || r.resultKind = ResultKind.WikidataNotFoundInDbData then
                for s in DataAccess.DbWkStationOfRoute.query r.title r.route do
                    yield! s |> List.map (fun s -> (r.route, s)) ]

    let numExamplesPerRoute = 3

    let examples =
        routesAndStations
        |> List.groupBy (fun (r, s) -> s.matchkind)
        |> List.map (fun (k, l) ->
            printfn
                "kind %A %A"
                k
                ((List.take numExamplesPerRoute l)
                 |> List.map (fun (r, s) -> (r, s.dbname, s.wkname)))
            (k, l.Length))

    printfn "MatchKindStatistics"
    for case in FSharpType.GetUnionCases typeof<MatchKind> do
        let mk =
            FSharpValue.MakeUnion(case, [||]) :?> MatchKind

        showMatchKindStatistic mk examples

let toResultOfStation (stations: seq<DbStationOfRoute>) =
    stations |> Seq.map (fun db -> Failure(db))

let printResultOfRoute showDetails (resultOfRoute: ResultOfRoute) =
    if (showDetails) then
        if resultOfRoute.fromToNameOrig.Length = 2
           && resultOfRoute.resultKind = Types.ResultKind.StartStopStationsNotFound then
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
        printfn "%A" resultOfRoute

    DataAccess.ResultOfRoute.insert resultOfRoute.title resultOfRoute.route resultOfRoute
    |> ignore


let dump (title: string) (route: int) (results: seq<ResultOfStation>) =
    let both =
        results
        |> Seq.map (fun result ->
            match result with
            | Failure db ->
                { dbname = db.name
                  dbkm = db.km
                  wkname = ""
                  wkkms = [||]
                  matchkind = MatchKind.Failed }
            | Success (db, wk, mk) ->
                { dbname = db.name
                  dbkm = db.km
                  wkname = wk.name
                  wkkms = wk.kms
                  matchkind = mk })
        |> Seq.toList

    DataAccess.DbWkStationOfRoute.insert title route both
    |> ignore
