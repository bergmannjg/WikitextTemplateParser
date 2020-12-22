/// match wiki and db operational points
module OpPointMatch

open Types
open System.Text.RegularExpressions

let private defaultEqualDistance = 1.0

let private equalDistance d1 d2 maxEqualDistance = abs (d1 - d2) < maxEqualDistance

let private regexTextInParentheses = Regex(@"\([^\)]+\)")

let private replaceIgnored s =
    s
    |> StringUtilities.replaceFromListToEmpty AdhocReplacements.OpPointMatch.ignoreStringsInStationname
    |> StringUtilities.replaceWithBlank '-'
    |> StringUtilities.replaceWithBlank '.'
    |> StringUtilities.replaceWithBlank ','
    |> StringUtilities.replaceWithBlank '“'
    |> StringUtilities.replaceWithBlank '„'
    |> StringUtilities.trim

let private replaceMultipleSpaces (s: string) = s.Replace(" ", "")

let checkEqualOrderChanged (s1: string) (s2: string) =
    if s1.Contains ' ' && s2.Contains ' ' then
        match s1.Split [| ' ' |], s2.Split [| ' ' |] with
        | ([| s1a; s1b |], [| s2a; s2b |]) when s1a = s2b && s1b = s2a -> true
        | _ -> false
    else
        false

let checkStartsWith (s1: string) (s2: string) =
    s1.StartsWith s2
    && s1.Length > s2.Length
    && not (System.Char.IsLetter s1.[s2.Length])

let private prepareWkName wikiName =
    let wikiName0 = replaceIgnored wikiName

    (wikiName0,
     StringUtilities.replaceFromRegexToEmpty regexTextInParentheses wikiName0
     |> replaceMultipleSpaces)

let private prepareDbName dbName =
    let dbName0 =
        dbName
        |> StringUtilities.replaceFromRegexToEmpty AdhocReplacements.regexRailroadSwitch
        |> StringUtilities.replaceFromRegexToEmpty AdhocReplacements.regexChangeOfRoute1
        |> replaceIgnored
        |> StringUtilities.replaceFromRegexToEmpty AdhocReplacements.regexChangeOfRoute2

    (dbName0,
     StringUtilities.replaceFromRegexToEmpty regexTextInParentheses dbName0
     |> replaceMultipleSpaces)

let private checkBorder (wikiStation: WkOpPointOfRoute) (dbStation: DbOpPointOfRoute) withDistance =
    (wikiStation.name.Contains "Staatsgrenze"
     || wikiStation.name.Contains "Infrastrukturgrenze")
    && (dbStation.name.Contains "DB Grenze"
        || dbStation.name.Contains "DB-Grenze"
        || dbStation.STELLE_ART = RInfData.StelleArtGrenze
        || withDistance && dbStation.name.Contains "Grenze")

let private orElse (op: unit -> bool) (mknext: MatchKind) (mkprev: MatchKind option) =
    Option.orElseWith (fun _ -> if op () then Some mknext else None) mkprev

/// no strict matching af names
let matchStationName (wikiName: string) (dbName: string) withDistance =
    let (wikiName0, wikiNamex) = prepareWkName wikiName
    let (dbName0, dbNamex) = prepareDbName dbName
    let limit = if withDistance then 5 else 12

    None
    |> orElse (fun _ -> System.String.Compare(wikiName, dbName, true) = 0) MatchKind.EqualNames
    |> orElse (fun _ -> System.String.Compare(wikiName0, dbName0, true) = 0) MatchKind.EqualWithoutIgnored
    |> orElse (fun _ -> System.String.Compare(wikiNamex, dbNamex, true) = 0) MatchKind.EqualWithoutParentheses
    |> orElse (fun _ -> checkEqualOrderChanged wikiName0 dbName0) MatchKind.EqualOrderChanged
    |> orElse (fun _ -> checkStartsWith dbName0 wikiName0) MatchKind.StartsWith
    |> orElse (fun _ -> dbName0.EndsWith wikiName0) MatchKind.EndsWith
    |> orElse (fun _ -> checkStartsWith wikiName0 dbName0) MatchKind.StartsWith
    |> orElse (fun _ -> wikiName0.EndsWith dbName0) MatchKind.EndsWith
    |> orElse (fun _ -> StringUtilities.startsWithSameSubstring wikiName0 dbName0 limit) MatchKind.SameSubstring
    |> Option.fold (fun _ mk -> mk) MatchKind.Failed

