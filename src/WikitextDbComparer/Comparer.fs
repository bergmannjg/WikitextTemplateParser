/// compare wiki data with db data
module Comparer

open RouteInfo
open StationsOfInfobox
open StationsOfRoute
open DbData
open StationMatch
open ResultsOfMatch

let compareMatch ((db0, wk0): DbStationOfRoute * StationOfRoute) ((db1, wk1): DbStationOfRoute * StationOfRoute) =
    if wk0.kms.Length = 0 || wk1.kms.Length = 0 then
        0
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

let getBestMatch (matches: (DbStationOfRoute * StationOfRoute) []) =
    if matches.Length > 1 then
        let sorted = matches |> Array.sortWith compareMatch
        sorted.[0]
    else
        matches.[0]

let findStation (wikiStations: StationOfRoute []) (dbStation: DbStationOfRoute) =
    let res =
        wikiStations
        |> Array.map (fun b -> getMatchedStation b dbStation)
        |> Array.choose id

    if res.Length = 0 then Failure(dbStation) else Success(getBestMatch res)

let checkDbDataInWikiData (strecke: int) (wikiStations: StationOfRoute []) (dbStations: DbStationOfRoute []) =
    let results =
        dbStations
        |> Array.map (fun p -> findStation wikiStations p)

    results |> filterResultsOfRoute

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
    |> Array.iter (sprintf "%A" >> lines.Add)
    sprintf "stations:" |> lines.Add
    stations |> Array.iter (sprintf "%A" >> lines.Add)
    sprintf "results:" |> lines.Add
    results
    |> Array.iter (fun result ->
        match result with
        | Success (db, wk) ->
            sprintf "find db station %s %.1f for wk station %s" db.name db.km wk.name
            |> lines.Add
        | Failure p ->
            sprintf "*** failed to find station for position %s %A" p.name p.km
            |> lines.Add)
    let s = String.concat "\n" lines
    System.IO.File.WriteAllText
        ("./dump/"
         + title
         + "-"
         + strecke.nummer.ToString()
         + ".txt",
         s)

let printResult (resultOfRoute: ResultOfRoute) showDetails =
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

let isDbRouteComplete (results: ResultOfStation []) (dbStations: DbStationOfRoute []) =
    let dbFirst = dbStations.[0]
    let dbLast = dbStations.[dbStations.Length - 1]

    let foundFirst =
        results
        |> existsInDbSuccessResults (fun db -> db.km = dbFirst.km)

    let foundLast =
        results
        |> existsInDbSuccessResults (fun db -> db.km = dbLast.km)

    foundFirst && foundLast

let existsWkStationInResultSuccess (station: string) results =
    results
    |> Array.exists (fun result ->
        match result with
        | Success (_, wk) when wk.name = station -> true
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

let compare (title: string)
            (streckeOrig: RouteInfo)
            (streckeMatched: RouteInfo)
            (wikiStations: StationOfRoute [])
            (dbStations: DbStationOfRoute [])
            (precodedStations: StationOfInfobox [])
            showDetails
            =
    let results =
        if wikiStations.Length > 0 && dbStations.Length > 0
        then checkDbDataInWikiData streckeMatched.nummer wikiStations dbStations
        else [||]

    let countWikiStops = wikiStations.Length
    let countDbStops = dbStations.Length
    let countDbStopsFound = countResultSuccess results
    let countDbStopsNotFound = countResultFailures results
    let minmaxkm = (getSuccessMinMaxDbKm results)

    let isCompleteDbRoute =
        dbStations.Length > 0
        && isDbRouteComplete results dbStations

    let resultkind =
        getResultKind
            countWikiStops
            countDbStops
            countDbStopsFound
            countDbStopsNotFound
            streckeMatched.railwayGuide
            (streckeMatched.routenameKind = Unmatched)

    let resultOfRoute =
        { route = streckeMatched.nummer
          title = title
          fromToNameOrig = [| streckeOrig.von; streckeOrig.bis |]
          fromToNameMatched =
              [| streckeMatched.von
                 streckeMatched.bis |]
          fromToKm = minmaxkm
          countWikiStops = countWikiStops
          countDbStops = countDbStops
          countDbStopsFound = countDbStopsFound
          countDbStopsNotFound = countDbStopsNotFound
          resultKind = resultkind
          railwayGuide =
              match streckeMatched.railwayGuide with
              | Some s -> s
              | None -> ""
          isCompleteDbRoute = isCompleteDbRoute }
        |> maybeReplaceResultkind streckeMatched

    ResultsOfMatch.dump title streckeMatched.nummer results

    if (showDetails) then
        dump title streckeMatched precodedStations wikiStations results
        printfn "see wikitext ./cache/%s.txt" title
        printfn "see dumps ./dump/%s-%d.txt" title streckeMatched.nummer

    printResult resultOfRoute showDetails
