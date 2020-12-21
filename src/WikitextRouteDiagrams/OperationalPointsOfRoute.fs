/// collect OpPointOfRoute data from OpPointOfInfobox data with corresponding RouteInfo
module OpPointsOfRoute

// fsharplint:disable RecordFieldNames

open Types
open OpPointsOfInfobox
open RouteInfo
open FSharp.Collections

let private checkReplacedWithChar (s1: string) (s2: string) (c: char) = s1.Replace(c, ' ') = s2.Replace(c, ' ')

let private checkReplaced (s1: string) (s2: string) = checkReplacedWithChar s1 s2 '-'

let getMatchInReplacement (isExactMatch: bool)
                          (strecke: RouteInfo)
                          (nameInTemplate: string)
                          (nameInHeader: string)
                          (abbr: string)
                          (r: string)
                          =
    let matcher =
        if isExactMatch then (=) else (fun (x: string) y -> x.StartsWith(y))

    if nameInHeader = abbr then
        if matcher nameInTemplate r then
            /// used by AdhocReplacements.replacementsInRouteStation
            fprintfn stderr "*** replace in route (\"%s\", %d, \"%s\", \"%s\")" strecke.title strecke.nummer abbr r
            true
        else
            false
    else
        matcher nameInTemplate nameInHeader

let private getMatchInReplacements (isExactMatch: bool)
                                   (strecke: RouteInfo)
                                   (nameInHeader: string)
                                   (nameInTemplate: string)
                                   =
    let candidates =
        AdhocReplacements.RouteInfo.replacementsInRouteStation
        |> Array.filter (fun (title, route, _, _) ->
            (route = 0 && title = strecke.title)
            || route = strecke.nummer)
        |> Array.map (fun (_, _, ab, s) ->
            if getMatchInReplacement isExactMatch strecke nameInTemplate nameInHeader ab s
            then Some nameInTemplate
            else None)
        |> Array.choose id

    if candidates.Length > 0 then Some candidates.[0] else None

/// getBestMatch, maybe 'takeFirst/takeLast' strategy is not enough
let private getBestMatchWithStrategy (isExactMatch: bool)
                                     (strecke: RouteInfo)
                                     (nameInHeader: string)
                                     (namesInTemplate: string [])
                                     =
    let matcher =
        if isExactMatch then
            (=)
        else
            (fun (x: string) (y: string) ->
                x.StartsWith y
                || y.StartsWith x
                || y.EndsWith x
                || checkReplaced y x)

    let exactCandidates =
        namesInTemplate
        |> Array.filter (matcher nameInHeader)

    if exactCandidates.Length > 0 then
        Some(exactCandidates.[0], exactCandidates.[exactCandidates.Length - 1])
    else
        let candidates =
            namesInTemplate
            |> Array.map (getMatchInReplacements isExactMatch strecke nameInHeader)
            |> Array.choose id

        if candidates.Length = 0
        then None
        else Some(candidates.[0], candidates.[candidates.Length - 1])

let private getBestMatch (strecke: RouteInfo) (nameInHeader: string) (namesInTemplate: string []) =
    match getBestMatchWithStrategy true strecke nameInHeader namesInTemplate with
    | Some result -> Some result
    | None -> getBestMatchWithStrategy false strecke nameInHeader namesInTemplate

let private refillRouteInfo (strecke: RouteInfo) (stations: OpPointOfInfobox []) =
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

let private getIndexTo (strecke: RouteInfo) (indexFrom: int) (name: string) (namesInTemplate: string []) =
    let index1 =
        namesInTemplate |> Array.findIndex ((=) name)

    let index2 =
        namesInTemplate |> Array.findIndexBack ((=) name)

    /// special handling for multiple occurrence of name 
    if strecke.bis = name
       && index1 <> index2
       && indexFrom < index2 then
        index2
    else
        index1

