namespace WikitextRouteDiagrams

// fsharplint:disable RecordFieldNames

open FSharp.Collections

/// operational point of wiki data
type WkOpPointOfRoute =
    { kms: float []
      name: string
      shortname: string }

/// collect OpPointOfRoute data from OpPointOfInfobox data with corresponding RouteInfo
module OpPointOfRoute =

    module Data =
        let private toMap (title: string) (route: int) =
            Map
                .empty
                .Add("title", title)
                .Add("route", route.ToString())

        let collection = "WkStationOfRoute"

        let insert title route value =
            DataAccess.typedCollectionInsert<WkOpPointOfRoute []> collection (toMap title route) value

        let query title route =
            DataAccess.typedCollectionQuery<WkOpPointOfRoute []> "WkStationOfRoute" (toMap title route)

        let queryAsStrings title route =
            DataAccess.collectionQuery "WkStationOfRoute" (toMap title route)


    let private checkReplacedWithChar (s1: string) (s2: string) (c: char) = s1.Replace(c, ' ') = s2.Replace(c, ' ')

    let private checkReplaced (s1: string) (s2: string) = checkReplacedWithChar s1 s2 '-'

    /// getBestMatch, maybe 'takeFirst/takeLast' strategy is not enough
    let private getBestMatchWithStrategy
        (isExactMatch: bool)
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
            None

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
                |> Array.tryFindBack (fun s -> s.distances.Length > 0 && s.distances.[0] <> -1.0)

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

            if indedFrom < indedTo then
                Some(fromFirst, toLast)
            else
                Some(toFirst, fromLast) // reverse search
        | _ -> None

    let private maybeReplaceRouteNr (strecke: RouteInfo) =
        let candidate =
            AdhocReplacements.RouteInfo.maybeWrongRouteNr
            |> Array.tryFind
                (fun (title, routeWrong, route) ->
                    title = strecke.title
                    && routeWrong = strecke.nummer)

        match candidate with
        | Some (_, _, nr) -> { strecke with nummer = nr }
        | None -> strecke

    let private applyMaybeReplacements (strecke: RouteInfo) =
        match AdhocReplacements.RouteInfo.maybeReplaceRouteStation
              |> Array.tryFind (fun (title, route, _, _) -> title = strecke.title && route = strecke.nummer) with
        | Some (_, _, Some von, Some bis) -> { strecke with von = von; bis = bis }
        | Some (_, _, Some von, None) -> { strecke with von = von }
        | Some (_, _, None, Some bis) -> { strecke with bis = bis }
        | _ -> strecke

    let private tryFindReplacementsInRouteStation strecke name =
        match AdhocReplacements.RouteInfo.replacementsInRouteStation
              |> Array.tryFind
                  (fun (title, route, nameOld, _) ->
                      title = strecke.title
                      && route = strecke.nummer
                      && name = nameOld) with
        | Some (_, _, _, nameNew) -> Some nameNew
        | None -> None

    let private applyVonBisReplacements (strecke: RouteInfo) =
        match tryFindReplacementsInRouteStation strecke strecke.von, tryFindReplacementsInRouteStation strecke strecke.bis with
        | Some (von), Some (bis) -> { strecke with von = von; bis = bis }
        | Some (von), None -> { strecke with von = von }
        | None, Some (bis) -> { strecke with bis = bis }
        | _ -> strecke

    /// match RouteInfo station names with station names of templates
    let matchRouteInfoOpPoints (strecke0: RouteInfo) (stations: OpPointOfInfobox []) (refillPossible: bool) =
        let strecke =
            strecke0
            |> maybeReplaceRouteNr
            |> applyMaybeReplacements
            |> applyVonBisReplacements

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
    let findOpPointsOfRoute (strecke: RouteInfo) (stations: OpPointOfInfobox []) =
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
            |> Array.mapi
                (fun i v ->
                    if i >= indexFrom && i <= indexTo2 then
                        Some(
                            { kms = v.distances
                              name = v.name
                              shortname = v.shortname }
                        )
                    else
                        None)
            |> Array.choose id
        | _ -> Array.empty

    let findStations strecke templates =
        templates
        |> Array.map OpPointOfInfobox.findStationOfInfobox
        |> Array.choose id
        |> findOpPointsOfRoute strecke

    let dump (title: string) (strecke: RouteInfo) (stations: WkOpPointOfRoute []) =
        Data.insert title strecke.nummer stations
        |> ignore
