/// type of matching result
module ResultsOfMatch

open StationsOfRoute
open DbData

type ResultKind =
    | WikidataFound
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
      fromToName: string []
      fromToKm: float []
      resultKind: ResultKind
      countWikiStops: int
      countDbStops: int
      countDbStopsNotFound: int }

type ResultOfStation =
    | Success of DbStationOfRoute * StationOfRoute
    | Failure of DbStationOfRoute

let createResult title route resultKind =
    { route = route
      title = title
      fromToName = [||]
      fromToKm = [||]
      countWikiStops = 0
      countDbStops = 0
      countDbStopsNotFound = 0
      resultKind = resultKind }

let getResultKind noStationsFound countWikiStops countDbStops countDbStopsNotFound =
    let dbStopsWithRoute = countDbStops > 1
    if noStationsFound && dbStopsWithRoute then
        StartStopStationsNotFound
    else if countWikiStops > 0
            && dbStopsWithRoute
            && countDbStopsNotFound = 0 then
        WikidataFound
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
             |> Array.filter (fun r -> r.resultKind = ResultKind.WikidataFound)).Length
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
