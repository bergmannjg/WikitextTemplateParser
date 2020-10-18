module Comparer

open AstUtils
open PrecodedStation
open Stations
open DbData
open System.Text.RegularExpressions

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
    | Success of Station * Station
    | Failure of Station

let createResult title route resultKind =
    { route = route
      title = title
      fromToName = [||]
      fromToKm = [||]
      countWikiStops = 0
      countDbStops = 0
      countDbStopsNotFound = 0
      resultKind = resultKind }

// see http://www.fssnip.net/bj/title/Levenshtein-distance
let levenshtein (word1: string) (word2: string) =
    let preprocess =
        fun (str: string) -> str.ToLower().ToCharArray()

    let chars1, chars2 = preprocess word1, preprocess word2
    let m, n = chars1.Length, chars2.Length
    let table: int [,] = Array2D.zeroCreate (m + 1) (n + 1)
    for i in 0 .. m do
        for j in 0 .. n do
            match i, j with
            | i, 0 -> table.[i, j] <- i
            | 0, j -> table.[i, j] <- j
            | _, _ ->
                let delete = table.[i - 1, j] + 1
                let insert = table.[i, j - 1] + 1
                //cost of substitution is 2
                let substitute =
                    if chars1.[i - 1] = chars2.[j - 1] then
                        table.[i - 1, j - 1] //same character
                    else
                        table.[i - 1, j - 1] + 2

                table.[i, j] <- List.min [ delete; insert; substitute ]
    table.[m, n] //return distance

// no strict matching af names, assuming the distance check has succeeded
let matchBahnhofName (wikiName: string) (dbName: string) =
    let nomatches = [ "Hbf"; "Pbf"; "Vorbahnhof" ]

    let sameSubstring (s0: string) (s1: string) =
        let checkchars = 5
        s0.Length
        >= checkchars
        && s1.Length >= checkchars
        && s0.Substring(0, checkchars) = s1.Substring(0, checkchars)

    let wikiName0 =
        nomatches
        |> List.fold (fun (x: string) y -> x.Replace(y, "")) wikiName

    let dbName0 =
        nomatches
        |> List.fold (fun (x: string) y -> x.Replace(y, "")) dbName

    // remove parentheses
    let regex1 = Regex(@"\([^\)]+\)")
    let wikiNamex = regex1.Replace(wikiName0, "").Trim()
    let dbNamex = regex1.Replace(dbName0, "").Trim()

    wikiName = dbName
    || wikiName0 = dbName0
    || dbName0.StartsWith wikiName0
    || dbName0.EndsWith wikiName0
    || wikiName0.StartsWith dbName0
    || wikiName0.EndsWith dbName0
    || wikiNamex = dbNamex
    || (levenshtein wikiNamex dbNamex) <= 3
    || sameSubstring wikiName0 dbName0

let matchBahnhof (wikiStation: Station) (dbStation: Station) =
    (abs (dbStation.km - wikiStation.km) < 1.0)
    && matchBahnhofName wikiStation.name dbStation.name

let findBahnhof (wikiStations: Station []) (dbStation: Station) =
    let res =
        wikiStations
        |> Array.filter (fun b -> matchBahnhof b dbStation)

    if res.Length = 0 then Failure(dbStation) else Success(res.[0], dbStation)

// filter results outside of current route
let filterResults (fromToKm: float []) (results: ResultOfStation []) =
    match fromToKm with
    | [| fromKm; toKm |] ->
        results
        |> Array.filter (fun result ->
            match result with
            | Failure p -> (p.km) >= fromKm && (p.km) <= toKm
            | _ -> true)
    | _ -> [||]

let getMinMaxKm (bahnhöfe: Station []) =
    if bahnhöfe.Length = 0 then
        [| 0.0; 0.0 |]
    else
        let fromKm =
            bahnhöfe |> Array.map (fun b -> b.km) |> Array.min

        let toKm =
            bahnhöfe |> Array.map (fun b -> b.km) |> Array.max

        [| fromKm; toKm |]

let checkDbDataInWikiData (strecke: int) (wikiStations: Station []) (dbStations: Station []) =
    dbStations
    |> Array.map (fun p -> findBahnhof wikiStations p)
    |> filterResults (getMinMaxKm wikiStations)

let countResultFailuers results =
    results
    |> Array.filter (fun result ->
        match result with
        | Failure p -> true
        | Success _ -> false)
    |> Array.length

let dump (title: string)
         (strecke: Strecke)
         (precodedStations: PrecodedStation [])
         (stations: Station [])
         (results: ResultOfStation [])
         =
    let lines = ResizeArray<string>()
    sprintf "fromTo: %A" [| strecke.von; strecke.bis |]
    |> lines.Add
    sprintf "guessDistanceCoding: %A" (guessDistanceCoding precodedStations)
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
            sprintf "*** failed to find station for position %s %.1f" p.name p.km
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

let printResult (resultOfRoute: ResultOfRoute) showDetails =
    if (showDetails)
    then printfn "%A" resultOfRoute
    else printfn "%s" (Serializer.Serialize<ResultOfRoute>(resultOfRoute))

let compare (title: string)
            (strecke: Strecke)
            (wikiStations: Station [])
            (dbStations: Station [])
            (precodedStations: PrecodedStation [])
            showDetails
            =
    let results =
        if wikiStations.Length > 0 && dbStations.Length > 0
        then checkDbDataInWikiData strecke.nummer wikiStations dbStations
        else [||]

    let countWikiStops = wikiStations.Length
    let countDbStops = dbStations.Length
    let countDbStopsNotFound = countResultFailuers results
    let minmaxkm = (getMinMaxKm wikiStations)
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
