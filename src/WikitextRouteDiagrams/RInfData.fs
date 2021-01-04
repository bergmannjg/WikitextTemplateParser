/// load rail network register of infrastructure data from file, see https://rinf.era.europa.eu
module RInfData

open FSharp.Data
open System.Collections.Generic
open Types
open System.Text.RegularExpressions

type SectionOfLine =
    { Component: int
      IMName: int
      NationalLine: int
      OperationalPointStart: string
      OperationalPointStartLocation: string
      OperationalPointEnd: string
      OperationalPointEndLocation: string
      LengthOfSoL: float
      LengthOfTracks: float }

type OperationalPoint =
    { Name: string
      UOPID: string
      Type: string
      GeographicalLocation: string }

let private memoize<'k, 'a> f =
    let mutable cache: Dictionary<'k, ResizeArray<'a>> option = None

    (fun () ->
        match cache with
        | Some data -> data
        | None ->
            cache <- Some(f ())
            cache.Value)

let private add<'k, 'a when 'k: comparison> (key: 'k) (data: 'a) (dict: Dictionary<'k, ResizeArray<'a>>) =
    match dict.TryGetValue(key) with
    | true, l -> l.Add(data)
    | _ ->
        let l = ResizeArray()
        l.Add(data)
        dict.Add(key, l)

let private loadCsvData<'k, 'a when 'k: comparison> path loader =
    try
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance)

        use stream =
            System.IO.File.Open(path, System.IO.FileMode.Open)

        let streamReader = new System.IO.StreamReader(stream)

        CsvFile.Load(streamReader, "|").Rows
        |> Seq.fold loader (Dictionary<'k, ResizeArray<'a>>())
    with ex ->
        fprintfn stderr "loadCsvData: %s %A" path ex
        Dictionary<'k, ResizeArray<'a>>()

let private loadSoLCsvData () =
    let data =
        loadCsvData
            "./dbdata/RINF/SectionOfLines.csv"
            (fun dict row ->
                let data =
                    { Component = 0
                      IMName = row.[0].AsInteger()
                      NationalLine = row.[1].AsInteger()
                      OperationalPointStart = row.[2]
                      OperationalPointStartLocation = row.[3]
                      OperationalPointEnd = row.[4]
                      OperationalPointEndLocation = row.[5]
                      LengthOfSoL = row.[6].Replace(",", ".").AsFloat()
                      LengthOfTracks = row.[7].Replace(",", ".").AsFloat() }

                dict |> add data.NationalLine data
                dict)

    data

let private loadOperationalPointsCsvData () =
    let data =
        loadCsvData
            "./dbdata/RINF/OperationalPoints.csv"
            (fun dict row ->
                let data =
                    { Name = row.[0]
                      UOPID = row.[1]
                      Type = row.[2]
                      GeographicalLocation = row.[3] }

                dict |> add data.Name data
                dict)

    data

let private loadSoLCsvDataCached = memoize loadSoLCsvData

let private loadOperationalPointsCsvDataCached = memoize loadOperationalPointsCsvData

let StelleArtWeiche = "Weiche"
let StelleArtGrenze = "Grenze"

let convertType (typeOfOp: string) =
    match typeOfOp with
    | "station" -> "Bf"
    | "passenger stop" -> "Hp"
    | "junction" -> "Abzw"
    | "switch" -> StelleArtWeiche
    | "border point" -> StelleArtGrenze
    | "domestic border point" -> StelleArtGrenze
    | _ -> ""

let private loadKuerzelArt (name: string) =
    if name.StartsWith "StrUeb" then
        ("", "Abzw")
    else
        match loadOperationalPointsCsvDataCached().TryGetValue name with
        | true, data when data.Count > 0 && data.[0].UOPID.Length > 2 ->
            (data.[0].UOPID.Substring(2).Trim(), convertType data.[0].Type)
        | _ -> ("", "")

