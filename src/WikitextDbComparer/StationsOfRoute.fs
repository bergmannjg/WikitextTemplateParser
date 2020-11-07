/// collect StationsOfRoute data from StationOfInfobox data with corresponding RouteInfo
module StationsOfRoute

open StationsOfInfobox
open RouteInfo
open FSharp.Collections

type StationOfRoute = { kms: float []; name: string }

let private checkReplacedWithChar (s1: string) (s2: string) (c: char) = s1.Replace(c, ' ') = s2.Replace(c, ' ')

let private checkReplaced (s1: string) (s2: string) = checkReplacedWithChar s1 s2 '-'

let private getMatch (nameInHeader: string) (nameInTemplate: string) =
    if nameInHeader.StartsWith nameInTemplate
       || nameInTemplate.StartsWith nameInHeader
       || nameInTemplate.EndsWith nameInHeader
       || checkReplaced nameInTemplate nameInHeader then
        Some nameInTemplate
    else
        let candidates =
            AdhocReplacements.abbreviationsInRoutename
            |> Array.map (fun (ab, s) ->
                if nameInTemplate.StartsWith(nameInHeader.Replace(ab, s))
                then Some nameInTemplate
                else None)
            |> Array.choose id

        if candidates.Length > 0 then Some candidates.[0] else None

/// getBestMatch, maybe 'takeFirst/takeLast' strategy is not enough
let private getBestMatch (nameInHeader: string) (namesInTemplate: string []) =
    let candidates =
        namesInTemplate
        |> Array.map (getMatch nameInHeader)
        |> Array.choose id

    if candidates.Length = 0
    then None
    else Some(candidates.[0], candidates.[candidates.Length - 1])

/// filter station list beginning and ending with a name  in 'fromTo'
let private filterStations0 (fromTo: string []) (stations: StationOfInfobox []) =
    let indexes =
        if stations.Length > 0 then
            match (fromTo
                   |> Array.tryFindIndex ((=) stations.[0].name)) with
            | Some index -> [ index ]
            | _ -> List.empty<int>
        else
            List.empty<int>

    let (stations, routeActive, indexes0) =
        stations
        |> Array.fold (fun (s: List<StationOfRoute>, routeActive: bool, indexes: List<int>) bhf ->
            let mutable (s0, routeActive0, indexes0) = (s, routeActive, indexes)

            if routeActive
            then s0 <- { kms = bhf.distances; name = bhf.name } :: s0
            if fromTo.Length > 0 then
                let index =
                    fromTo |> Array.tryFindIndex ((=) bhf.name)

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
           && stations.Length > 0 then // close route for error handling
            let routeActive0 =
                not
                    (fromTo
                     |> Array.exists ((=) stations.[stations.Length - 1].name))

            if routeActive0
            then fprintfn stderr "*** filterRoute: some of %A not found, found indexes %A" fromTo indexes0

            not routeActive0
        else
            true

    if routeClosed then List.toArray stations else Array.empty

let filterStations (strecke: RouteInfo) (stations: StationOfInfobox []) =
    filterStations0 [| strecke.von; strecke.bis |] stations

let findStations strecke templates =
    let precodedStations =
        templates
        |> Array.map findStationOfInfobox
        |> Array.choose id

    filterStations strecke precodedStations

let private refillRouteInfo (strecke: RouteInfo) (stations: StationOfInfobox []) =
    if stations.Length > 1
       && strecke.routenameKind <> RoutenameKind.Unmatched then

        let first =
            stations
            |> Array.tryFind (fun s -> s.distances.Length > 0)

        let last =
            stations
            |> Array.tryFindBack (fun s -> s.distances.Length > 0)

        { nummer = strecke.nummer
          title = strecke.title
          von =
              match first with
              | Some s -> s.name
              | None -> ""
          bis =
              match last with
              | Some s -> s.name
              | None -> ""
          railwayGuide = strecke.railwayGuide
          routenameKind = strecke.routenameKind
          searchstring = strecke.searchstring }
    else
        strecke

let private getMatchedRouteStations (von: string) (bis: string) (stations: StationOfInfobox []) =
    let namesInTemplate = stations |> Array.map (fun s -> s.name)

    let matchFrom = getBestMatch von namesInTemplate

    let matchTo = getBestMatch bis namesInTemplate

    match matchFrom, matchTo with
    | Some (fromFirst, fromLast), Some (toFirst, toLast) ->
        let indedFrom =
            namesInTemplate |> Array.findIndex ((=) fromFirst)

        let indedTo =
            namesInTemplate |> Array.findIndex ((=) toLast)

        if indedFrom <= indedTo then Some(fromFirst, toLast) else Some(toFirst, fromLast) // reverse search
    | _ -> None

let private maybeReplaceRouteNr (strecke: RouteInfo) =
    let candidate =
        AdhocReplacements.maybeWrongRoutes
        |> Array.tryFind (fun (title, routeWrong, route) ->
            title = strecke.title
            && routeWrong = strecke.nummer)

    match candidate with
    | Some (_, _, nr) -> { strecke with nummer = nr }
    | None -> strecke

/// match RouteInfo station names with station names of templates
let getMatchedRouteInfo (strecke0: RouteInfo) (stations: StationOfInfobox []) (refillPossible: bool) =
    let strecke = maybeReplaceRouteNr strecke0
    match getMatchedRouteStations strecke.von strecke.bis stations with
    | Some (f, t) -> { strecke with von = f; bis = t }
    | _ -> if refillPossible then refillRouteInfo strecke stations else strecke

let dump (title: string) (strecke: RouteInfo) (stations: StationOfRoute []) =
    let json =
        (Serializer.Serialize<StationOfRoute []>(stations))

    System.IO.File.WriteAllText
        ("./dump/"
         + title
         + "-"
         + strecke.nummer.ToString()
         + "-StationOfRoute.json",
         json)
