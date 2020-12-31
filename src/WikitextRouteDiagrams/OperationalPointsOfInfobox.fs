/// collect OpPointsOfInfobox data from templates
module OpPointsOfInfobox

open Types
open Templates
open System.Text.RegularExpressions
open FSharp.Collections

/// see https://de.wikipedia.org/wiki/Wikipedia:Formatvorlage_Bahnstrecke/Bilderkatalog
/// "BS2l" "BS2r" are wrong symbols
let private BhfSymbolTypes =
    [| "BHF"
       "DST"
       "ÜST"
       "HST"
       "BST"
       "KMW"
       "KRW"
       "ABZ"
       "KRZ"
       "EIU"
       "GRENZE"
       "TZOLLWo"
       "xGRENZE"
       "BS2" |]

let private BhfSymbolTypesIgnored = [| "hKRZWae"; "tKRZW" |]

let private hasReplaceDistance (name: string) (kms: float []) =
    AdhocReplacements.Wikitext.maybeWrongDistances
    |> Array.exists (fun (_, name0, distanceWrong, _) -> name = name0 && distanceWrong = kms)

let private maybeReplaceDistances (name: string) (kms: float []) =
    let candidate =
        AdhocReplacements.Wikitext.maybeWrongDistances
        |> Array.tryFind (fun (_, name0, distanceWrong, _) -> name = name0 && distanceWrong = kms)

    match candidate with
    | Some (_, _, _, d) -> d
    | _ -> kms

let markersOfStop = [| "BHF"; "DST"; "HST" |]

let regexChars = Regex(@"^(\w+)")

let findShortName (title: string) =
    let templates =
        match DataAccess.TemplatesOfStop.query title
              |> List.tryHead with
        | Some row -> row
        | None -> List.empty

    match findTemplateParameterString templates "Infobox Bahnhof" "Abkürzung" with
    | Some value when not (System.String.IsNullOrEmpty value) ->
        match StringUtilities.regexMatchedValues regexChars value with
        | [ shortName ] -> shortName
        | _ -> ""
    | _ -> ""

let NOBREAKSPACE = '\xA0' // NO-BREAK SPACE U+00A0

let private createStationOfInfobox (symbols: string []) (kms: float []) (name: string) (link: string) =
    let name0 =
        name
            .Replace(" -", "-")
            .Replace("- ", "-")
            .Replace(NOBREAKSPACE, ' ')
            .Replace("<!--sic!-->", "")

    let isStation =
        symbols
        |> Array.exists (fun s -> (markersOfStop |> Array.exists s.Contains))

    let link0 = if isStation then link else ""

    let shortname =
        if isStation
           && not (System.String.IsNullOrEmpty link0) then
            findShortName link0
        else
            ""

    let kms0 = maybeReplaceDistances name0 kms

    { symbols = symbols
      distances = kms0
      name = name0
      link = link0
      shortname = shortname }

let isValidText (s: string) =
    not (System.String.IsNullOrEmpty(s))
    && s <> "'''"
    && s <> "("
    && s <> "/"

let private normalizeTextOfLink (link: Link) =
    (textOfLink link).Replace("&nbsp;", " ")

let onlyChars (s: string) =
    let s0 =
        if s.EndsWith "-" then s.Substring(0, s.Length - 1) else s

    s0 |> Seq.forall System.Char.IsLetter

