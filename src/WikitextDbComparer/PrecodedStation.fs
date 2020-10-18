module PrecodedStation

open Ast
open AstUtils
open System.Text.RegularExpressions
open FSharp.Collections

// see https://de.wikipedia.org/wiki/Wikipedia:Formatvorlage_Bahnstrecke/Bilderkatalog


let BorderSymbols = [| "TZOLLWo" |]

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
       "S+BHF" |]

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
       "xKMW" |] // Kilometrierungswechsel

let SHstSymbols =
    [| "SHST" // S-Bahnhalt...
       "SHSTq" // S-Bahnhalt... quer
       "KSHSTa" // S-Bahnhalt... Streckenanfang
       "KSHSTxa" // S-Bahnhalt... Streckenanfang
       "KSHSTe" // S-Bahnhalt... Streckenende
       "KSHSTxe" |]

let allbhftypes =
    Array.concat [ BhfSymbols
                   SBhfSymbols
                   HstSymbols
                   SHstSymbols
                   DstSymbols ]

let bhftypes =
    Array.concat [ BhfSymbols
                   HstSymbols
                   DstSymbols ]

// DistanceCoding for a BS template
type DistanceCoding =
    | Undef
    | BsKm // with template BsKm
    | SingleValue // unique single value
    | MainTrack // multiple values for SBHF or BHF etc.

type PrecodedStation =
    { distanceCoding: DistanceCoding
      symbols: string []
      distances: float []
      name: string }

let findBahnhofName (p: Parameter) =
    match p with
    | Composite (_, cl) ->
        match getFirstLinkInList cl with
        | Some (link) -> Some(textOfLink link)
        | _ -> None
    | String (_, n) -> Some(n.Trim())
    | _ -> None

let matchesType (parameters: List<Parameter>) (types: string []) =
    parameters
    |> List.exists (fun t ->
        match t with
        | String (n, v) when types |> Array.exists ((=) v) -> true
        | _ -> false)

let normalizeKms (kms: string) =
    let km0 =
        kms.Replace("(", "").Replace(")", "").Replace("&nbsp;", "")

    let regex1 = Regex(@"\s+")
    regex1.Replace(km0, " ").Trim()

let filterBhfSymbols (symbols: string []) (kms: float []) =
    if kms.Length = 1 && kms.Length = symbols.Length then
        (symbols, kms)
    else if kms.Length = symbols.Length then
        Array.fold2 (fun (x1, x2) y z ->
            if bhftypes |> Array.contains y
            then (Array.concat [ [| y |]; x1 ], Array.concat [ [| z |]; x2 ])
            else (x1, x2)) ([||], [||]) symbols kms
    else
        (symbols, kms)

let createDistanceCoding (symbols0: string []) (kms0: float []) (name: string) =
    let (symbols, kms) = filterBhfSymbols symbols0 kms0
    if kms.Length = 1 then
        Some
            ({ distanceCoding = DistanceCoding.SingleValue
               symbols = symbols
               distances = kms
               name = name })
    else if kms.Length = symbols.Length then
        Some
            ({ distanceCoding = DistanceCoding.MainTrack
               symbols = symbols
               distances = kms
               name = name })
    else if kms.Length > symbols.Length && symbols.Length = 1 then // adhoc
        Some
            ({ distanceCoding = DistanceCoding.SingleValue
               symbols = symbols
               distances = [| kms.[kms.Length - 1] |]
               name = name })
    else
        fprintfn
            stderr
            "*** findDistanceCoding, %s, distances.Length %d <> symbols.Length %d, %A"
            name
            kms.Length
            symbols.Length
            kms
        None

let findDistanceCoding (symbols: string []) (p: Parameter) (name: string) =
    try
        match p with
        | String (_, km) ->
            let km0 = normalizeKms km
            let kms = km0.Split " " |> Array.map parse2float
            createDistanceCoding symbols kms name
        | Composite (_, cl) ->
            match cl with
            | Composite.Template (n, lp) :: _ when n = "BSkm" && lp.Length = 2 ->
                match (getFirstStringValue lp.[0]), (getFirstStringValue lp.[1]) with
                | Some (km), Some (k2) ->
                    Some
                        ({ distanceCoding = DistanceCoding.BsKm
                           symbols = symbols
                           distances = [| (parse2float km); (parse2float k2) |]
                           name = name })
                | _ -> None
            | _ ->
                let kms =
                    getCompositeStrings cl
                    |> List.map (fun s ->
                        match s with
                        | Composite.String (f) -> parse2float f
                        | _ -> -1.0)
                    |> List.toArray

                createDistanceCoding symbols kms name
        | _ -> None
    with ex ->
        fprintfn stderr "error: %A, findKm parameter %A" ex p
        None

let containsBorderSymbols (parameters: List<Parameter>) =
    getParameterStrings parameters
    |> List.map (fun s ->
        match s with
        | Parameter.String (_, str) -> str
        | _ -> "")
    |> List.exists (fun s -> BorderSymbols |> Array.contains s)

let findSymbols (parameters: List<Parameter>) =
    getParameterStrings parameters
    |> List.map (fun s ->
        match s with
        | Parameter.String (_, str) -> str
        | _ -> "")
    |> List.filter (fun s -> bhftypes |> Array.contains s)
    |> List.toArray

let createPrecodedStation (symbols: string []) (p1: Parameter) (p2: Parameter) =
    match findBahnhofName p2, symbols with
    | Some (name), _ -> findDistanceCoding symbols p1 name
    | _, [| "xKMW" |] -> findDistanceCoding symbols p1 "Kilometrierungswechsel"
    | _ -> None

let containsBorderStation (t: Template) =
    match t with
    | (n, l) when "BS" = n
                  && l.Length >= 3
                  && (containsBorderSymbols (List.take 1 l)) -> true
    | _ -> false

let findPrecodedStation (t: Template) =
    try
        match t with
        | (n, l) when "BS" = n
                      && l.Length >= 3
                      && (matchesType (List.take 1 l) allbhftypes) ->
            createPrecodedStation (findSymbols (List.take 1 l)) l.[1] l.[2]
        | (n, l) when "BS2" = n
                      && l.Length >= 4
                      && (matchesType (List.take 2 l) allbhftypes) ->
            createPrecodedStation (findSymbols (List.take 2 l)) l.[2] l.[3]
        | (n, l) when "BS3" = n
                      && l.Length >= 5
                      && (matchesType (List.take 3 l) allbhftypes) ->
            createPrecodedStation (findSymbols (List.take 3 l)) l.[3] l.[4]
        | (n, l) when "BS4" = n
                      && l.Length >= 6
                      && (matchesType (List.take 4 l) allbhftypes) ->
            createPrecodedStation (findSymbols (List.take 4 l)) l.[4] l.[5]
        | _ -> None
    with ex ->
        fprintfn stderr "*** error %A\n  template %A" ex t
        None

let fillStreckeNames (strecke: Strecke) (precodedStations: PrecodedStation []) =
    if precodedStations.Length > 1
       && (System.String.IsNullOrEmpty strecke.von
           || System.String.IsNullOrEmpty strecke.bis) then

        let first =
            precodedStations
            |> Array.tryFind (fun s -> s.symbols.Length > 0)

        let last =
            precodedStations
            |> Array.tryFind (fun s -> s.symbols.Length > 0)

        { nummer = strecke.nummer
          von =
              match first with
              | Some s -> s.name
              | None -> ""
          bis =
              match last with
              | Some s -> s.name
              | None -> "" }
    else
        strecke
