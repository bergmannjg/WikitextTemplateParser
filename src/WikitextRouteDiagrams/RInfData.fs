/// load rail network register of infrastructure data from file, see https://rinf.era.europa.eu
module RInfData

open FSharp.Data
open System.Collections.Generic
open Serializer
open Types
open DbData
open System.Text.RegularExpressions

type SectionOfLine =
    { IMName: int
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
        loadCsvData "./dbdata/RINF/SectionOfLines.csv" (fun dict row ->
            let data =
                { IMName = row.[0].AsInteger()
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
        loadCsvData "./dbdata/RINF/OperationalPoints.csv" (fun dict row ->
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

let rec private sortEdges (data: List<SectionOfLine>) =
    let rec collect (data: seq<SectionOfLine>)
                    (curr: SectionOfLine)
                    (follows: SectionOfLine -> SectionOfLine -> bool)
                    (sorted: ResizeArray<SectionOfLine>)
                    =
        match data |> Seq.tryFind (follows curr) with
        | Some next ->
            if not (sorted.Exists(fun s -> s.OperationalPointStart = next.OperationalPointStart)) then
                sorted.Add(next)
                collect data next follows sorted
            else
                sorted
        | None -> sorted

    if data.Count = 0 then
        Seq.empty
    else
        let h = data |> Seq.head

        let sortedBefore =
            collect data h (fun h d -> h.OperationalPointStart = d.OperationalPointEnd) (ResizeArray())
            |> Seq.rev

        let sortedAfter =
            collect data h (fun h d -> h.OperationalPointEnd = d.OperationalPointStart) (ResizeArray())

        let sorted =
            Seq.concat [ sortedBefore
                         [ h ] :> seq<SectionOfLine>
                         sortedAfter :> seq<SectionOfLine> ]
            |> Seq.toArray

        if data.Count > sorted.Length then
            let rest =
                sortEdges (data.FindAll(fun d -> not (sorted |> Array.exists (fun s -> s = d))))

            Seq.concat [ sorted :> seq<SectionOfLine>
                         rest ]
        else
            sorted :> seq<SectionOfLine>

let loadSoL routenr =
    match (loadSoLCsvDataCached ()).TryGetValue routenr with
    | true, data when data.Count > 0 -> sortEdges (data)
    | _ -> Seq.empty

let private regexRoutes = Regex(@"StrUeb_(\d{4})_(\d{4})")

let private findEntry opname routenr startname =
    let mc = regexRoutes.Matches opname
    if mc.Count > 0 then
        let m = mc |> Seq.head
        if m.Groups.Count = 3 then
            let routenr1 = m.Groups.[1].Value |> int
            let routenr2 = m.Groups.[2].Value |> int

            let routenrToSearch =
                if routenr1 = routenr then routenr2 else routenr1

            loadSoL routenrToSearch
            |> Seq.tryFind (fun e -> e.OperationalPointStart = startname)
        else
            None
    else
        None

let private maybeLoadRouteStartSoL routenr (opname: string) =
    match loadRouteStart routenr with
    | Some (name, km) -> if name = "" || name = opname then (km, None) else (km, findEntry opname routenr name)
    | _ -> (0.0, None)

// todo
let private adhocPreReplacements routenr (edges: SectionOfLine []) =
    if routenr = 6425 then
        edges
        |> Array.filter (fun op -> op.OperationalPointStart = "Bad Harzburg")
    else
        edges

// todo
let private adhocPostReplacements routenr (ops: DbOpPointOfRoute []) =
    if routenr = 2610 then
        ops
        |> Array.filter (fun op ->
            op.name
            <> "Dormagen Chempark        (Nordbahnsteig)")
        |> Array.map (fun op ->
            if op.name = "Dormagen Chempark         (Südbahnsteig)"
            then { op with name = "Dormagen Chempark" }
            else op)
    else
        ops

let loadRoute routenr =
    let edges = 
            loadSoL routenr |> Seq.toArray
            |> adhocPreReplacements routenr

    if edges.Length = 0 then
        Array.empty
    else
        let mutable (kmCurr, maybeSoL) =
            maybeLoadRouteStartSoL routenr edges.[0].OperationalPointStart

        let h =
            if maybeSoL.IsSome then
                let (k, a) =
                    (loadKuerzelArt maybeSoL.Value.OperationalPointStart)

                [ { km = 0.0 // maybeSoL.Value.LengthOfSoL should be eqaul to kmCurr
                    name = maybeSoL.Value.OperationalPointStart
                    STELLE_ART = a
                    KUERZEL = k } ] :> seq<DbOpPointOfRoute>
            else
                Seq.empty

        let ops =
            edges
            |> Array.map (fun e ->
                let kmAct = kmCurr
                let (k, a) = (loadKuerzelArt e.OperationalPointStart)
                kmCurr <- e.LengthOfSoL + kmCurr
                { km = System.Math.Round(kmAct, 1)
                  name = e.OperationalPointStart
                  STELLE_ART = a
                  KUERZEL = k })
            |> adhocPostReplacements routenr

        let operationalPointEnd =
            edges.[edges.Length - 1].OperationalPointEnd

        let (k, a) = (loadKuerzelArt operationalPointEnd)

        Seq.concat [ h
                     ops :> seq<DbOpPointOfRoute>
                     [ { km = System.Math.Round(kmCurr, 1)
                         name = operationalPointEnd
                         STELLE_ART = a
                         KUERZEL = k } ] :> seq<DbOpPointOfRoute> ]
        |> Seq.toArray

let loadSoLAsJSon routenr =
    Serializer.Serialize<seq<SectionOfLine>>(loadSoL routenr)

let loadRouteAsJSon routenr =
    Serializer.Serialize<seq<DbOpPointOfRoute>>(loadRoute routenr)

let printSoL (data: seq<SectionOfLine>) =
    data
    |> Seq.iter (fun d ->
        printfn "%s %s %.1f %.1f" d.OperationalPointStart d.OperationalPointEnd d.LengthOfSoL d.LengthOfTracks)