let private getMatchedRouteStations (strecke: RouteInfo) (stations: OpPointOfInfobox []) =
    let namesInTemplate =
        stations
        |> Array.filter (fun s -> s.distances.Length > 0)
        |> Array.map (fun s -> s.name)

    let matchFrom =
        getBestMatch strecke strecke.von namesInTemplate

    let matchTo =
        getBestMatch strecke strecke.bis namesInTemplate

    match matchFrom, matchTo with
    | Some (fromFirst, fromLast), Some (toFirst, toLast) ->
        let indedFrom =
            namesInTemplate |> Array.findIndex ((=) fromFirst)

        let indedTo =
            namesInTemplate
            |> getIndexTo strecke indedFrom toLast

        if indedFrom < indedTo then Some(fromFirst, toLast) else Some(toFirst, fromLast) // reverse search
    | _ -> None

let private maybeReplaceRouteNr (strecke: RouteInfo) =
    let candidate =
        AdhocReplacements.RouteInfo.maybeWrongRouteNr
        |> Array.tryFind (fun (title, routeWrong, route) ->
            title = strecke.title
            && routeWrong = strecke.nummer)

    match candidate with
    | Some (_, _, nr) -> { strecke with nummer = nr }
    | None -> strecke

let private maybeReplaceRouteStation (strecke: RouteInfo) =
    match AdhocReplacements.RouteInfo.maybeReplaceRouteStation
          |> Array.tryFind (fun (title, route, _, _) -> title = strecke.title && route = strecke.nummer) with
    | Some (_, _, Some von, Some bis) -> { strecke with von = von; bis = bis }
    | Some (_, _, Some von, None) -> { strecke with von = von }
    | Some (_, _, None, Some bis) -> { strecke with bis = bis }
    | _ -> strecke

/// match RouteInfo station names with station names of templates
let findRouteInfoStations (strecke0: RouteInfo) (stations: OpPointOfInfobox []) (refillPossible: bool) =
    let strecke =
        strecke0
        |> maybeReplaceRouteNr
        |> maybeReplaceRouteStation

    match getMatchedRouteStations strecke stations with
    | Some (f, t) -> { strecke with von = f; bis = t }
    | _ ->
        if refillPossible then
            refillRouteInfo strecke stations
        else
            { strecke with
                  routenameKind = Unmatched }

let private equalsStationOfInfobox (name: string) (station: OpPointOfInfobox) =
    station.name = name
    && station.distances.Length > 0

/// filter station list beginning and ending with nameFrom and nameTo
let filterStations (strecke: RouteInfo) (stations: OpPointOfInfobox []) =
    let maybeIndex1 =
        stations
        |> Array.tryFindIndex (equalsStationOfInfobox strecke.von)

    let maybeIndex2 =
        stations
        |> Array.tryFindIndex (equalsStationOfInfobox strecke.bis)

    match maybeIndex1, maybeIndex2 with
    | Some (indexFrom), Some (indexTo)
    | Some (indexTo), Some (indexFrom) when indexFrom < indexTo ->
        // try find a further index
        let indexTo2 =
            if stations.Length > (indexTo + 1) then
                match stations
                      |> Array.skip (indexTo + 1)
                      |> Array.tryFindIndex (equalsStationOfInfobox strecke.bis) with
                | Some i -> indexTo + 1 + i
                | None -> indexTo
            else
                indexTo

        stations
        |> Array.mapi (fun i v ->
            if i >= indexFrom && i <= indexTo2 then
                Some
                    ({ kms = v.distances
                       name = v.name
                       shortname = v.shortname })
            else
                None)
        |> Array.choose id
    | _ -> Array.empty

let findStations strecke templates =
    templates
    |> Array.map findStationOfInfobox
    |> Array.choose id
    |> filterStations strecke

let dump (title: string) (strecke: RouteInfo) (stations: WkOpPointOfRoute []) =
    DataAccess.WkOpPointOfRoute.insert title strecke.nummer stations
    |> ignore