let private findStationName (p: Parameter) =
    match p with // try first string
    | Composite (_,
                 Composite.String ("'''") :: Link link1 :: Composite.String ("-") :: Link link2 :: Composite.String ("'''") :: _) ->
        Some(linktextOfLink link1 + "-" + linktextOfLink link2, linktextOfLink link1)
    | Composite (_, Composite.String (s0) :: Link link :: Composite.String (s1) :: _) when onlyChars (s0)
                                                                                           && isValidText (s1) ->
        Some(s0 + " " + normalizeTextOfLink link + " " + s1, linktextOfLink link)
    | Composite (_, Composite.String (s0) :: Link link :: Composite.String (s1) :: _) when s0.EndsWith("(") && s1 = ")" ->
        Some(s0 + " " + normalizeTextOfLink link + " " + s1, linktextOfLink link)
    | Composite (_, Composite.String (_) :: Link link :: Composite.String (s) :: _) when isValidText (s) ->
        Some(normalizeTextOfLink link + " " + s, linktextOfLink link)
    | Composite (_, Composite.Link link1 :: Composite.String ("/") :: [ Composite.Link link2 ]) ->
        Some(
            normalizeTextOfLink link1
            + "/"
            + normalizeTextOfLink link2,
            linktextOfLink link1
        )
    | Composite (_, Composite.Link link1 :: Composite.String (s) :: Composite.Link link2 :: _) when isValidText (s) ->
        Some(
            normalizeTextOfLink link1
            + " "
            + s
            + " "
            + normalizeTextOfLink link2,
            linktextOfLink link1
        )
    | Composite (_, Composite.Link link :: Composite.String (s) :: _) when isValidText (s) ->
        Some(normalizeTextOfLink link + " " + s, linktextOfLink link)
    | Composite (_, Composite.Link link1 :: Composite.Link link2 :: _) ->
        Some(
            normalizeTextOfLink link1
            + " "
            + normalizeTextOfLink link2,
            linktextOfLink link2
        )
    | Composite (_, Composite.String (s) :: Composite.Link link :: _) when isValidText (s) ->
        Some(s + " " + normalizeTextOfLink link, linktextOfLink link)
    | Composite (_, cl) ->
        match getFirstLinkInList cl with
        | Some (link) -> Some(normalizeTextOfLink link, linktextOfLink link)
        | _ ->
            match cl with
            | Composite.String (s) :: _ -> Some(s, "")
            | _ -> None
    | Parameter.String (_, n) -> Some(n.Trim(), "")
    | _ -> None

let private matchesSymbolType (parameters: seq<Parameter>) =
    parameters
    |> existsParameterStringValueInList
        BhfSymbolTypes
        (fun s p ->
            s.Contains(p)
            && not (BhfSymbolTypesIgnored |> Array.exists ((=) s)))

let private matchesParameterName (parameters: seq<Parameter>) (names: string []) =
    parameters
    |> forallStringsInParameterStringNameList names (=)

let private regexSpaces = Regex(@"\s+")

let private normalizeKms (kms: string) =
    StringUtilities.replaceFromRegexToString
        regexSpaces
        " "
        (kms
            .Replace("(", "")
            .Replace(")", "")
            .Replace("&nbsp;", ""))

let private regexFloat = Regex(@"^([-]?[0-9\.]+)")

let private parse2float (km: string) =
    match StringUtilities.regexMatchedValues
              regexFloat
              (km
                  .Replace(",", ".")
                  .Replace("(", "")
                  .Replace(")", "")
                  .Replace("~", "")
                  .Replace("−", "-")) with
    | [ float ] -> System.Math.Round(double (System.Single.Parse(float)), 1)
    | _ -> -1.0

let private matchStationDistances (p: Parameter) (name: string) =
    try
        match p with // todo: find index
        | Parameter.String (_, km) ->
            let km0 = normalizeKms km
            km0.Split " " |> Array.map parse2float
        | Parameter.Empty when hasReplaceDistance name [||] -> [||]
        | Composite (_, cl) ->
            match cl with
            | Composite.Template (n, _, lp) :: _ when (n = "BSkm" || n = "BSkmL") && lp.Length = 2 ->
                match (getFirstStringValue lp.[0]), (getFirstStringValue lp.[1]) with
                | Some (km), Some (k2) -> [| (parse2float km); (parse2float k2) |]
                | _ -> [||]
            | Composite.Template (n, _, lp) :: _ when n = "Coordinate" && lp.Length = 6 ->
                match (lp.[4], getFirstStringValue lp.[4]) with
                | Parameter.String ("text", _), Some (km) -> [| (parse2float km) |]
                | _ -> [||]
            | _ ->
                getCompositeStrings cl
                |> Seq.map
                    (fun s ->
                        match s with
                        | Composite.String (f) -> parse2float f
                        | _ -> -1.0)
                |> Seq.toArray
        | _ -> [||]
    with ex -> // todo: find index
        fprintfn stderr "error: %A, findKm parameter %A" ex p
        [||]

