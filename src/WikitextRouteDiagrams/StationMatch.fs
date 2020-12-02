/// match wiki station with db station
module StationMatch

open Types
open StationsOfRoute
open DbData
open System.Text.RegularExpressions

let private regexTextInParentheses = Regex(@"\([^\)]+\)")

let private replaceIgnored s =
    s
    |> StringUtilities.replaceFromListToEmpty AdhocReplacements.ignoreStringsInStationname
    |> StringUtilities.replaceWithBlank '-'

/// no strict matching af names, assuming the distance check has succeeded
let private matchStationName (wikiName: string) (dbName: string) =
    if System.String.Compare(wikiName, dbName, true) = 0 then
        MatchKind.EqualNames
    else
        let wikiName0 = replaceIgnored wikiName
        let dbName0 = replaceIgnored dbName

        if wikiName0 = dbName0 then
            MatchKind.EqualWithoutIgnored
        else

        if dbName0.StartsWith wikiName0 then
            MatchKind.StartsWith
        else

        if dbName0.EndsWith wikiName0 then
            MatchKind.EndsWith
        else

        if wikiName0.StartsWith dbName0 then
            MatchKind.StartsWith
        else

        if wikiName0.EndsWith dbName0 then
            MatchKind.EndsWith
        else
            let wikiNamex =
                StringUtilities.replaceFromRegexToEmpty regexTextInParentheses wikiName0

            let dbNamex =
                StringUtilities.replaceFromRegexToEmpty regexTextInParentheses dbName0

            if wikiNamex = dbNamex then
                MatchKind.EqualWithoutParentheses
            else if (StringUtilities.levenshtein wikiNamex dbNamex)
                    <= 3 then
                MatchKind.Levenshtein
            else if StringUtilities.sameSubstring wikiName0 dbName0 5 then
                MatchKind.SameSubstring
            else
                MatchKind.Failed

/// the distance matches, if any of the wikiDistances matches with the dbDistance
let private matchStationDistance (wikiDistances: float []) (dbDistance: float) =
    wikiDistances
    |> Array.exists (fun d -> abs (dbDistance - d) < 1.0)

let matchesWkStationWithDbStation (wikiStation: StationOfRoute) (dbStation: DbStationOfRoute) =
    if wikiStation.shortname.Length > 0
       && wikiStation.shortname = dbStation.KUERZEL then
        let mk =
            if matchStationDistance wikiStation.kms dbStation.km
            then EqualShortNames
            else EqualShortNamesNotDistance

        Some(dbStation, wikiStation, mk)
    else

    if matchStationDistance wikiStation.kms dbStation.km then
        let mk =
            matchStationName wikiStation.name dbStation.name

        if mk <> MatchKind.Failed then Some(dbStation, wikiStation, mk) else None
    else
        None
