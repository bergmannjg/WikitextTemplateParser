/// match wiki and db operational points
module OpPointMatch

open Types
open DiffMatchPatch

let private defaultEqualDistance = 1.0

let private equalDistance d1 d2 maxEqualDistance = abs (d1 - d2) < maxEqualDistance

let private checkBorder (wikiStation: WkOpPointOfRoute) (dbStation: DbOpPointOfRoute) withDistance =
    (wikiStation.name.Contains "Staatsgrenze"
     || wikiStation.name.Contains "Infrastrukturgrenze")
    && (dbStation.name.Contains "DB Grenze"
        || dbStation.name.Contains "DB-Grenze"
        || dbStation.STELLE_ART = RInfData.StelleArtGrenze
        || withDistance && dbStation.name.Contains "Grenze")

/// check s1 starts with s2 and after the end of the common substring is a blank
let checkStartsWith (s1: string) (s2: string) =
    s1.StartsWith s2
    && (s1.[s2.Length - 1] = ' '
        || (s1.Length > s2.Length
            && (let c = s1.[s2.Length]
                c = ' ' || c = '/')))

/// check s1 ends with s2 and before the start of the common substring is a blank
let checkEndsWith (s1: string) (s2: string) =
    s1 = s2
    || (s1.EndsWith s2
        && (s2.[0] = ' '
            || (s1.Length > s2.Length
                && (let c = s1.[s1.Length - s2.Length - 1]
                    c = ' ' || c = '-' || c = '/'))))

let private getDiffs (s1: string) (s2: string) =
    let d = diff_match_patch ()
    let diffs = d.diff_main (s1, s2)
    d.diff_cleanupSemantic (diffs)
    diffs |> Seq.toList

let equalIgnoreCase s1 s2 = System.String.Compare(s1, s2, true) = 0

let ignoreStringsInStationname =
    AdhocReplacements.OpPointMatch.ignoreStringsInStationname
    |> List.map (fun s -> s.ToLower())

let isIgnoredStringsInStationname (s: string) =
    let t = s.Trim()
    ignoreStringsInStationname |> List.exists ((=) t)

let private applyDelete (s: string) (diff: Diff) =
    if diff.operation = Operation.DELETE
       && isIgnoredStringsInStationname diff.text then
        s.Replace(diff.text, "")
    else
        s

let private applyDeletes (s: string) (diffs: list<Diff>) = diffs |> List.fold (applyDelete) s

let private (|Diff|_|) (op: Operation) (diff: Diff) =
    if diff.operation = op then Some(diff.text.Trim()) else None

/// check equality if first text block or text block after first ignored text starts with 's1' or 's2'
let private isStartsWith (diffs: list<Diff>) (s1: string) (s2: string) =
    let s1a = applyDeletes s1 diffs

    checkStartsWith s2 s1
    || checkStartsWith s2 s1a
    || (diffs.Head.operation = Operation.EQUAL
        && (checkStartsWith diffs.Head.text s1
            || checkStartsWith diffs.Head.text s2))
    || (diffs.Length > 1
        && diffs.Head.operation <> Operation.EQUAL
        && isIgnoredStringsInStationname diffs.Head.text
        && (diffs.[1].operation = Operation.EQUAL
            && (checkStartsWith diffs.[1].text s1
                || checkStartsWith diffs.[1].text s2)))

/// check equality if last text block or text block before last ignored text ends with 's1' or 's2'
let private isEndsWith (diffs: list<Diff>) (s1: string) (s2: string) =
    let s1a = (applyDeletes s1 diffs).TrimEnd()
    let s2a = (applyDeletes s2 diffs).TrimEnd()

    if checkEndsWith s2 s1a then
        true
    else if checkEndsWith s2a s1a then
        true
    else
        let last = diffs.[diffs.Length - 1]

        let prev =
            if diffs.Length > 1 then Some diffs.[diffs.Length - 2] else None

        match prev, last with
        | (_, Diff Operation.EQUAL s) -> s.EndsWith s1 || s.EndsWith s2
        | (Some (Diff Operation.EQUAL s), Diff Operation.DELETE d) ->
            (isIgnoredStringsInStationname d)
            && (checkEndsWith s s1 || checkEndsWith s s2)
        | _ -> false

/// check equality for start text block or text block after ignored text with min length 'limit'
let private isSameSubstring (diffs: list<Diff>) (s1: string) (s2: string) limit =
    diffs.Head.operation = Operation.EQUAL
    && diffs.Head.text.Length >= limit
    || (diffs.Length > 1
        && diffs.Head.operation <> Operation.EQUAL
        && isIgnoredStringsInStationname diffs.Head.text
        && (diffs.[1].operation = Operation.EQUAL
            && (diffs.[1].text.Length >= limit
                || diffs.[1].text.StartsWith(s1)
                || diffs.[1].text.StartsWith(s2))))

/// check equality after removing ignored text blocks
let private isEqualWithoutIgnored (diffs: list<Diff>) =
    diffs
    |> List.forall
        (fun d ->
            d.operation = Operation.EQUAL
            || d.text = " "
            || isIgnoredStringsInStationname d.text)

let isEqualReplaced (s1: string) (s2: string) =
    s1.Replace("-", "") = s2.Replace("-", "")

type Op = Operation

/// check equality after changing the order of text blocks
let private isEqualOrderChanged (diffs: list<Diff>) =
    match diffs with
    | [ Diff Op.DELETE d; Diff Op.EQUAL _; Diff Op.INSERT i ] when isEqualReplaced d i -> true
    | [ Diff Op.DELETE n; Diff Op.INSERT i; Diff Op.EQUAL _; Diff Op.DELETE d ] when isEqualReplaced d i
                                                                                     && isIgnoredStringsInStationname n ->
        true
    | _ -> false

/// check equality after removing text between parentheses
let private isEqualWithoutParentheses (diffs: list<Diff>) =
    match diffs with
    | [ Diff Op.EQUAL _; Diff Op.INSERT i ] when i.StartsWith "(" && i.EndsWith ")" -> true
    | [ Diff Op.EQUAL _; Diff Op.INSERT i; Diff Op.EQUAL _ ] when i.StartsWith "(" && i.EndsWith ")" -> true
    | _ -> false

let private diffStationName (wikiName: string) (dbName: string) withDistance =
    let limit = if withDistance then 5 else 12

    let diffs = getDiffs wikiName dbName

    let countContainsEqual =
        diffs
        |> List.filter (fun d -> d.operation = Operation.EQUAL && d.text.Length > 2)
        |> List.length

    if countContainsEqual = 0 then Failed
    else if isEqualWithoutIgnored diffs then EqualWithoutIgnored
    else if isEqualOrderChanged diffs then EqualOrderChanged
    else if isEqualWithoutParentheses diffs then EqualWithoutParentheses
    else if isStartsWith diffs wikiName dbName then StartsWith
    else if isStartsWith diffs dbName wikiName then StartsWith
    else if isEndsWith diffs wikiName dbName then EndsWith
    else if isEndsWith diffs dbName wikiName then EndsWith
    else if isSameSubstring diffs wikiName dbName limit then SameSubstring
    else Failed

let matchStationName (wikiName: string) (dbName: string) withDistance =
    if equalIgnoreCase wikiName dbName
    then MatchKind.EqualNames
    else diffStationName (wikiName.ToLower()) (dbName.ToLower()) withDistance

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
