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
    if diff.operation = op then
        Some(diff.text.Trim())
    else
        None

let private checkNeqDiffContainsIgnoredString (diff: Diff) =
    diff.operation <> Operation.EQUAL
    && isIgnoredStringsInStationname diff.text

let private checkEqDiffStartsWith (diff: Diff) (s1: string) (s2: string) =
    diff.operation = Operation.EQUAL
    && (checkStartsWith diff.text s1
        || checkStartsWith diff.text s2)

let private checkEqDiffLengh (diff: Diff) limit =
    diff.operation = Operation.EQUAL
    && diff.text.Length >= limit

let private checkEqDiffEndsWith (diff: Diff) (s1: string) (s2: string) =
    diff.operation = Operation.EQUAL
    && (checkEndsWith diff.text s1
        || checkEndsWith diff.text s2)

/// check equality if first text block or text block after first ignored text starts with 's1' or 's2'
let private isStartsWith (diffs: list<Diff>) (s1: string) (s2: string) =
    let s1a = applyDeletes s1 diffs
    let s2a = applyDeletes s2 diffs

    checkStartsWith s2 s1
    || checkStartsWith s2 s1a
    || checkStartsWith s2a s1a
    || checkEqDiffStartsWith diffs.Head s1 s2
    || (diffs.Length > 1
        && checkNeqDiffContainsIgnoredString diffs.[0]
        && checkEqDiffStartsWith diffs.[1] s1 s2)

/// check equality if last text block or text block before last with ignored text ends with 's1' or 's2'
let private isEndsWith (diffs: list<Diff>) (s1: string) (s2: string) =
    let s1a = (applyDeletes s1 diffs).TrimEnd()
    let s2a = (applyDeletes s2 diffs).TrimEnd()

    if checkEndsWith s2 s1a then
        true
    else if checkEndsWith s2a s1a then
        true
    else
        let last = diffs.[diffs.Length - 1]

        checkEqDiffEndsWith last s1 s2
        || (diffs.Length > 1
            && (let prev = diffs.[diffs.Length - 2]

                checkEqDiffEndsWith prev s1 s2
                && checkNeqDiffContainsIgnoredString last))

/// check equality for start text block or text block after ignored text with min length 'limit'
let private isSameSubstring (diffs: list<Diff>) limit =
    checkEqDiffLengh diffs.Head limit
    || (diffs.Length > 1
        && checkNeqDiffContainsIgnoredString diffs.[0]
        && checkEqDiffLengh diffs.[1] limit)

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
    | [ Diff Op.DELETE n; Diff Op.INSERT i; Diff Op.EQUAL _; Diff Op.DELETE d ] when
        isEqualReplaced d i
        && isIgnoredStringsInStationname n -> true
    | _ -> false

/// check equality if first and last text block are equal
let private isStartsAndEndsWith (diffs: list<Diff>) =
    match diffs with
    | [ Diff Op.EQUAL _; Diff Op.DELETE d; Diff Op.INSERT i;Diff Op.EQUAL _ ] when 
        isIgnoredStringsInStationname i
        && isIgnoredStringsInStationname d -> true
    | _ -> false

/// check equality after removing text between parentheses
let private isEqualWithoutParentheses (diffs: list<Diff>) =
    match diffs with
    | [ Diff Op.EQUAL _; Diff Op.INSERT i ] when i.StartsWith "(" && i.EndsWith ")" -> true
    | [ Diff Op.EQUAL _; Diff Op.INSERT i; Diff Op.EQUAL _ ] when i.StartsWith "(" && i.EndsWith ")" -> true
    | [ Diff Op.EQUAL _; Diff Op.DELETE d1; Diff Op.EQUAL _; Diff Op.DELETE d2 ] when d1 = "(" && d2 = ")" -> true
    | _ -> false

let private getSubstrings length (s: string) =
    if s.Length < length then
        []
    else
        [ 0 .. s.Length - length ]
        |> List.map (fun pos -> s.Substring(pos, length))

let private containsSubstringWithLength (s1: string) (s2: string) length =
    let s2Substrings = s2 |> getSubstrings length

    s1
    |> getSubstrings length
    |> List.exists (fun t1 -> s2Substrings |> List.exists ((=) t1))