/// the distance matches, if any of the wikiDistances matches with the dbDistance
let private matchStationDistance (wikiDistances: float []) (dbDistance: float) =
    wikiDistances
    |> Array.exists (fun d -> equalDistance dbDistance d defaultEqualDistance)

let private matchkindOfWkStationWithDbStationPhase1 (wikiStation: WkOpPointOfRoute) (dbStation: DbOpPointOfRoute) =
    if wikiStation.shortname.Length > 0
       && wikiStation.shortname = dbStation.KUERZEL then
        if matchStationDistance wikiStation.kms dbStation.km
        then EqualShortNames
        else EqualShortNamesNotDistance
    else

    if System.String.Compare(wikiStation.name, dbStation.name, true) = 0 then
        if matchStationDistance wikiStation.kms dbStation.km
        then MatchKind.EqualNames
        else MatchKind.EqualtNamesNotDistance
    else

    if checkBorder wikiStation dbStation false then
        if matchStationDistance wikiStation.kms dbStation.km
        then MatchKind.EqualBorder
        else MatchKind.EqualBorderNotDistance
    else
        MatchKind.Failed

let private matchkindOfWkStationWithDbStationPhase2 (wikiStation: WkOpPointOfRoute) (dbStation: DbOpPointOfRoute) =
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
        | MatchKind.EqualNames -> MatchKind.EqualtNamesNotDistance
        | MatchKind.EqualWithoutIgnored -> MatchKind.EqualWithoutIgnoredNotDistance
        | MatchKind.StartsWith -> MatchKind.StartsWithNotDistance
        | MatchKind.EndsWith -> MatchKind.EndsWithNotDistance
        | MatchKind.SameSubstring -> MatchKind.SameSubstringNotDistance
        | MatchKind.EqualWithoutParentheses -> MatchKind.SameSubstringNotDistance
        | mk -> MatchKind.Failed

let matchesWkStationWithDbStationPhase1 (wikiStation: WkOpPointOfRoute) (dbStation: DbOpPointOfRoute) =
    match matchkindOfWkStationWithDbStationPhase1 wikiStation dbStation with
    | MatchKind.Failed -> None
    | mk -> Some(dbStation, wikiStation, mk)

let matchesWkStationWithDbStationPhase2 (wikiStation: WkOpPointOfRoute) (dbStation: DbOpPointOfRoute) =
    match matchkindOfWkStationWithDbStationPhase2 wikiStation dbStation with
    | MatchKind.Failed -> None
    | mk -> Some(dbStation, wikiStation, mk)

let private hasNoDistance (length: int) (mk: MatchKind) =
    length = 0
    || mk = MatchKind.EndsWithNotDistance
    || mk = MatchKind.EqualWithoutIgnoredNotDistance
    || mk = MatchKind.SameSubstringNotDistance
    || mk = MatchKind.StartsWithNotDistance

let compareMatchForDistance (maxEqualDistance: float)
                            ((db0, wk0, mk0): DbOpPointOfRoute * WkOpPointOfRoute * MatchKind)
                            ((db1, wk1, mk1): DbOpPointOfRoute * WkOpPointOfRoute * MatchKind)
                            =
    if hasNoDistance wk0.kms.Length mk0
       || hasNoDistance wk1.kms.Length mk1 then
        if mk0 = mk1 then 0
        else if mk0 < mk1 then -1
        else 1
    else
        let diff0 =
            wk0.kms
            |> Array.map (fun k -> abs (db0.km - k))
            |> Array.min

        let diff1 =
            wk1.kms
            |> Array.map (fun k -> abs (db1.km - k))
            |> Array.min

        if equalDistance diff0 diff1 maxEqualDistance then
            if mk0 = mk1 then
                if wk0.name = wk1.name then 0
                else if wk0.name = db0.name then -1
                else 1
            else if mk0 < mk1 then
                -1
            else
                1
        else if diff0 < diff1 then
            -1
        else
            1

let compareMatch ((db0, wk0, mk0): DbOpPointOfRoute * WkOpPointOfRoute * MatchKind)
                 ((db1, wk1, mk1): DbOpPointOfRoute * WkOpPointOfRoute * MatchKind)
                 =
    compareMatchForDistance defaultEqualDistance (db0, wk0, mk0) (db1, wk1, mk1)
