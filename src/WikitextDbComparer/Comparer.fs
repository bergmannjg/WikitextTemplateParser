/// compare wiki data with db data
module Comparer

open RouteInfo
open StationsOfInfobox
open StationsOfRoute
open DbData
open StationMatch
open ResultsOfMatch

let compareMatch ((db0, wk0, _): DbStationOfRoute * StationOfRoute * MatchKind)
                 ((db1, wk1, _): DbStationOfRoute * StationOfRoute * MatchKind)
                 =
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

let getBestMatch (matches: (DbStationOfRoute * StationOfRoute * MatchKind) []) =
    if matches.Length > 1 then
        let sorted = matches |> Array.sortWith compareMatch
        sorted.[0]
    else
        matches.[0]

let findStation (wikiStations: StationOfRoute []) (dbStation: DbStationOfRoute) =
    let res =
        wikiStations
        |> Array.map (fun b -> matchesWkStationWithDbStation b dbStation)
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

let compare (title: string)
            (routeInfoOrig: RouteInfo)
            (routeInfoMatched: RouteInfo)
            (wikiStations: StationOfRoute [])
            (dbStations: DbStationOfRoute [])
            =
    let resultsOfMatch =
        if wikiStations.Length > 0 && dbStations.Length > 0
        then checkDbDataInWikiData routeInfoMatched.nummer wikiStations dbStations
        else [||]

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

    let resultOfRoute =
        { route = routeInfoMatched.nummer
          title = title
          fromToNameOrig = [| routeInfoOrig.von; routeInfoOrig.bis |]
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

let printResult (title: string)
                (streckeMatched: RouteInfo)
                (wikiStations: StationOfRoute [])
                (stationsOfInfobox: StationOfInfobox [])
                showDetails
                ((resultOfRoute, resultsOfMatch): (ResultOfRoute * ResultOfStation []))
                =
    if (showDetails) then
        dump title streckeMatched stationsOfInfobox wikiStations resultsOfMatch
        printfn "see dumps ./dump/%s-%d.txt" title streckeMatched.nummer

    printResultOfRoute showDetails resultOfRoute
