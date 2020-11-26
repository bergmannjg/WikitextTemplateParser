/// match wiki station with db station
module StationMatch

open StationsOfRoute
open DbData
open System.Text.RegularExpressions

/// kind of match of wk station name and db sttaion name
type MatchKind =
    | Failed
    | EqualShortNames
    | EqualNames
    | StartsWith
    | EndsWith
    | EqualWithoutIgnored
    | EqualWithoutParentheses
    | Levenshtein
    | SameSubstring

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

let private nomatches =
    [ "Hbf"
      "Pbf"
      "Vorbahnhof"
      "Awanst"
      "Abzw" ]

let private sameSubstring (s0: string) (s1: string) =
    let checkchars = 5
    s0.Length
    >= checkchars
    && s1.Length >= checkchars
    && s0.Substring(0, checkchars) = s1.Substring(0, checkchars)

let replaceWithBlank (c: char) (s: string) = s.Replace(c, ' ')

/// no strict matching af names, assuming the distance check has succeeded
let private matchStationName (wikiName: string) (wikishortname: string) (dbName: string) (dbshortname: string) =
    if wikishortname.Length > 0
       && wikishortname = dbshortname then
        MatchKind.EqualShortNames
    else if System.String.Compare(wikiName, dbName, true) = 0 then
        MatchKind.EqualNames
    else
        let wikiName0 =
            nomatches
            |> List.fold (fun (x: string) y -> x.Replace(y, "").Trim()) wikiName
            |> replaceWithBlank '-'

        let dbName0 =
            nomatches
            |> List.fold (fun (x: string) y -> x.Replace(y, "")) dbName
            |> replaceWithBlank '-'

        // remove parentheses
        let regex1 = Regex(@"\([^\)]+\)")
        let wikiNamex = regex1.Replace(wikiName0, "").Trim()
        let dbNamex = regex1.Replace(dbName0, "").Trim()
        if System.String.Compare(wikiName, dbName, true) = 0
        then MatchKind.EqualNames
        else if wikiName0 = dbName0
        then MatchKind.EqualWithoutIgnored
        else if dbName0.StartsWith wikiName0
        then MatchKind.StartsWith
        else if dbName0.EndsWith wikiName0
        then MatchKind.EndsWith
        else if wikiName0.StartsWith dbName0
        then MatchKind.StartsWith
        else if wikiName0.EndsWith dbName0
        then MatchKind.EndsWith
        else if wikiNamex = dbNamex
        then MatchKind.EqualWithoutParentheses
        else if (levenshtein wikiNamex dbNamex) <= 3
        then MatchKind.Levenshtein
        else if sameSubstring wikiName0 dbName0
        then MatchKind.SameSubstring
        else MatchKind.Failed

/// the distance matches, if any of the wikiDistances matches with the dbDistance
let private matchStationDistance (wikiDistances: float []) (dbDistance: float) =
    wikiDistances
    |> Array.exists (fun d -> abs (dbDistance - d) < 1.0)

let matchesWkStationWithDbStation (wikiStation: StationOfRoute) (dbStation: DbStationOfRoute) =
    if matchStationDistance wikiStation.kms dbStation.km then
        let mk =
            matchStationName wikiStation.name wikiStation.shortname dbStation.name dbStation.KUERZEL

        if mk <> MatchKind.Failed then Some(dbStation, wikiStation, mk) else None
    else
        None
