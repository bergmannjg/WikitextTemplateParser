/// compare wiki data with db data
module Comparer

open RouteInfo
open StationsOfInfobox
open StationsOfRoute
open DbData
open StationMatch
open ResultsOfMatch

let findStation (wikiStations: StationOfRoute []) (dbStation: DbStationOfRoute) =
    let res =
        wikiStations
        |> Array.map (fun b -> getMatchedStation b dbStation)
        |> Array.choose id

    if res.Length = 0 then Failure(dbStation) else Success(res.[0])

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
    if (showDetails)
    then printfn "%A" resultOfRoute
    else printfn "%s" (Serializer.Serialize<ResultOfRoute>(resultOfRoute))

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
        then checkDbDataInWikiData streckeOrig.nummer wikiStations dbStations
        else [||]

    let countWikiStops = wikiStations.Length
    let countDbStops = dbStations.Length
    let countDbStopsFound = countResultSuccess results
    let countDbStopsNotFound = countResultFailures results
    let minmaxkm = (getSuccessMinMaxDbKm results)

    let isCompleteDbRoute =
        dbStations.Length > 0
        && isDbRouteComplete results dbStations

    let resultOfRoute =
        { route = streckeOrig.nummer
          title = title
          fromToNameOrig = [| streckeOrig.von; streckeOrig.bis |]
          fromToNameMatched =
              [| streckeMatched.von
                 streckeMatched.bis |]
          fromToKm = minmaxkm
          countWikiStops = countWikiStops
          countDbStops = countDbStops
          countDbStopsNotFound = countDbStopsNotFound
          resultKind = getResultKind countWikiStops countDbStops countDbStopsFound countDbStopsNotFound
          isCompleteDbRoute = isCompleteDbRoute }

    if (showDetails) then
        dump title streckeOrig precodedStations wikiStations results
        printfn "see wikitext ./cache/%s.txt" title
        printfn "see templates ./wikidata/%s.txt" title
        printfn "see dumps ./dump/%s-%d.txt" title streckeOrig.nummer

    printResult resultOfRoute showDetails
