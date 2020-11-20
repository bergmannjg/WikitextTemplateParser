/// collect StationOfInfobox data from templates
module StationsOfInfobox

open Ast
open System.Text.RegularExpressions
open FSharp.Collections

type StationOfInfobox =
    { symbols: string []
      distances: float []
      name: string }


/// see https://de.wikipedia.org/wiki/Wikipedia:Formatvorlage_Bahnstrecke/Bilderkatalog
/// "BS2l" "BS2r" are wrong symbols
let BhfSymbolTypes =
    [| "BHF"
       "DST"
       "ÜST"
       "HST"
       "BST"
       "KMW"
       "KRW"
       "ABZ"
       "KRZ"
       "GRENZE"
       "TZOLLWo"
       "xGRENZE"
       "BS2" |]

let private hasReplaceDistance (name: string) (kms: float []) =
    AdhocReplacements.maybeWrongDistances
    |> Array.exists (fun (_, name0, distanceWrong, _) -> name = name0 && distanceWrong = kms)

let private maybeReplaceDistances (name: string) (kms: float []) =
    let candidate =
        AdhocReplacements.maybeWrongDistances
        |> Array.tryFind (fun (_, name0, distanceWrong, _) -> name = name0 && distanceWrong = kms)

    match candidate with
    | Some (_, _, _, d) -> d
    | _ -> kms

let private createStationOfInfobox (symbols: string []) (kms: float []) (name: string) =
    let name0 =
        name.Replace(" -", "-").Replace("- ", "-")

    let kms0 = maybeReplaceDistances name0 kms
    { symbols = symbols
      distances = kms0
      name = name0 }

let isValidText (s: string) =
    not (System.String.IsNullOrEmpty(s))
    && s
    <> "'''"
    && s <> "("

let private normalizeTextOfLink (link: Link) = (textOfLink link).Replace("&nbsp;", " ")

let private findStationName (p: Parameter) =
    match p with // try first string
    | Composite (_, Composite.String (_) :: Link link :: Composite.String (s) :: _) when isValidText (s) ->
        Some(normalizeTextOfLink link + " " + s)
    | Composite (_, Composite.Link link1 :: Composite.String (s) :: Composite.Link link2 :: _) when isValidText (s) ->
        Some
            (normalizeTextOfLink link1
             + s
             + normalizeTextOfLink link2)
    | Composite (_, Composite.Link link :: Composite.String (s) :: _) when isValidText (s) ->
        Some(normalizeTextOfLink link + " " + s)
    | Composite (_, Composite.String (s) :: Composite.Link link :: _) when isValidText (s) ->
        Some(s + " " + normalizeTextOfLink link)
    | Composite (_, cl) ->
        match getFirstLinkInList cl with
        | Some (link) -> Some(normalizeTextOfLink link)
        | _ ->
            match cl with
            | Composite.String (s) :: _ -> Some(s)
            | _ -> None
    | Parameter.String (_, n) -> Some(n.Trim())
    | _ -> None

let private matchesType (parameters: List<Parameter>) (types: string []) =
    parameters
    |> List.exists (fun t ->
        match t with
        | Parameter.String (_, v) when types |> Array.exists (fun t -> v.Contains(t)) -> true
        | _ -> false)

let private matchesParameterName (parameters: List<Parameter>) (names: string []) =
    parameters
    |> List.exists (fun t ->
        match t with
        | Parameter.String (n, _) when names |> Array.exists ((=) n) -> true
        | _ -> false)

let private normalizeKms (kms: string) =
    let km0 =
        kms.Replace("(", "").Replace(")", "").Replace("&nbsp;", "")

    let regex1 = Regex(@"\s+")
    regex1.Replace(km0, " ").Trim()

let private convertfloattxt (km: string) =
    let regex0 = Regex(@"^([0-9\.]+)")

    let m =
        regex0.Match(km.Replace(",", ".").Replace("(", "").Replace(")", ""))

    if m.Success && m.Groups.Count = 2 then m.Groups.[1].Value else "-1.0"

let private parse2float (km: string) =
    let f =
        double (System.Single.Parse(convertfloattxt km))

    System.Math.Round(f, 1)