let private diffStationName (wikiName: string) (dbName: string) withDistance =
    let limit = if withDistance then 8 else 12

    let diffs = getDiffs wikiName dbName

    let countContainsEqual =
        diffs
        |> List.filter (fun d -> d.operation = Operation.EQUAL && d.text.Length > 2)
        |> List.length

    if countContainsEqual = 0 then
        Failed
    else if isEqualWithoutIgnored diffs then
        EqualWithoutIgnored
    else if isEqualOrderChanged diffs then
        EqualOrderChanged
    else if isEqualWithoutParentheses diffs then
        EqualWithoutParentheses
    else if isStartsWith diffs wikiName dbName then
        StartsWith
    else if isStartsWith diffs dbName wikiName then
        StartsWith
    else if isEndsWith diffs wikiName dbName then
        EndsWith
    else if isEndsWith diffs dbName wikiName then
        EndsWith
    else if isStartsAndEndsWith diffs then
        EqualWithoutIgnored
    else if isSameSubstring diffs limit then
        SameSubstring
    else
        Failed

let matchStationName (wikiName: string) (dbName: string) withDistance =
    if equalIgnoreCase wikiName dbName then
        MatchKind.EqualNames
    else
        diffStationName (wikiName.ToLower()) (dbName.ToLower()) withDistance

/// the distance matches, if any of the wikiDistances matches with the dbDistance
let private matchStationDistanceWithDistance (wikiDistances: float []) (dbDistance: float) (distance: float) =
    wikiDistances
    |> Array.exists (fun d -> equalDistance dbDistance d distance)

let private matchStationDistance (wikiDistances: float []) (dbDistance: float) =
    matchStationDistanceWithDistance wikiDistances dbDistance defaultEqualDistance

let private matchkindOfWkStationWithDbStationPhase1 (wikiStation: WkOpPointOfRoute) (dbStation: DbOpPointOfRoute) =
    if wikiStation.shortname.Length > 0
       && wikiStation.shortname = dbStation.KUERZEL then
        if matchStationDistance wikiStation.kms dbStation.km then
            EqualShortNames
        else
            EqualShortNamesNotDistance
    else

    if System.String.Compare(wikiStation.name, dbStation.name, true) = 0 then
        if matchStationDistance wikiStation.kms dbStation.km then
            MatchKind.EqualNames
        else
            MatchKind.EqualtNamesNotDistance
    else

    if checkBorder wikiStation dbStation false then
        if matchStationDistance wikiStation.kms dbStation.km then
            MatchKind.EqualBorder
        else
            MatchKind.EqualBorderNotDistance
    else
        MatchKind.Failed

let private matchkindOfWkStationWithDbStationPhase2 (wikiStation: WkOpPointOfRoute) (dbStation: DbOpPointOfRoute) =
    if wikiStation.shortname.Length > 0
       && wikiStation.shortname = dbStation.KUERZEL then
        if matchStationDistance wikiStation.kms dbStation.km then
            EqualShortNames
        else
            EqualShortNamesNotDistance
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

let private matchkindOfWkStationWithDbStationPhase3 (wikiStation: WkOpPointOfRoute) (dbStation: DbOpPointOfRoute) =
    if matchStationDistanceWithDistance wikiStation.kms dbStation.km 0.4
       && containsSubstringWithLength wikiStation.name dbStation.name 4 then
        EqualDistanceShortSubstring
    else
        MatchKind.Failed

let matchesWkStationWithDbStationPhase1 (wikiStation: WkOpPointOfRoute) (dbStation: DbOpPointOfRoute) =
    match matchkindOfWkStationWithDbStationPhase1 wikiStation dbStation with
    | MatchKind.Failed -> None
    | mk -> Some(dbStation, wikiStation, mk)

let matchesWkStationWithDbStationPhase2 (wikiStation: WkOpPointOfRoute) (dbStation: DbOpPointOfRoute) =
    match matchkindOfWkStationWithDbStationPhase2 wikiStation dbStation with
    | MatchKind.Failed -> None
    | mk -> Some(dbStation, wikiStation, mk)

let matchesWkStationWithDbStationPhase3 (wikiStation: WkOpPointOfRoute) (dbStation: DbOpPointOfRoute) =
    match matchkindOfWkStationWithDbStationPhase3 wikiStation dbStation with
    | MatchKind.Failed -> None
    | mk -> Some(dbStation, wikiStation, mk)

let isDistanceMatchKind (mk: MatchKind) =
    not (
        mk = MatchKind.EndsWithNotDistance
        || mk = MatchKind.EqualWithoutIgnoredNotDistance
        || mk = MatchKind.SameSubstringNotDistance
        || mk = MatchKind.StartsWithNotDistance
    )

let private hasNoDistance (length: int) (mk: MatchKind) =
    length = 0 || not (isDistanceMatchKind mk)

let compareMatchForDistance
    (maxEqualDistance: float)
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

let compareMatch
    ((db0, wk0, mk0): DbOpPointOfRoute * WkOpPointOfRoute * MatchKind)
    ((db1, wk1, mk1): DbOpPointOfRoute * WkOpPointOfRoute * MatchKind)
    =
    compareMatchForDistance defaultEqualDistance (db0, wk0, mk0) (db1, wk1, mk1)