let private findSymbols (parameters: List<Parameter>) =
    getParameterStrings parameters
    |> Seq.map
        (fun s ->
            match s with
            | Parameter.String (_, str) -> str
            | _ -> "")
    |> Seq.toArray

let private matchStation (symbols: string []) (p1: Parameter) (p2: Parameter) =
    match findStationName p2, symbols with
    | Some (name, link), _ -> Some(createStationOfInfobox symbols (matchStationDistances p1 name) name link)
    | _, [| "xKMW" |] ->
        let name = "Kilometrierungswechsel"
        Some(createStationOfInfobox symbols (matchStationDistances p1 name) name "")
    | _ -> None

let private chooseNonEmptyParameter (index: int) (l: Parameter list) =
    match l.[index] with // try first string
    | Parameter.Empty when index + 1 < l.Length -> l.[index + 1]
    | Parameter.String (n, _) when not (System.String.IsNullOrEmpty n) // bypass hints
                                   && index + 1 < l.Length -> l.[index + 1]
    | _ -> l.[index]

let private matchCondition name (names: string []) minLength index (l: list<Parameter>) =
    names |> Array.exists ((=) name)
    && l.Length >= minLength
    && matchesSymbolType (List.take index l)

let private matchConditionWithParameters name (names: string []) minLength index parameterNames (l: list<Parameter>) =
    names |> Array.exists ((=) name)
    && l.Length >= minLength
    && matchesParameterName (List.take index l) parameterNames
    && matchesSymbolType (List.take index l)

let private matchStationAt index l =
    matchStation (findSymbols (List.take index l)) l.[index] (chooseNonEmptyParameter (index + 1) l)

let findStationOfInfobox (t: Template) =
    try
        match t with
        | (n, [], l) when matchConditionWithParameters n [| "BS"; "BSe" |] 4 2 [| "T" |] l -> matchStationAt 2 l
        | (n, [], l) when matchCondition n [| "BS"; "BSe" |] 3 1 l -> matchStationAt 1 l
        | (n, [], l) when matchConditionWithParameters n [| "BS2"; "BS2e" |] 7 5 [| "T1"; "T2" |] l ->
            matchStationAt 4 l
        | (n, [], l) when matchConditionWithParameters n [| "BS2"; "BS2e" |] 5 3 [| "T1" |] l ->
            matchStationAt 3 l
        | (n, [], l) when matchCondition n [| "BS2"; "BS2e" |] 4 2 l -> matchStationAt 2 l
        | (n, [], l) when matchConditionWithParameters n [| "BS3"; "BS3e" |] 8 6 [| "T1"; "T2"; "T3" |] l ->
            matchStationAt 6 l
        | (n, [], l) when matchCondition n [| "BS3"; "BS3e" |] 5 3 l -> matchStationAt 3 l
        | (n, [], l) when matchCondition n [| "BS4"; "BS4e" |] 6 4 l -> matchStationAt 4 l
        | (n, [], l) when matchCondition n [| "BS5"; "BS5e" |] 7 5 l -> matchStationAt 5 l
        | _ -> None
    with ex ->
        fprintfn stderr "*** error %A\n  template %A" ex t
        None

let queryName (name: string) =
    // DataAccess.WkStationOfInfobox.queryKeys ()

    let results =
        Serializer.Deserialize<ResultOfRoute []>(DataAccess.ResultOfRoute.queryAll ())

    [ for r in results do
        if r.resultKind = ResultKind.WikidataFoundInDbData
           || r.resultKind = ResultKind.WikidataNotFoundInDbData then
            yield r.title ]
    |> List.distinct
    |> List.iter
        (fun t ->
            match DataAccess.WkOpPointOfInfobox.query t
                  |> List.tryHead with
            | Some stations ->
                stations
                |> Array.filter (fun s -> s.name.Contains name)
                |> Array.iter (fun s -> printfn "'%s', '%s'" t s.name)
            | None -> ())

let dump (title: string) (precodedStations: OpPointOfInfobox []) =
    DataAccess.WkOpPointOfInfobox.insert title precodedStations
    |> ignore
