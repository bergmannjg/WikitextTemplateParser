/// type of matching result
module ResultsOfMatch

// fsharplint:disable RecordFieldNames 

open DbData
open Types
open StationsOfRoute
open StationMatch
open System.Text.RegularExpressions

/// result of route match
type ResultOfRoute =
    { route: int
      title: string
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

/// result of station match
type ResultOfStation =
    | Success of DbStationOfRoute * StationOfRoute * MatchKind
    | Failure of DbStationOfRoute

/// view of ResultOfStation.Success
type StationOfDbWk =
    { dbname: string
      dbkm: float
      wkname: string
      wkkms: float []
      matchkind: MatchKind }

let createResult title route resultKind =
    { route = route
      title = title
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

let guessRailwayGuideIsValid (value: string option) =
    match value with
    | Some v -> Regex("\d{3}").Match(v).Success
    | None -> false

let guessRouteIsShutdown (railwayGuide: string option) =
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
            | Success (db, _, _) -> Some(db.km)
            | _ -> None)
        |> Seq.choose id

    if Seq.isEmpty dbkm then
        [| 0.0; 0.0 |]
    else
        let dbMin = dbkm |> Seq.min
        let dbMax = dbkm |> Seq.max
        [| dbMin; dbMax |]

/// filter results outside of current route
let filterResultsOfRoute (results: ResultOfStation []) =
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

let showResults () =
    let results =
        Serializer.Deserialize<ResultOfRoute []>(DataAccess.ResultOfRoute.queryAll ())

    printfn "distinct routes count: %d" (results |> Array.countBy (fun r -> r.route)).Length
    printfn "articles count : %d" (results |> Array.countBy (fun r -> r.title)).Length
    printfn
        "route parameter empty: %d"
        (results
         |> Array.filter (fun r -> r.resultKind = ResultKind.RouteParameterEmpty)).Length
    printfn
        "route parameter not parsed: %d"
        (results
         |> Array.filter (fun r -> r.resultKind = ResultKind.RouteParameterNotParsed)).Length
    printfn
        "route is no passenger train: %d"
        (results
         |> Array.filter (fun r -> r.resultKind = ResultKind.RouteIsNoPassengerTrain)).Length
    printfn
        "start/stop stations of route not found: %d"
        (results
         |> Array.filter (fun r -> r.resultKind = ResultKind.StartStopStationsNotFound)).Length
    printfn
        "found wikidata : %d"
        (results
         |> Array.filter (fun r -> r.resultKind = ResultKind.WikidataFoundInDbData)).Length
    printfn
        "not found wikidata in templates: %d"
        (results
         |> Array.filter (fun r -> r.resultKind = ResultKind.WikidataNotFoundInTemplates)).Length
    printfn
        "not found wikidata in db data: %d"
        (results
         |> Array.filter (fun r ->
             r.resultKind = ResultKind.WikidataNotFoundInDbData
             && not  // check result of route with WikidataFoundInDbData and complete
                 (results
                  |> Array.exists (fun s ->
                      s.route = r.route
                      && s.resultKind = WikidataFoundInDbData
                      && s.isCompleteDbRoute)))).Length
    printfn
        "no db data found, but has railway guide : %d"
        (results
         |> Array.filter (fun r -> r.resultKind = ResultKind.NoDbDataFoundWithRailwayGuide)).Length
    printfn
        "route is shutdown : %d"
        (results
         |> Array.filter (fun r -> r.resultKind = ResultKind.RouteIsShutdown)).Length
    printfn
        "no db data found: %d"
        (results
         |> Array.filter (fun r -> r.resultKind = ResultKind.NoDbDataFoundWithoutRailwayGuide)).Length

    let countUndef =
        (results
         |> Array.filter (fun r -> r.resultKind = ResultKind.Undef)).Length

    if countUndef > 0
    then fprintf stderr "undef result kind unexpected, count %d" countUndef

let showMatchKindStatistic (mk: MatchKind) (l: List<MatchKind * int>) =
    match l |> List.tryFind (fun (mk0, _) -> mk = mk0) with
    | Some (mk, len) -> printfn "%A %d" mk len
    | None -> ()

let showMatchKindStatistics () =
    let results =
        Serializer.Deserialize<ResultOfRoute []>(DataAccess.ResultOfRoute.queryAll ())

    let stationsOfRoute =
        [ for r in results do
            if r.resultKind = ResultKind.WikidataFoundInDbData
               || r.resultKind = ResultKind.WikidataNotFoundInDbData then
                for s in DataAccess.DbWkStationOfRoute.query r.title r.route do
                    yield!
                        Serializer.Deserialize<StationOfDbWk []>(s)
                        |> Array.map (fun s -> (r.route, s)) ]

    let groups =
        stationsOfRoute
        |> List.groupBy (fun (r, s) -> s.matchkind)
        |> List.map (fun (k, l) ->
            printfn
                "kind %A %A"
                k
                ((List.take 3 l)
                 |> List.map (fun (r, s) -> (r, s.dbname, s.wkname)))
            (k, l.Length))

    printfn "MatchKindStatistics"
    showMatchKindStatistic MatchKind.EqualNames groups
    showMatchKindStatistic MatchKind.EqualShortNames groups
    showMatchKindStatistic MatchKind.EqualShortNamesNotDistance groups
    showMatchKindStatistic MatchKind.EqualWithoutIgnored groups
    showMatchKindStatistic MatchKind.EqualWithoutParentheses groups
    showMatchKindStatistic MatchKind.StartsWith groups
    showMatchKindStatistic MatchKind.EndsWith groups
    showMatchKindStatistic MatchKind.Levenshtein groups
    showMatchKindStatistic MatchKind.SameSubstring groups

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

    DataAccess.ResultOfRoute.insert
        resultOfRoute.title
        resultOfRoute.route
        (Serializer.Serialize<ResultOfRoute>(resultOfRoute))
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

    DataAccess.DbWkStationOfRoute.insert title route (Serializer.Serialize<list<StationOfDbWk>>(both))
    |> ignore
