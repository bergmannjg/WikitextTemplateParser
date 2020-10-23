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

let countResultFailuers results =
    results
    |> Array.filter (fun result ->
        match result with
        | Failure p -> true
        | Success _ -> false)
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
    results
    |> Array.iter (fun result ->
        match result with
        | Failure p ->
            sprintf "*** failed to find station for position %s %A" p.name p.km
            |> lines.Add
        | _ -> ())
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

let compare (title: string)
            (strecke: RouteInfo)
            (wikiStations: StationOfRoute [])
            (dbStations: DbStationOfRoute [])
            (precodedStations: StationOfInfobox [])
            showDetails
            =
    let results =
        if wikiStations.Length > 0 && dbStations.Length > 0
        then checkDbDataInWikiData strecke.nummer wikiStations dbStations
        else [||]

    let countWikiStops = wikiStations.Length
    let countDbStops = dbStations.Length
    let countDbStopsNotFound = countResultFailuers results
    let minmaxkm = (getSuccessMinMaxDbKm results)
    let noStationsFound = (minmaxkm |> Array.max) = 0.0

    let resultOfRoute =
        { route = strecke.nummer
          title = title
          fromToName = [| strecke.von; strecke.bis |]
          fromToKm = minmaxkm
          countWikiStops = countWikiStops
          countDbStops = countDbStops
          countDbStopsNotFound = countDbStopsNotFound
          resultKind = getResultKind noStationsFound countWikiStops countDbStops countDbStopsNotFound }

    if (showDetails) then
        dump title strecke precodedStations wikiStations results
        printfn "see wikitext ./cache/%s.txt" title
        printfn "see templates ./wikidata/%s.txt" title
        printfn "see dumps ./dump/%s-%d.txt" title strecke.nummer

    printResult resultOfRoute showDetails
