module Stations

open Ast
open AstUtils
open System.Text.RegularExpressions
open FSharp.Collections

// see https://de.wikipedia.org/wiki/Wikipedia:Formatvorlage_Bahnstrecke/Bilderkatalog

let BhfSymbols =
    [| "BHF" // Bahnhof, Station
       "BHF-R"
       "BHF-L"
       "BHFq" // Bahnhof, Station quer
       "KBHFa" // Kopfbahnhof Streckenanfang
       "KBHFxa" // Kopfbahnhof Streckenanfang
       "KBHFe" // Kopfbahnhof Streckenende
       "KBHFxe" // Kopfbahnhof Streckenende
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
       "KDSTxe" |]

let HstSymbols =
    [| "HST" // Haltepunkt, Haltestelle
       "HSTq" // Haltepunkt, Haltestelle quer
       "KHSTa" // Halt... Streckenanfang
       "KHSTxa" // Halt... Streckenanfang
       "KHSTe" // Halt... Streckenende
       "KHSTxe" |] // Halt... Streckenende

let SHstSymbols =
    [| "SHST" // S-Bahnhalt...
       "SHSTq" // S-Bahnhalt... quer
       "KSHSTa" // S-Bahnhalt... Streckenanfang
       "KSHSTxa" // S-Bahnhalt... Streckenanfang
       "KSHSTe" // S-Bahnhalt... Streckenende
       "KSHSTxe" |]

let bhftypes =
    Array.concat [ BhfSymbols
                   SBhfSymbols
                   HstSymbols
                   SHstSymbols
                   DstSymbols ]

// DistanceCoding for a BS template
type DistanceCoding =
    | Undef
    | BsKm // with template BsKm
    | SingleValue // unique single value
    | MainTrack // multiple values for SBHF or BHF

type PrecodedStation =
    { distanceCoding: DistanceCoding
      symbols: string []
      distances: float []
      name: string }

type Station = { km: float; name: string }

let findBahnhofName (p: Parameter) =
    match p with
    | Composite (_, cl) ->
        match getFirstLinkInList cl with
        | Some (link) -> Some(textOfLink link)
        | _ -> None
    | String (_, n) -> Some(n)
    | _ -> None

let matchesType (parameters: List<Parameter>) (types: string []) =
    parameters
    |> List.exists (fun t ->
        match t with
        | String (n, v) when types |> Array.exists (fun t -> t = v) -> true
        | _ -> false)

