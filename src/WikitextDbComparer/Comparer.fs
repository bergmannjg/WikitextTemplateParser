module Comparer

open Ast
open PrecodedStation
open Stations
open DbData
open System.Text.RegularExpressions

type ResultKind =
    | WikidataFound
    | WikidataNotFoundInTemplates
    | WikidataNotFoundInDbData
    | NoDbDataFound
    | Undef

type ResultOfRoute =
    { route: int
      title: string
      fromToName: string []
      fromToKm: float []
      countWikiStops: int
      countDbStops: int
      countDbStopsNotFound: int
      resultKind: ResultKind }

type ResultOfStation =
    | Success of Station * BetriebsstelleRailwayRoutePosition
    | Failure of BetriebsstelleRailwayRoutePosition

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

let matchBahnhof (wikiBahnhof: Station) (position: BetriebsstelleRailwayRoutePosition) =
    let dbkm = getKMI2Float position.KM_I
    (abs (dbkm - wikiBahnhof.km) < 1.0)
    && matchBahnhofName wikiBahnhof.name position.BEZEICHNUNG

let findBahnhof (wikiBahnhöfe: Station []) (position: BetriebsstelleRailwayRoutePosition) =
    let res =
        wikiBahnhöfe
        |> Array.filter (fun b -> matchBahnhof b position)

    if res.Length = 0 then Failure(position) else Success(res.[0], position)

// filter results outside of current route
let filterResults (fromToKm: float []) (results: ResultOfStation []) =
    match fromToKm with
    | [| fromKm; toKm |] ->
        results
        |> Array.filter (fun result ->
            match result with
            | Failure p ->
                (getKMI2Float p.KM_I)
                >= fromKm
                && (getKMI2Float p.KM_I) <= toKm
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

let checkDbDataInWikiData (strecke: int)
                          (wikiBahnhöfe: Station [])
                          (dbRoutePositions: BetriebsstelleRailwayRoutePosition [])
                          =
    dbRoutePositions
    |> Array.map (fun p -> findBahnhof wikiBahnhöfe p)
    |> filterResults (getMinMaxKm wikiBahnhöfe)

let countResultFailuers results =
    results
    |> Array.filter (fun result ->
        match result with
        | Failure p -> true
        | Success _ -> false)
    |> Array.length

let dump (title: string)
         (strecke: System.Collections.Generic.KeyValuePair<int, string []>)
         (fromTo: string [])
         (precodedStations: PrecodedStation [])
         (stations: Station [])
         (results: ResultOfStation [])
         =
    let lines = ResizeArray<string>()
    sprintf "fromTo: %A" fromTo |> lines.Add
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
            sprintf "*** failed to find station for position %s %s" p.BEZEICHNUNG p.KM_L
            |> lines.Add
        | _ -> ())
    let s = String.concat "\n" lines
    System.IO.File.WriteAllText
        ("./dump/"
         + title
         + "-"
         + strecke.Key.ToString()
         + ".txt",
         s)

let getResultKind countWikiStops countDbStops countDbStopsNotFound =
    if countWikiStops > 0
       && countDbStops > 0
       && countDbStopsNotFound = 0 then
        WikidataFound
    else if countWikiStops > 0
            && countDbStops > 0
            && countDbStopsNotFound > 0 then
        WikidataNotFoundInDbData
    else if countWikiStops = 0 && countDbStops > 0 then
        WikidataNotFoundInTemplates
    else if countDbStops = 0 then
        NoDbDataFound
    else
        Undef

let compare (title: string)
            (strecke: System.Collections.Generic.KeyValuePair<int, string []>)
            (useFilter: bool)
            (precodedStations: PrecodedStation [])
            (dbRoutePositions: BetriebsstelleRailwayRoutePosition [])
            showDetails
            =
    let fromTo =
        if useFilter then strecke.Value else Array.empty

    let wikiBahnhöfe =
        if dbRoutePositions.Length > 0 then filterStations fromTo precodedStations else [||]

    let mutable results = [||]
    if wikiBahnhöfe.Length > 0
       && dbRoutePositions.Length > 0 then
        results <- checkDbDataInWikiData strecke.Key wikiBahnhöfe dbRoutePositions

    let countFailuers = countResultFailuers results

    if (showDetails) then
        dump title strecke strecke.Value precodedStations wikiBahnhöfe results
        printfn "see wikitext ./cache/%s.txt" title
        printfn "see templates ./wikidata/%s.txt" title
        printfn "see templates ./wikidata/%s.txt" title
        printfn "see dumps ./dump/%s-%d.txt" title strecke.Key

    let fromToKm = (getMinMaxKm wikiBahnhöfe)
    let countWikiStops = wikiBahnhöfe.Length
    let countDbStops = dbRoutePositions.Length
    let countDbStopsNotFound = countFailuers

    let resultOfRoute =
        { route = strecke.Key
          title = title
          fromToName = strecke.Value
          fromToKm = fromToKm
          countWikiStops = countWikiStops
          countDbStops = countDbStops
          countDbStopsNotFound = countDbStopsNotFound
          resultKind = getResultKind countWikiStops countDbStops countDbStopsNotFound }

    if (showDetails) then
        printfn "%A" resultOfRoute
    else
        let s =
            Serializer.Serialize<ResultOfRoute>(resultOfRoute)

        printfn "%s," s
