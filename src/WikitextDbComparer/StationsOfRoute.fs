/// collect StationsOfRoute data from StationOfInfobox data with corresponding RouteInfo
module StationsOfRoute

open StationsOfInfobox
open RouteInfo
open FSharp.Collections

type StationOfRoute = { kms: float []; name: string }

// adhoc
let abbreviations =
    [| ("Bln.", "Berlin")
       ("Dr.-Friedrichst.", "Dresden-Friedrichstadt")
       ("Frankfurt(M) Hbf", "Frankfurt (Main) Hbf")
       ("Frankfurt Hbf", "Frankfurt (Main) Hbf")
       ("Hdbg Hbf", "Heidelberg Hbf") |]

let startsWithAnyAbbreviation (s1: string) (s2: string) =
    if s1.StartsWith s2 then
        true
    else
        abbreviations
        |> Array.exists (fun (ab, s) -> s1.StartsWith(s2.Replace(ab, s)))

let matchStationNames (s1: string) (s2: string) = startsWithAnyAbbreviation s1 s2

/// filter station list beginning and ending with a name  in 'fromTo'
let filterStations (strecke: RouteInfo) (precodedStations: StationOfInfobox []) =
    let fromTo = [| strecke.von; strecke.bis |]

    let indexes =
        if precodedStations.Length > 0 then
            match (fromTo
                   |> Array.tryFindIndex (matchStationNames precodedStations.[0].name)) with
            | Some index -> [ index ]
            | _ -> List.empty<int>
        else
            List.empty<int>

    let (stations, routeActive, indexes0) =
        precodedStations
        |> Array.fold (fun (s: List<StationOfRoute>, routeActive: bool, indexes: List<int>) bhf ->
            let mutable (s0, routeActive0, indexes0) = (s, routeActive, indexes)

            if routeActive
            then s0 <- { kms = bhf.distances; name = bhf.name } :: s0
            if fromTo.Length > 0 then
                let index =
                    fromTo
                    |> Array.tryFindIndex (matchStationNames bhf.name)

                let routeWillChange =
                    (index.IsSome
                     && not (List.exists ((=) index.Value) indexes))

                if index.IsSome && routeWillChange then indexes0 <- index.Value :: indexes0

                if routeActive && routeWillChange then
                    routeActive0 <- false
                else if not routeActive && routeWillChange then
                    routeActive0 <- true
                    s0 <- { kms = bhf.distances; name = bhf.name } :: s0

            (s0, routeActive0, indexes0))
               (List.empty<StationOfRoute>, fromTo.Length = 0 || (not indexes.IsEmpty), indexes)

    let routeClosed =
        if routeActive
           && fromTo.Length > 0
           && fromTo.Length <> indexes0.Length
           && precodedStations.Length > 0 then // close route for error handling
            let routeActive0 =
                not
                    (fromTo
                     |> Array.exists (matchStationNames precodedStations.[precodedStations.Length - 1].name))

            if routeActive0
            then fprintfn stderr "*** filterRoute: some of %A not found, found indexes %A" fromTo indexes0

            not routeActive0
        else
            true

    if routeClosed then List.toArray stations else Array.empty

let findStations strecke templates =
    let precodedStations =
        templates
        |> Array.takeWhile (containsBorderStation >> not)
        |> Array.map findStationOfInfobox
        |> Array.choose id

    filterStations strecke precodedStations
