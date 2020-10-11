module Comparer

open Ast
open AstUtils
open DbData
open System.Text.RegularExpressions

type Result =
    | Success of Bahnhof * BetriebsstelleRailwayRoutePosition
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

// adhoc matchings
let matchBahnhofName (wikiName: string) (dbName: string) =
    let nomatches = [ "Hbf"; "Pbf"; "Vorbahnhof" ]

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

    wikiName0 = dbName0
    || dbName0.StartsWith wikiName0
    || dbName0.EndsWith wikiName0
    || wikiName0.StartsWith dbName0
    || wikiName0.EndsWith dbName0
    || wikiNamex = dbNamex
    || (levenshtein wikiNamex dbNamex) <= 3

let matchBahnhof (wikiBahnhof: Bahnhof) (position: BetriebsstelleRailwayRoutePosition) =
    let dbkm = getKMI2Float position.KM_I
    (abs (dbkm - wikiBahnhof.km) < 1.0)
    && matchBahnhofName wikiBahnhof.name position.BEZEICHNUNG

let findBahnhof (wikiBahnhöfe: Bahnhof []) (position: BetriebsstelleRailwayRoutePosition) =
    let res =
        wikiBahnhöfe
        |> Array.filter (fun b -> matchBahnhof b position)

    if res.Length = 0 then Failure(position) else Success(res.[0], position)

// filter results outside of current route
let filterResults ((fromKm, toKm): float * float) (results: Result []) =
    results
    |> Array.filter (fun result ->
        match result with
        | Failure p ->
            (getKMI2Float p.KM_I)
            >= fromKm
            && (getKMI2Float p.KM_I) <= toKm
        | _ -> true)

let getMinMaxKm (bahnhöfe: Bahnhof []) =
    if bahnhöfe.Length = 0 then
        (0.0, 0.0)
    else
        let fromKm =
            bahnhöfe |> Array.map (fun b -> b.km) |> Array.min

        let toKm =
            bahnhöfe |> Array.map (fun b -> b.km) |> Array.max

        (fromKm, toKm)

let checkDbDataInWikiData (strecke: int)
                          (wikiBahnhöfe: Bahnhof [])
                          (dbRoutePositions: BetriebsstelleRailwayRoutePosition [])
                          =
    let results =
        dbRoutePositions
        |> Array.map (fun p -> findBahnhof wikiBahnhöfe p)
        |> filterResults (getMinMaxKm wikiBahnhöfe)

    results
    |> Array.iter (fun result ->
        match result with
        | Failure p -> printfn "*** failed to find Bahnhof for position %s %s" p.BEZEICHNUNG p.KM_L
        | _ -> ())

    let countSucces =
        results
        |> Array.filter (fun result ->
            match result with
            | Failure p -> false
            | Success _ -> true)
        |> Array.length

    let countFailuers =
        results
        |> Array.filter (fun result ->
            match result with
            | Failure p -> true
            | Success _ -> false)
        |> Array.length

    if countFailuers = 0
    then printfn "route %d, %d stations of dbdata found in wikidata" strecke countSucces

let compare (strecke: System.Collections.Generic.KeyValuePair<int, string []>)
            (wikiBahnhöfe: Bahnhof [])
            (dbRoutePositions: BetriebsstelleRailwayRoutePosition [])
            =
    printfn
        "compare route %d %A %A, wikidata stops %d, dbdata stop %d "
        strecke.Key
        strecke.Value
        (getMinMaxKm wikiBahnhöfe)
        wikiBahnhöfe.Length
        dbRoutePositions.Length
    checkDbDataInWikiData strecke.Key wikiBahnhöfe dbRoutePositions
