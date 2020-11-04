/// match wiki station with db station
module StationMatch

open StationsOfRoute
open DbData
open System.Text.RegularExpressions

/// see http://www.fssnip.net/bj/title/Levenshtein-distance
let private levenshtein (word1: string) (word2: string) =
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

/// no strict matching af names, assuming the distance check has succeeded
let private matchStationName (wikiName: string) (dbName: string) =
    let nomatches = [ "Hbf"; "Pbf"; "Vorbahnhof"; "Awanst"; "Abzw" ]

    let sameSubstring (s0: string) (s1: string) =
        let checkchars = 5
        s0.Length
        >= checkchars
        && s1.Length >= checkchars
        && s0.Substring(0, checkchars) = s1.Substring(0, checkchars)

    let wikiName0 =
        nomatches
        |> List.fold (fun (x: string) y -> x.Replace(y, "").Trim()) wikiName

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

/// the distance matches, if any of the wikiDistances matches with the dbDistance
let private matchStationDistance (wikiDistances: float []) (dbDistance: float) =
    wikiDistances
    |> Array.exists (fun d -> abs (dbDistance - d) < 1.0)

let getMatchedStation (wikiStation: StationOfRoute) (dbStation: DbStationOfRoute) =
    if (matchStationDistance wikiStation.kms dbStation.km
        && matchStationName wikiStation.name dbStation.name) then
        Some(dbStation, wikiStation)
    else
        None