let private matchStationDistances (symbols: string []) (p: Parameter) (name: string) =
    try
        match p with // todo: find index
        | Parameter.String (_, km) ->
            let km0 = normalizeKms km
            let kms = km0.Split " " |> Array.map parse2float
            Some(createStationOfInfobox symbols kms name)
        | Parameter.Empty when hasReplaceDistance name [||] -> Some(createStationOfInfobox symbols [||] name)
        | Composite (_, cl) ->
            match cl with
            | Composite.Template (n, _, lp) :: _ when n = "BSkm" && lp.Length = 2 ->
                match (getFirstStringValue lp.[0]), (getFirstStringValue lp.[1]) with
                | Some (km), Some (k2) ->
                    Some(createStationOfInfobox symbols [| (parse2float km); (parse2float k2) |] name)
                | _ -> None
            | Composite.Template (n, _, lp) :: _ when n = "Coordinate" && lp.Length = 6 ->
                match (lp.[4], getFirstStringValue lp.[4]) with
                | Parameter.String ("text", _), Some (km) ->
                    Some(createStationOfInfobox symbols [| (parse2float km) |] name)
                | _ -> None
            | _ ->
                let kms =
                    getCompositeStrings cl
                    |> List.map (fun s ->
                        match s with
                        | Composite.String (f) -> parse2float f
                        | _ -> -1.0)
                    |> List.toArray

                Some(createStationOfInfobox symbols kms name)
        | _ -> None
    with ex ->
        fprintfn stderr "error: %A, findKm parameter %A" ex p
        None

let private findSymbols (parameters: List<Parameter>) =
    getParameterStrings parameters
    |> List.map (fun s ->
        match s with
        | Parameter.String (_, str) -> str
        | _ -> "")
    |> List.toArray

let private matchStation (symbols: string []) (p1: Parameter) (p2: Parameter) =
    match findStationName p2, symbols with
    | Some (name), _ -> matchStationDistances symbols p1 name
    | _, [| "xKMW" |] -> matchStationDistances symbols p1 "Kilometrierungswechsel"
    | _ -> None

let chooseNonEmptyParameter (index: int) (l: Parameter list) =
    match l.[index] with // try first string
    | Parameter.Empty when index + 1 < l.Length -> l.[index + 1]
    | Parameter.String (n, _) when not (System.String.IsNullOrEmpty n) // bypass hints
                                   && index
                                   + 1 < l.Length -> l.[index + 1]
    | _ -> l.[index]

let findStationOfInfobox (t: Template) =
    try
        match t with
        | (n, [], l) when ("BS" = n || "BSe" = n)
                          && l.Length
                          >= 4
                          && (matchesParameterName (List.take 2 l) [| "T" |])
                          && (matchesType (List.take 2 l) BhfSymbolTypes) ->
            matchStation (findSymbols (List.take 2 l)) l.[2] (chooseNonEmptyParameter 3 l)
        | (n, [], l) when ("BS" = n || "BSe" = n)
                          && l.Length
                          >= 3
                          && (matchesType (List.take 1 l) BhfSymbolTypes) ->
            matchStation (findSymbols (List.take 1 l)) l.[1] (chooseNonEmptyParameter 2 l)
        | (n, [], l) when ("BS2" = n || "BS2e" = n)
                          && l.Length
                          >= 4
                          && (matchesType (List.take 2 l) BhfSymbolTypes) ->
            matchStation (findSymbols (List.take 2 l)) l.[2] (chooseNonEmptyParameter 3 l)
        | (n, [], l) when ("BS3" = n || "BS3e" = n)
                          && l.Length
                          >= 8
                          && (matchesParameterName (List.take 6 l) [| "T1"; "T2"; "T3" |])
                          && (matchesType (List.take 6 l) BhfSymbolTypes) ->
            matchStation (findSymbols (List.take 6 l)) l.[6] (chooseNonEmptyParameter 7 l)
        | (n, [], l) when ("BS3" = n || "BS3e" = n)
                          && l.Length
                          >= 5
                          && (matchesType (List.take 3 l) BhfSymbolTypes) ->
            matchStation (findSymbols (List.take 3 l)) l.[3] (chooseNonEmptyParameter 4 l)
        | (n, [], l) when "BS4" = n
                          && l.Length >= 6
                          && (matchesType (List.take 4 l) BhfSymbolTypes) ->
            matchStation (findSymbols (List.take 4 l)) l.[4] l.[5]
        | (n, [], l) when "BS5" = n
                          && l.Length >= 7
                          && (matchesType (List.take 5 l) BhfSymbolTypes) ->
            matchStation (findSymbols (List.take 5 l)) l.[5] l.[6]
        | _ -> None
    with ex ->
        fprintfn stderr "*** error %A\n  template %A" ex t
        None

let dump (title: string) (precodedStations: StationOfInfobox []) =
    DataAccess.WkStationOfInfobox.insert title (Serializer.Serialize<StationOfInfobox []>(precodedStations))
    |> ignore
