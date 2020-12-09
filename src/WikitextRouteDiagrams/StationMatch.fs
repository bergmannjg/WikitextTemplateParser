/// match wiki station with db station
module StationMatch

open Types
open System.Text.RegularExpressions

let private regexTextInParentheses = Regex(@"\([^\)]+\)")

let private replaceIgnored s =
    s
    |> StringUtilities.replaceFromListToEmpty AdhocReplacements.ignoreStringsInStationname
    |> StringUtilities.replaceWithBlank '-'
    |> StringUtilities.trim

let checkEqualOrderChanged (s1: string) (s2: string) =
    if s1.Contains ' ' && s2.Contains ' ' then
        match s1.Split [| ' ' |], s2.Split [| ' ' |] with
        | ([| s1a; s1b |], [| s2a; s2b |]) when s1a = s2b && s1b = s2a -> true
        | _ -> false
    else
        false

let private prepareWkName wikiName =
    let wikiName0 = replaceIgnored wikiName
    (wikiName0, StringUtilities.replaceFromRegexToEmpty regexTextInParentheses wikiName0)

let private prepareDbName dbName =
    let dbName0 =
        replaceIgnored dbName
        |> StringUtilities.replaceFromRegexToEmpty AdhocReplacements.regexRailroadSwitch
        |> StringUtilities.replaceFromRegexToEmpty AdhocReplacements.regexChangeOfRoute

    (dbName0, StringUtilities.replaceFromRegexToEmpty regexTextInParentheses dbName0)

let private checkGrenze (wikiName: string) (dbName: string) withDistance =
    wikiName.Contains "Staatsgrenze"
    && (dbName.Contains "DB Grenze"
        || dbName.Contains "DB-Grenze"
        || withDistance && dbName.Contains "Grenze")

let private tryMatch (op: unit -> bool) (mknext: MatchKind) (mkprev: MatchKind option) =
    match mkprev with
    | Some _ -> mkprev
    | None -> if op () then Some mknext else None

/// no strict matching af names
let private matchStationName (wikiName: string) (dbName: string) withDistance =
    let (wikiName0, wikiNamex) = prepareWkName wikiName
    let (dbName0, dbNamex) = prepareDbName dbName
    let limit = if withDistance then 5 else 10

    tryMatch (fun _ -> System.String.Compare(wikiName, dbName, true) = 0) MatchKind.EqualNames None
    |> tryMatch (fun _ -> wikiName0 = dbName0) MatchKind.EqualWithoutIgnored
    |> tryMatch (fun _ -> wikiNamex = dbNamex) MatchKind.EqualWithoutParentheses
    |> tryMatch (fun _ -> checkEqualOrderChanged wikiName0 dbName0) MatchKind.EqualOrderChanged
    |> tryMatch (fun _ -> dbName0.StartsWith wikiName0) MatchKind.StartsWith
    |> tryMatch (fun _ -> dbName0.EndsWith wikiName0) MatchKind.EndsWith
    |> tryMatch (fun _ -> wikiName0.StartsWith dbName0) MatchKind.StartsWith
    |> tryMatch (fun _ -> wikiName0.EndsWith dbName0) MatchKind.EndsWith
    |> tryMatch (fun _ -> checkGrenze wikiName0 dbName0 withDistance) MatchKind.SameSubstring
    |> tryMatch (fun _ -> StringUtilities.startsWithSameSubstring wikiName0 dbName0 limit) MatchKind.SameSubstring
    |> tryMatch (fun _ -> (StringUtilities.levensht wikiNamex dbNamex) <= 3) MatchKind.Levenshtein
    |> Option.fold (fun _ mk -> mk) MatchKind.Failed

/// the distance matches, if any of the wikiDistances matches with the dbDistance
let private matchStationDistance (wikiDistances: float []) (dbDistance: float) =
    wikiDistances
    |> Array.exists (fun d -> abs (dbDistance - d) < 1.0)

let private matchkindOfWkStationWithDbStation (wikiStation: StationOfRoute) (dbStation: DbStationOfRoute) =
    if wikiStation.shortname.Length > 0
       && wikiStation.shortname = dbStation.KUERZEL then
        if matchStationDistance wikiStation.kms dbStation.km
        then EqualShortNames
        else EqualShortNamesNotDistance
    else

    if matchStationDistance wikiStation.kms dbStation.km then
        matchStationName wikiStation.name dbStation.name true
    else
        match matchStationName wikiStation.name dbStation.name false with
        | MatchKind.EqualNames
        | MatchKind.EqualWithoutIgnored -> MatchKind.EqualShortNamesNotDistance
        | MatchKind.SameSubstring -> MatchKind.SameSubstringNotDistance
        | MatchKind.EqualWithoutParentheses -> MatchKind.SameSubstringNotDistance
        | mk -> MatchKind.Failed

let matchesWkStationWithDbStation (wikiStation: StationOfRoute) (dbStation: DbStationOfRoute) =
    match matchkindOfWkStationWithDbStation wikiStation dbStation with
    | MatchKind.Failed -> None
    | mk -> Some(dbStation, wikiStation, mk)