/// returns a list of sorted seqs
let rec sortEdges<'a, 's when 'a: equality and 's: equality>
    (nodeStart: 'a -> 's)
    (nodeEnd: 'a -> 's)
    (edges: List<'a>)
    =
    let rec collect (data: seq<'a>) (curr: 'a) (follows: 'a -> 'a -> bool) (sorted: List<'a>) =
        match data |> Seq.tryFind (follows curr) with
        | Some next ->
            if (not (sorted.Exists(fun s -> follows next s))) then // no cycles
                sorted.Add(next)
                collect data next follows sorted
            else
                sorted
        | None -> sorted

    if edges.Count = 0 then
        [ Seq.empty ]
    else
        let h = edges |> Seq.head

        let sortedBefore =
            collect edges h (fun h d -> nodeStart h = nodeEnd d) (List())
            |> Seq.rev

        let sortedAfter =
            collect edges h (fun h d -> nodeEnd h = nodeStart d) (List())

        let sorted =
            Seq.concat [ sortedBefore
                         [ h ] :> seq<'a>
                         sortedAfter :> seq<'a> ]

        if edges.Count > (Seq.length sorted) then
            let sortedRests =
                sortEdges nodeStart nodeEnd (edges.FindAll(fun d -> not (sorted |> Seq.exists (fun s -> s = d))))

            sorted :: sortedRests
        else
            [ sorted ]

let private loadSeqsOfSoL routenr =
    match (loadSoLCsvDataCached ()).TryGetValue routenr with
    | true, data when data.Count > 0 -> data
    | _ -> List<SectionOfLine>()

let private loadSeqsOfSoLSorted routenr =
    let data = loadSeqsOfSoL routenr

    if data.Count > 0 then
        let mutable componentNr = 0
        let nodeStart (n: SectionOfLine) = n.OperationalPointStart
        let nodeEnd (n: SectionOfLine) = n.OperationalPointEnd

        sortEdges nodeStart nodeEnd data
        |> List.map
            (fun seqs ->
                componentNr <- componentNr + 1

                seqs
                |> Seq.toList
                |> List.map (fun s -> { s with Component = componentNr }))
    else
        [ List.empty ]

let loadSoL routenr =
    loadSeqsOfSoLSorted routenr |> Seq.concat

let loadSoLAsJSon routenr =
    Serializer.Serialize<seq<SectionOfLine>>(loadSoL routenr)

/// get position in route for opname with ds100 or missingOperationalPoint data
let private getPositionInRouteSeq routenr opname =
    match loadKuerzelArt opname with
    | (ds100, _) when ds100 <> "" -> DbData.getPositionInRoute routenr ds100
    | _ -> None
    |> Option.orElseWith
        (fun _ ->
            AdhocReplacements.RInfData.missingOperationalPoint
            |> Array.tryFind (fun (r, name, _) -> r = routenr && name = opname)
            |> Option.bind (fun (_, _, km) -> Some km))

let isJunction (name: string) = name.StartsWith "StrUeb"

let private getNthPositionInRouteSeq routenr index (edges: SectionOfLine []) =
    if index >= edges.Length
       || isJunction edges.[index].OperationalPointStart then
        None
    else
        let dist =
            if index = 0 then
                0.0
            else
                edges
                |> Array.take (index)
                |> Array.sumBy (fun s -> s.LengthOfSoL)

        getPositionInRouteSeq routenr edges.[index].OperationalPointStart
        |> Option.map (fun km -> km - dist)

/// get absolute start position (of kilometering) in route using db open data, rinf data is only relative in SectionOfLine
let private getStartPositionInRoute routenr (edges: SectionOfLine []) =
    getNthPositionInRouteSeq routenr 0 edges
    |> Option.orElseWith (fun _ -> getNthPositionInRouteSeq routenr 1 edges)
    |> Option.orElseWith (fun _ -> getNthPositionInRouteSeq routenr 2 edges)
    |> Option.orElseWith (fun _ -> getNthPositionInRouteSeq routenr 3 edges)
    |> Option.fold (fun _ p -> p) 0.0

let private adhocPreReplacements routenr (edges: SectionOfLine []) =
    edges
    |> Array.map
        (fun op ->
            match AdhocReplacements.RInfData.replacementsOfLengthOfSoL
                  |> Array.tryFind
                      (fun (r, opStart, opEnd, _) ->
                          r = routenr
                          && opStart = op.OperationalPointStart
                          && opEnd = op.OperationalPointEnd) with
            | Some (_, _, _, length) -> { op with LengthOfSoL = length } // error in rinf data ??
            | None -> op)

let private adhocPostReplacements routenr (ops: DbOpPointOfRoute []) =
    match AdhocReplacements.RInfData.replacementsOfOperationalPoints
          |> Array.tryFind (fun (r, _, _, _) -> r = routenr) with
    | Some (_, op1, op2, opReplace) -> // op1 and op2 are replaced with opReplace
        ops
        |> Array.filter (fun op -> op.name <> op1)
        |> Array.map
            (fun op ->
                if op.name = op2 then
                    { op with name = opReplace }
                else
                    op)
    | None -> ops

let regexSpaces = Regex(@"\s\s+")

let regexJunction = Regex(@"StrUeb_(\d{4})_(\d{4})")

let private getStationToJunctionDistance routenr nameOfStation nameOfJunction =
    let m = regexJunction.Match nameOfJunction

    if m.Success && m.Groups.Count = 3 then
        let route1 = m.Groups.[1].Value |> int
        let route2 = m.Groups.[2].Value |> int

        let otherRoute =
            if route1 = routenr then
                route2
            else
                route1

        loadSeqsOfSoL otherRoute
        |> Seq.tryFind
            (fun s ->
                (s.OperationalPointStart = nameOfStation
                 && s.OperationalPointEnd = nameOfJunction)
                || (s.OperationalPointStart = nameOfJunction
                    && s.OperationalPointEnd = nameOfStation))
        |> Option.fold (fun _ s -> s.LengthOfSoL) 0.0
    else
        0.0

let private loadStationNearRouteJunction routenr nameOfJunction expectedKm =
    DbData.loadStartOfRoute routenr expectedKm
    |> Option.map
        (fun (nameOfStation, km) ->
            let dist =
                getStationToJunctionDistance routenr nameOfStation nameOfJunction

            (nameOfStation, max (km - dist) 0.0))

let private loadRouteOfSeq routenr (edges: SectionOfLine []) =

    if edges.Length = 0 then
        Seq.empty
    else
        let mutable kmCurr = getStartPositionInRoute routenr edges

        let h =
            if isJunction edges.[0].OperationalPointStart then
                match loadStationNearRouteJunction routenr edges.[0].OperationalPointStart kmCurr with
                | Some (name, km) ->
                    let (k, a) = (loadKuerzelArt name)

                    [| { km = km
                         name = name
                         STELLE_ART = a
                         KUERZEL = k } |]

                | None -> Array.empty
            else
                Array.empty

        let ops =
            edges
            |> Array.map
                (fun e ->
                    let kmAct = kmCurr
                    let (k, a) = (loadKuerzelArt e.OperationalPointStart)

                    kmCurr <- e.LengthOfSoL + kmCurr

                    { km = System.Math.Round(kmAct, 1)
                      name = StringUtilities.replaceFromRegexToString regexSpaces " " e.OperationalPointStart
                      STELLE_ART = a
                      KUERZEL = k })
            |> adhocPostReplacements routenr

        let operationalPointEnd =
            edges.[edges.Length - 1].OperationalPointEnd

        let (k, a) = (loadKuerzelArt operationalPointEnd)

        Seq.concat [ h
                     ops
                     [| { km = System.Math.Round(kmCurr, 1)
                          name = operationalPointEnd
                          STELLE_ART = a
                          KUERZEL = k } |] ]

let loadRoute routenr =
    loadSeqsOfSoLSorted routenr
    |> Seq.collect
        (fun edgesSeq ->
            let edges =
                edgesSeq
                |> Seq.toArray
                |> adhocPreReplacements routenr

            loadRouteOfSeq routenr edges)
    |> Seq.toArray
    |> Array.sortBy (fun op -> op.km)

let loadRouteAsJSon routenr =
    Serializer.Serialize<seq<DbOpPointOfRoute>>(loadRoute routenr)

let printSoL (data: seq<SectionOfLine>) =
    data
    |> Seq.iter
        (fun d ->
            printfn "%s %s %.1f %.1f" d.OperationalPointStart d.OperationalPointEnd d.LengthOfSoL d.LengthOfTracks)

let getRouteNumbers =
    (loadSoLCsvDataCached ()).Keys :> seq<int>

type SectionOfLineDbOpsDifference =
    { routenr: int
      sol: SectionOfLine
      opStart: DbOpPointOfRoute
      opEnd: DbOpPointOfRoute
      opDistance: float
      difference: float }

let compareDbDataOpDistances (routenr: int) =
    let rinfRoute = loadSoL routenr
    let dbRoute = DbData.loadRoute routenr

    rinfRoute
    |> Seq.map
        (fun s ->
            let (opStartKuerzel, _) = loadKuerzelArt s.OperationalPointStart
            let (opEndKuerzel, _) = loadKuerzelArt s.OperationalPointEnd

            let opStart =
                dbRoute
                |> Array.tryFind
                    (fun op ->
                        op.KUERZEL = opStartKuerzel
                        && opStartKuerzel.Length > 0)

            let opEnd =
                dbRoute
                |> Array.tryFind
                    (fun op ->
                        op.KUERZEL = opEndKuerzel
                        && opEndKuerzel.Length > 0)

            match opStart, opEnd with
            | Some (opS), Some (opE) ->
                let dist = abs (opE.km - opS.km)

                if (abs (dist - s.LengthOfSoL) > 0.2) then
                    Some(
                        { routenr = routenr
                          sol = s
                          opStart = opS
                          opEnd = opE
                          opDistance = System.Math.Round(dist, 1)
                          difference = System.Math.Round(abs (dist - s.LengthOfSoL), 1) }
                    )
                else
                    None
            | _ ->
                (*
                let isJunction =
                    s.OperationalPointStart.StartsWith("StrUeb")
                    || s.OperationalPointEnd.StartsWith("StrUeb")

                if not isJunction then
                    printfn
                        "not found route %d '%s' '%s' %.1f"
                        routenr
                        s.OperationalPointStart
                        s.OperationalPointEnd
                        s.LengthOfSoL
                *)
                None)
    |> Seq.choose id

let compareDbDataRoute (routenr: int) =
    let rinfRoute = loadRoute routenr
    let dbRoute = DbData.loadRoute routenr

    let missing =
        rinfRoute
        |> Seq.filter (fun r -> r.STELLE_ART = "Bf" || r.STELLE_ART = "Hp")
        |> Seq.filter
            (fun r ->
                not (
                    dbRoute
                    |> Seq.exists (fun d -> d.KUERZEL = r.KUERZEL)
                ))

    let count = missing |> Seq.length

    if count > 0 then
        printfn "route %d, %d missing, %A" routenr count missing

    (routenr, count)

let compareDbDataRoutes () =
    let keys = (loadSoLCsvDataCached ()).Keys

    let missing =
        keys
        |> Seq.map compareDbDataRoute
        |> Seq.filter (fun (r, m) -> m > 0)

    printfn "total %d routes, %d routes with missing data" (keys |> Seq.length) (missing |> Seq.length)

let compareDbDataOps () =
    (loadSoLCsvDataCached ()).Keys
    |> Seq.collect compareDbDataOpDistances
    |> Seq.iter
        (fun d ->
            printfn
                "distinct distance route %d '%s' '%s' rinf dist %.1f db dist %.1f diff %.1f"
                d.routenr
                d.sol.OperationalPointStart
                d.sol.OperationalPointEnd
                d.sol.LengthOfSoL
                d.opDistance
                d.difference)

let compareDbDataOpsAsJson () =
    let res =
        (loadSoLCsvDataCached ()).Keys
        |> Seq.collect compareDbDataOpDistances

    Serializer.Serialize<seq<SectionOfLineDbOpsDifference>>(res)

let private parseFloat (s: string) =
    match System.Double.TryParse(s) with
    | true, n -> Some n
    | _ -> None

/// from lat/lon to lon/lat
let private parseLocation (s: string) =
    let split = s.Replace(",", ".").Split()

    if split.Length = 2 then
        match parseFloat split.[0], parseFloat split.[1] with
        | Some f1, Some f2 -> Some(f2, f1)
        | _ -> None
    else
        None

let private getBRouterUrl (zoom: int) (lonStart: float, latStart: float) (lonlats: (float * float) list) =
    let lonlatString =
        lonlats
        |> List.map (fun (lon, lat) -> sprintf "%.7f,%.7f" lon lat)
        |> String.concat ";"

    sprintf
        "https://brouter.de/brouter-web/#map=%d/%.4f/%.4f/osm-mapnik-german_style&lonlats=%s&profile=rail"
        zoom
        latStart
        lonStart
        lonlatString

let getBRouterUrlOfSol routenr opStart opEnd =
    loadSeqsOfSoL routenr
    |> Seq.tryFind
        (fun s ->
            s.OperationalPointStart = opStart
            && s.OperationalPointEnd = opEnd)
    |> Option.bind
        (fun s ->
            match (parseLocation s.OperationalPointStartLocation), (parseLocation s.OperationalPointEndLocation) with
            | Some lonLatStart, Some lonLatEnd -> Some(getBRouterUrl 12 lonLatStart [ lonLatStart; lonLatEnd ])
            | _ -> None)

let getLengthOfRouteComponents routenr =
    loadSeqsOfSoLSorted routenr
    |> List.map (fun seqs -> seqs |> List.sumBy (fun s -> s.LengthOfSoL))

let getBRouterUrlOfRouteComponent routenr nr =
    let defaultUrl = getBRouterUrl 8 (9.871, 52.302) []

    let listOfSols = loadSeqsOfSoLSorted routenr

    if nr = 0
       || listOfSols.Length < nr
       || listOfSols.[nr - 1].Length = 0 then
        defaultUrl
    else
        let sols = listOfSols.[nr - 1]

        let lonlats =
            sols
            |> List.map (fun s -> parseLocation s.OperationalPointStartLocation)
            |> List.choose id

        let lastSol = sols.[sols.Length - 1]

        List.concat [ lonlats
                      parseLocation lastSol.OperationalPointEndLocation
                      |> Option.toList ]
        |> getBRouterUrl 9 lonlats.[0]
