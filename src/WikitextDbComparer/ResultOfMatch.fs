/// type of matching result
module ResultsOfMatch

open StationsOfRoute
open DbData

type ResultKind =
    | WikidataFoundInDbData
    | StartStopStationsNotFound
    | WikidataNotFoundInTemplates
    | WikidataNotFoundInDbData
    | NoDbDataFound
    | RouteParameterNotParsed
    | RouteParameterEmpty
    | RouteIsNoPassengerTrain
    | Undef

type ResultOfRoute =
    { route: int
      title: string
      fromToNameOrig: string []
      fromToNameMatched: string []
      fromToKm: float []
      resultKind: ResultKind
      countWikiStops: int
      countDbStops: int
      countDbStopsNotFound: int
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
      countDbStopsNotFound = 0
      resultKind = resultKind
      isCompleteDbRoute = false }

let getResultKind countWikiStops countDbStops countDbStopsFound countDbStopsNotFound =
    let dbStopsWithRoute = countDbStops > 1
    if countWikiStops = 0 && dbStopsWithRoute then
        StartStopStationsNotFound
    else if countDbStopsFound > 0
            && dbStopsWithRoute
            && countDbStopsNotFound = 0 then
        WikidataFoundInDbData
    else if countWikiStops > 0
            && dbStopsWithRoute
            && countDbStopsNotFound > 0 then
        WikidataNotFoundInDbData
    else if countWikiStops = 0 && dbStopsWithRoute then
        WikidataNotFoundInTemplates
    else if not dbStopsWithRoute then
        NoDbDataFound
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
             |> Array.filter (fun r -> r.resultKind = ResultKind.WikidataNotFoundInDbData)).Length
        printfn
            "no db data found: %d"
            (results
             |> Array.filter (fun r -> r.resultKind = ResultKind.NoDbDataFound)).Length

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
