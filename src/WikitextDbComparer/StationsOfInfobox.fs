/// collect StationOfInfobox data from templates
module StationsOfInfobox

open Ast
open RouteInfo
open System.Text.RegularExpressions
open FSharp.Collections

// see https://de.wikipedia.org/wiki/Wikipedia:Formatvorlage_Bahnstrecke/Bilderkatalog

let BorderSymbols = [| "TZOLLWo"; "GRENZE"; "xGRENZE" |]

let BhfSymbols =
    [| "BHF" // Bahnhof, Station
       "BHF-R"
       "BHF-L"
       "tTBHF"
       "BHFq" // Bahnhof, Station quer
       "KBHFa" // Kopfbahnhof Streckenanfang
       "KBHFxa" // Kopfbahnhof Streckenanfang
       "KBHFe" // Kopfbahnhof Streckenende
       "KBHFxe" // Kopfbahnhof Streckenende
       "KBHFxeq"
       "KBHFaq" // Spitzkehrenbahnhof
       "ABHFl+l" // Spitzkehrbahnhof links
       "ABHFr+r" // Spitzkehrbahnhof rechts
       "TBHFo" // Turmbahnhof oben
       "TBHFu" // Turmbahnhof unten
       "TBHFxo" // Turmbahnhof oben
       "TBHFxu" // Turmbahnhof unten
       "DST" // "Bahnhof ohne Personenverkehr, Dienststation, Betriebs- oder Güterbahnhof
       "DSTq" // "Bahnhof ohne Personenverkehr
       "S+BHF"
       "xKBHFe"
       "exBHF"
       "KS+BHFa" |]

let SBhfSymbols =
    [| "SBHF" // S-Bahnhof
       "SBHFq"
       "KSBHFa"
       "KSBHFxa"
       "KSBHFe"
       "KSBHFxe" |]

let DstSymbols =
    [| "DST" // Bahnhof ohne Personenverkehr
       "DST-R"
       "DST-L"
       "DSTq"
       "KDSTa"
       "KDSTxa"
       "KDSTe"
       "KDSTxe"
       "exKDSTe"
       "ÜST" |]

let HstSymbols =
    [| "HST" // Haltepunkt, Haltestelle
       "HSTq" // Haltepunkt, Haltestelle quer
       "KHSTa" // Halt... Streckenanfang
       "KHSTxa" // Halt... Streckenanfang
       "KHSTe" // Halt... Streckenende
       "KHSTxe" // Halt... Streckenende
       "BST" // Blockstelle etc.
       "BSTq" // Blockstelle etc. quer
       "KBSTa" // Betriebsstelle Streckenanfang
       "KBSTxa" // Betriebsstelle Streckenanfang
       "KBSTe" // Betriebsstelle Streckenende
       "KBSTxe" // Betriebsstelle Streckenende
       "KMW" // Streckenwechsel
       "xKMW" |] // Kilometrierungswechsel

let SHstSymbols =
    [| "SHST" // S-Bahnhalt...
       "SHSTq" // S-Bahnhalt... quer
       "KSHSTa" // S-Bahnhalt... Streckenanfang
       "KSHSTxa" // S-Bahnhalt... Streckenanfang
       "KSHSTe" // S-Bahnhalt... Streckenende
       "KSHSTxe" |]

let AbzweigSymbols =
    [| "ABZgl" // Abzweig geradeaus und nach links
       "ABZgr" |] // Abzweig geradeaus und nach rechts

let allbhftypes =
    Array.concat [ BhfSymbols
                   SBhfSymbols
                   HstSymbols
                   SHstSymbols
                   DstSymbols
                   AbzweigSymbols ]

let bhftypes =
    Array.concat [ BhfSymbols
                   HstSymbols
                   DstSymbols ]

type StationOfInfobox =
    { symbols: string []
      distances: float []
      name: string }

let private createStationOfInfobox (symbols: string []) (kms: float []) (name: string) =
    { symbols = symbols
      distances = kms
      name = name }

let private findBahnhofName (p: Parameter) =
    match p with
    | Composite (_, cl) ->
        match getFirstLinkInList cl with
        | Some (link) -> Some(textOfLink link)
        | _ -> None
    | Parameter.String (_, n) -> Some(n.Trim())
    | _ -> None

let private matchesType (parameters: List<Parameter>) (types: string []) =
    parameters
    |> List.exists (fun t ->
        match t with
        | Parameter.String (n, v) when types |> Array.exists ((=) v) -> true
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
        match p with
        | Parameter.String (_, km) ->
            let km0 = normalizeKms km
            let kms = km0.Split " " |> Array.map parse2float
            Some(createStationOfInfobox symbols kms name)
        | Composite (_, cl) ->
            match cl with
            | Composite.Template (n, _, lp) :: _ when n = "BSkm" && lp.Length = 2 ->
                match (getFirstStringValue lp.[0]), (getFirstStringValue lp.[1]) with
                | Some (km), Some (k2) ->
                    Some(createStationOfInfobox symbols [| (parse2float km); (parse2float k2) |] name)
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

let private containsBorderSymbols (parameters: List<Parameter>) =
    getParameterStrings parameters
    |> List.map (fun s ->
        match s with
        | Parameter.String (_, str) -> str
        | _ -> "")
    |> List.exists (fun s -> BorderSymbols |> Array.contains s)

let private findSymbols (parameters: List<Parameter>) =
    getParameterStrings parameters
    |> List.map (fun s ->
        match s with
        | Parameter.String (_, str) -> str
        | _ -> "")
    |> List.filter (fun s -> bhftypes |> Array.contains s)
    |> List.toArray

let private matchStation (symbols: string []) (p1: Parameter) (p2: Parameter) =
    match findBahnhofName p2, symbols with
    | Some (name), _ -> matchStationDistances symbols p1 name
    | _, [| "xKMW" |] -> matchStationDistances symbols p1 "Kilometrierungswechsel"
    | _ -> None

let findStationOfInfobox (t: Template) =
    try
        match t with
        | (n, [], l) when ("BS" = n || "BSe" = n)
                          && l.Length
                          >= 3
                          && (matchesType (List.take 1 l) allbhftypes) ->
            matchStation (findSymbols (List.take 1 l)) l.[1] l.[2]
        | (n, [], l) when ("BS2" = n || "BS2e" = n)
                          && l.Length
                          >= 4
                          && (matchesType (List.take 2 l) allbhftypes) ->
            matchStation (findSymbols (List.take 2 l)) l.[2] l.[3]
        | (n, [], l) when "BS3" = n
                          && l.Length >= 5
                          && (matchesType (List.take 3 l) allbhftypes) ->
            matchStation (findSymbols (List.take 3 l)) l.[3] l.[4]
        | (n, [], l) when "BS4" = n
                          && l.Length >= 6
                          && (matchesType (List.take 4 l) allbhftypes) ->
            matchStation (findSymbols (List.take 4 l)) l.[4] l.[5]
        | _ -> None
    with ex ->
        fprintfn stderr "*** error %A\n  template %A" ex t
        None
