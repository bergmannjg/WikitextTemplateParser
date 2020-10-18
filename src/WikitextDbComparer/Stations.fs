module Stations

open Ast
open PrecodedStation
open AstUtils
open System.Text.RegularExpressions
open FSharp.Collections

type Station = { km: float; name: string }

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
        fprintfn stderr "guessDistanceCoding %A failed" groups
        DistanceCoding.Undef

// filter station list beginning and ending with a name  in 'fromTo'
let filterStationsByStrategy (fromTo: string [])
                             (precodedStations: PrecodedStation [])
                             (canChange: PrecodedStation -> bool)
                             (mustChange: bool)
                             (getDistance0: PrecodedStation -> float)
                             (getDistance1: PrecodedStation -> float)
                             : Station [] =
    let indexes =
        if precodedStations.Length > 0 then
            match (fromTo
                   |> Array.tryFindIndex precodedStations.[0].name.StartsWith) with
            | Some index -> [ index ]
            | _ -> List.empty<int>
        else
            List.empty<int>

    let (stations, routeActive, indexes0) =
        precodedStations
        |> Array.fold (fun (s: List<Station>, routeActive: bool, indexes: List<int>) bhf ->
            let mutable (s0, routeActive0, indexes0) = (s, routeActive, indexes)

            if routeActive then
                s0 <-
                    { km = getDistance0 bhf
                      name = bhf.name }
                    :: s0
            if fromTo.Length > 0 && canChange bhf then
                let index =
                    fromTo |> Array.tryFindIndex bhf.name.StartsWith

                let routeWillChange =
                    (routeActive && mustChange)
                    || (index.IsSome
                        && not (List.exists ((=) index.Value) indexes))

                if index.IsSome && routeWillChange then indexes0 <- index.Value :: indexes0

                if routeActive && routeWillChange then
                    routeActive0 <- false
                else if not routeActive && routeWillChange then
                    routeActive0 <- true
                    s0 <-
                        { km = getDistance1 bhf
                          name = bhf.name }
                        :: s0

            (s0, routeActive0, indexes0)) (List.empty<Station>, fromTo.Length = 0 || (not indexes.IsEmpty), indexes)

    let routeClosed =
        if routeActive
           && fromTo.Length > 0
           && fromTo.Length <> indexes0.Length
           && precodedStations.Length > 0 then // close route for error handling
            let routeActive0 =
                not
                    (fromTo
                     |> Array.exists precodedStations.[precodedStations.Length - 1].name.StartsWith)

            if routeActive0
            then fprintfn stderr "*** filterRoute: some of %A not found, found indexes %A" fromTo indexes0

            not routeActive0
        else
            true

    if routeClosed then List.toArray stations else Array.empty

let filterStationsBySingleValue (fromTo: string []) (stations: PrecodedStation []): Station [] =
    filterStationsByStrategy fromTo stations (fun s -> true) false (fun s -> s.distances.[0]) (fun s -> s.distances.[0])

let filterStationsByBsKm (fromTo: string []) (stations: PrecodedStation []): Station [] =
    filterStationsByStrategy
        fromTo
        stations
        (fun s -> s.distanceCoding = DistanceCoding.BsKm)
        true
        (fun s -> s.distances.[0])
        (fun s -> s.distances.[1])

let filterMainTrack (s: PrecodedStation): Station =
    let distance = s.distances.[s.distances.Length - 1] // adhoc, last entry may be on main track

    { km = distance; name = s.name }

let filterStationsByMainTrack (fromTo: string []) (stations: PrecodedStation []): Station [] =
    stations |> Array.map filterMainTrack

let filterStations (strecke: Strecke) (stations: PrecodedStation []) =
    let fromTo = [| strecke.von; strecke.bis |]

    match guessDistanceCoding stations with
    | DistanceCoding.SingleValue -> filterStationsBySingleValue fromTo stations
    | DistanceCoding.BsKm -> filterStationsByBsKm fromTo stations
    | DistanceCoding.MainTrack -> filterStationsByMainTrack fromTo stations
    | _ -> Array.empty

let findStations strecke templates =
    let precodedStations =
        templates
        |> Array.takeWhile (containsBorderStation >> not)
        |> Array.map findPrecodedStation
        |> Array.choose id

    filterStations strecke precodedStations
