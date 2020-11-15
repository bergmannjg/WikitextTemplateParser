/// type of matching result
module ResultsOfMatch

open DbData
open Types
open StationsOfRoute

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

type ResultOfStation =
    | Success of DbStationOfRoute * StationOfRoute
    | Failure of DbStationOfRoute

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
    | Some v ->
        let index = v.IndexOf "<br" // maybe valid
        if index > 1 then
            v.Substring(0, index - 1)
            |> Seq.forall System.Char.IsDigit
        else
            v |> Seq.forall System.Char.IsDigit
    | None -> false

let guessRouteIsShutdown (railwayGuide: string option) =
    match railwayGuide with
    | Some v -> v.Contains "ehem" || v.Contains "alt"
    | None -> false

let getResultKind countWikiStops
                  countDbStops
                  countDbStopsFound
                  countDbStopsNotFound
                  (railwayGuide: string option)
                  (unmatched: bool)
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
        if guessRailwayGuideIsValid railwayGuide then
            NoDbDataFoundWithRailwayGuide
        else
            NoDbDataFoundWithoutRailwayGuide
    else
        Undef

let existsInDbSuccessResults (pred: DbStationOfRoute -> bool) (results: ResultOfStation []) =
    results
    |> Array.exists (fun result ->
        match result with
        | Success (db, _) -> pred (db)
        | _ -> false)

let getSuccessMinMaxDbKm (results: ResultOfStation []) =
    let dbkm =
        results
        |> Array.map (fun result ->
            match result with
            | Success (db, _) -> Some(db.km)
            | _ -> None)
        |> Array.choose id

    if dbkm.Length > 0 then
        let dbMin = dbkm |> Array.min
        let dbMax = dbkm |> Array.max
        [| dbMin; dbMax |]
    else
        [| 0.0; 0.0 |]

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
    | _ -> [||]

let showResults (path: string) =
    if System.IO.File.Exists path then
        let text = System.IO.File.ReadAllText path

        let results =
            Serializer.Deserialize<ResultOfRoute []>(text)

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
    else
        fprintfn stderr "file not found: %s" path

type StationOfDbWk =
    { dbname: string
      dbkm: float
      wkname: string
      wkkms: float [] }

let toResultOfStation (stations: DbStationOfRoute []) =
    stations |> Array.map (fun db -> Failure(db))

let dump (title: string) (route: int) (results: ResultOfStation []) =
    let both =
        results
        |> Array.map (fun result ->
            match result with
            | Failure db ->
                { dbname = db.name
                  dbkm = db.km
                  wkname = ""
                  wkkms = [||] }
            | Success (db, wk) ->
                { dbname = db.name
                  dbkm = db.km
                  wkname = wk.name
                  wkkms = wk.kms })

    let json =
        (Serializer.Serialize<StationOfDbWk []>(both))

    System.IO.File.WriteAllText
        ("./dump/"
         + title
         + "-"
         + route.ToString()
         + "-StationOfDbWk.json",
         json)