let findDistanceCoding (symbols: string []) (p: Parameter) (name: string) =
    try
        match p with
        | String (_, km) ->
            let km0 = km.Replace("(", "").Replace(")", "")

            let kms = km0.Split " " |> Array.map parse2float
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
            else
                printfn
                    "*** findDistanceCoding, %s, distances.Length %d <> symbols.Length %d"
                    name
                    kms.Length
                    symbols.Length
                None
        | Composite (_, cl) ->
            match cl with
            | Composite.Template (n, lp) :: _ when n = "BSkm" && lp.Length = 2 ->
                match (getFirstStringValue lp.[0]), (getFirstStringValue lp.[1]) with
                | Some (km), Some (k2) ->
                    Some
                        ({ distanceCoding = DistanceCoding.BsKm
                           symbols = [||]
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

                if kms.Length = symbols.Length then
                    Some
                        ({ distanceCoding = DistanceCoding.MainTrack
                           symbols = symbols
                           distances = kms
                           name = name })
                else
                    printfn
                        "*** findDistanceCoding, %s, distances.Length %d <> symbols.Length %d"
                        name
                        kms.Length
                        symbols.Length
                    None
        | _ -> None
    with ex ->
        printfn "error: %A, findKm parameter %A" ex p
        None

let findSymbols (parameters: List<Parameter>) =
    getParameterStrings parameters
    |> List.map (fun s ->
        match s with
        | Parameter.String (_, str) -> str
        | _ -> "")
    |> List.filter (fun s -> bhftypes |> Array.contains s)
    |> List.toArray

let createPrecodedStation (symbols: string []) (p1: Parameter) (p2: Parameter) =
    match findBahnhofName p2 with
    | Some (name) -> findDistanceCoding symbols p1 name
    | _ -> None

let findPrecodedStation (t: Template) =
    try
        match t with
        | (n, l) when ("BS" = n || "BSe" = n)
                      && l.Length
                      >= 3
                      && (matchesType (List.take 1 l) bhftypes) ->
            createPrecodedStation (findSymbols (List.take 1 l)) l.[1] l.[2]
        | (n, l) when "BS2" = n
                      && l.Length >= 4
                      && (matchesType (List.take 2 l) bhftypes) ->
            createPrecodedStation (findSymbols (List.take 2 l)) l.[2] l.[3]
        | (n, l) when "BS3" = n
                      && l.Length >= 5
                      && (matchesType (List.take 3 l) bhftypes) ->
            createPrecodedStation (findSymbols (List.take 3 l)) l.[3] l.[4]
        | (n, l) when "BS4" = n
                      && l.Length >= 6
                      && (matchesType (List.take 4 l) bhftypes) ->
            createPrecodedStation (findSymbols (List.take 4 l)) l.[4] l.[5]
        | _ -> None
    with ex ->
        printfn "*** error %A\n  template %A" ex t
        None

let guessDistanceCoding (stations: PrecodedStation []) =
    let groups =
        stations
        |> Array.map (fun s -> s.distanceCoding)
        |> Array.groupBy id
        |> Array.map (fun g ->
            let (d, _) = g
            d)

    if groups.Length = 1
       && groups
          |> Array.contains DistanceCoding.SingleValue then
        groups.[0]
    else if groups.Length = 2
            && groups |> Array.contains DistanceCoding.BsKm
            && groups
               |> Array.contains DistanceCoding.SingleValue then
        DistanceCoding.BsKm
    else if groups.Length = 2
            && groups |> Array.contains DistanceCoding.MainTrack
            && groups
               |> Array.contains DistanceCoding.SingleValue then
        DistanceCoding.MainTrack
    else
        printfn "guessDistanceCoding %A failed" groups
        DistanceCoding.Undef

let filterPrecodedStationsByStrategy (fromTo: string [])
                                     (stations: PrecodedStation [])
                                     (canChange: PrecodedStation -> bool)
                                     (getDistance0: PrecodedStation -> float)
                                     (getDistance1: PrecodedStation -> float)
                                     : Station [] =
    let indexes = ResizeArray<int>()

    if stations.Length > 0 then
        match (fromTo
               |> Array.tryFindIndex stations.[0].name.StartsWith) with
        | Some index -> indexes.Add index
        | _ -> ()

    let mutable routeActive = fromTo.Length = 0 || indexes.Count > 0

    let arbhfOfRoute = ResizeArray<Station>()
    for bhf in stations do
        if routeActive then
            arbhfOfRoute.Add
                { km = getDistance0 bhf
                  name = bhf.name }
        if canChange bhf then
            let index =
                fromTo |> Array.tryFindIndex bhf.name.StartsWith

            let routeWillChange =
                index.IsSome && not (indexes.Contains index.Value)

            if index.IsSome && routeWillChange then indexes.Add(index.Value)

            if routeActive && routeWillChange then
                routeActive <- false
            else if not routeActive && routeWillChange then
                routeActive <- true
                arbhfOfRoute.Add
                    { km = getDistance1 bhf
                      name = bhf.name }

    if routeActive && stations.Length > 0 then // close route for error handling
        routeActive <-
            not
                (fromTo
                 |> Array.exists stations.[stations.Length - 1].name.StartsWith)
    if routeActive && fromTo.Length > 0
    then printfn "*** filterRoute: some of %A not found" fromTo
    arbhfOfRoute.ToArray()

let filterPrecodedStationsBySingleValue (fromTo: string []) (stations: PrecodedStation []): Station [] =
    filterPrecodedStationsByStrategy
        fromTo
        stations
        (fun s -> true)
        (fun s -> s.distances.[0])
        (fun s -> s.distances.[0])

let filterPrecodedStationsByBsKm (fromTo: string []) (stations: PrecodedStation []): Station [] =
    filterPrecodedStationsByStrategy
        fromTo
        stations
        (fun s -> s.distanceCoding = DistanceCoding.BsKm)
        (fun s -> s.distances.[0])
        (fun s -> s.distances.[1])

let filterMainTrack (s: PrecodedStation): Station =
    let distance =
        if s.distances.Length > 1 then
            let maybeIndex =
                s.symbols
                |> Array.tryFindIndex (fun sym -> BhfSymbols |> Array.contains sym)

            match maybeIndex with
            | Some index when index < s.distances.Length -> s.distances.[index]
            | _ -> -1.0
        else
            s.distances.[0]

    { km = distance; name = s.name }

let filterPrecodedStationsByMainTrack (fromTo: string []) (stations: PrecodedStation []): Station [] =
    stations |> Array.map filterMainTrack

let filterPrecodedStations (fromTo: string []) (stations: PrecodedStation []) =
    let dc = guessDistanceCoding stations
    if dc = DistanceCoding.SingleValue
    then filterPrecodedStationsBySingleValue fromTo stations
    else if dc = DistanceCoding.BsKm
    then filterPrecodedStationsByBsKm fromTo stations
    else if dc = DistanceCoding.MainTrack
    then filterPrecodedStationsByMainTrack fromTo stations
    else Array.empty

let findBahnhöfe (templates: Template []) (fromTo: string []) =
    templates
    |> Array.map findPrecodedStation
    |> Array.choose id
    |> filterPrecodedStations fromTo
