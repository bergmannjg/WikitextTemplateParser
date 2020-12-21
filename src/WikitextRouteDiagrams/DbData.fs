/// load db data from csv files
module DbData

// fsharplint:disable RecordFieldNames

open FSharp.Data
open System.Collections.Generic
open Types

type Strecke =
    { STRNR: int
      KMANF_E: int
      KMEND_E: int
      KMANF_V: string
      KMEND_V: string
      STRNAME: string
      STRKURZN: string }

type OperationalPointRailwayRoutePosition =
    { STRECKE_NR: int
      RICHTUNG: int
      KM_I: int
      KM_L: string
      BEZEICHNUNG: string
      STELLE_ART: string
      KUERZEL: string
      GEOGR_BREITE: float
      GEOGR_LAENGE: float }

type Streckenutzung =
    { mifcode: string
      strecke_nr: int
      richtung: int
      laenge: int
      von_km_i: int
      bis_km_i: int
      von_km_l: string
      bis_km_l: string
      elektrifizierung: string
      bahnnutzung: string
      geschwindigkeit: string
      strecke_kurzn: string
      gleisanzahl: string
      bahnart: string
      kmspru_typ_anf: string
      kmspru_typ_end: string }

// i.e. 112240006 -> 122,4
let getKMI2Float (kmi: int) =
    let km = float (kmi - 100000000) / 100000.0
    System.Math.Round(km, 1)

let bfStelleArt =
    [| "Bf"
       "Bft"
       "Hp"
       "Abzw"
       "Bft Abzw"
       "Awanst" |]

let private memoize<'a> f =
    let mutable cache: Dictionary<int, ResizeArray<'a>> option = None
    (fun () ->
        match cache with
        | Some data -> data
        | None ->
            cache <- Some(f ())
            cache.Value)

let private add<'a> (key: int) (data: 'a) (dict: Dictionary<int, ResizeArray<'a>>) =
    match dict.TryGetValue(key) with
    | true, l -> l.Add(data)
    | _ ->
        let l = ResizeArray()
        l.Add(data)
        dict.Add(key, l)

let private loadCsvData path (cp: int) loader =
    try
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance)

        use stream =
            System.IO.File.Open(path, System.IO.FileMode.Open)

        let streamReader =
            new System.IO.StreamReader(stream, System.Text.Encoding.GetEncoding(cp))

        CsvFile.Load(streamReader).Rows
        |> Seq.fold loader (Dictionary<int, ResizeArray<'a>>())
    with ex ->
        fprintfn stderr "loadCsvData: %s %A" path ex
        Dictionary<int, ResizeArray<'a>>()

let private loadStreckenCsvData () =
    loadCsvData "./dbdata/original/strecken.csv" 852 (fun dict row ->
        let data =
            { STRNR = row.["STRNR"].AsInteger()
              KMANF_E = row.["KMANF_E"].AsInteger()
              KMEND_E = row.["KMEND_E"].AsInteger()
              KMANF_V = row.["KMANF_V"]
              KMEND_V = row.["KMEND_V"]
              STRNAME = row.["STRNAME"]
              STRKURZN = row.["STRKURZN"] }

        dict |> add data.STRNR data
        dict)

let private loadStreckenCsvDataCached = memoize loadStreckenCsvData

let private loadOperationalPointsCsvData () =
    loadCsvData "./dbdata/original/betriebsstellen_open_data.csv" 852 (fun dict row ->
        let data =
            { STRECKE_NR = row.["STRECKE_NR"].AsInteger()
              RICHTUNG = row.["RICHTUNG"].AsInteger()
              KM_I = row.["KM_I"].AsInteger()
              KM_L = row.["KM_L"]
              BEZEICHNUNG = row.["BEZEICHNUNG"]
              STELLE_ART = row.["STELLE_ART"]
              KUERZEL = row.["KUERZEL"]
              GEOGR_BREITE =
                  if System.String.IsNullOrEmpty(row.["GEOGR_BREITE"])
                  then 0.0
                  else row.["GEOGR_BREITE"].AsFloat()
              GEOGR_LAENGE = row.["GEOGR_LAENGE"].AsFloat() }

        dict |> add data.STRECKE_NR data
        dict)

let loadOperationalPointsCsvDataCached = memoize loadOperationalPointsCsvData

let private loadStreckenutzungCsvData () =
    loadCsvData "./dbdata/original/strecken_nutzung.csv" 1252 (fun dict row ->
        let data =
            { mifcode = row.[0]
              strecke_nr = row.[1].AsInteger()
              richtung = row.[2].AsInteger()
              laenge = row.[3].AsInteger()
              von_km_i = row.[4].AsInteger()
              bis_km_i = row.[5].AsInteger()
              von_km_l = row.[6]
              bis_km_l = row.[7]
              elektrifizierung = row.[8]
              bahnnutzung = row.[9]
              geschwindigkeit = row.[10]
              strecke_kurzn = row.[11]
              gleisanzahl = row.[12]
              bahnart = row.[13]
              kmspru_typ_anf = row.[14]
              kmspru_typ_end = row.[15] }

        dict |> add data.strecke_nr data
        dict)

let private loadStreckenutzungCsvDataCached = memoize loadStreckenutzungCsvData

let removeRest (name: string) (pattern: string) =
    let index = name.IndexOf(pattern)
    if index > 0 then name.Substring(0, index) else name

/// split route name like 'Bln-Spandau - Hamburg-Altona'
let private splitRoutename (streckekurzname: string) =
    let split = streckekurzname.Split " - "
    if (split.Length = 2) then split else Array.empty

/// ignore meters < 100
let kmIEqual (km_I0: int) (km_I1: int) = abs (km_I0 - km_I1) < 100

let private addRouteEndpoints (route: Strecke) (dbdata: seq<OperationalPointRailwayRoutePosition>) =
    let indexAnf =
        dbdata
        |> Seq.tryFind (fun d -> kmIEqual d.KM_I route.KMANF_E)

    let indexEnd =
        dbdata
        |> Seq.tryFind (fun d -> kmIEqual d.KM_I route.KMEND_E)

    let split = splitRoutename route.STRNAME

    if split.Length = 2
       && (indexAnf.IsNone || indexEnd.IsNone) then

        let isRailroadSwitch =
            AdhocReplacements.regexRailroadSwitch.IsMatch split.[0]

        let useDistance =
            not isRailroadSwitch
            || split.[0].StartsWith "Abzw "


        let station0 =
            split.[0]
            |> StringUtilities.replaceFromRegexToEmpty AdhocReplacements.regexRailroadSwitch
            |> StringUtilities.replaceFromListToEmpty [ "Abzw " ]

        let found0 =
            dbdata
            |> Seq.exists (fun d -> d.BEZEICHNUNG = station0)

        let x1 =
            if indexAnf.IsNone && not found0 then
                [ { STRECKE_NR = route.STRNR
                    RICHTUNG = 1
                    KM_I = if useDistance then route.KMANF_E else 100000000
                    KM_L = route.KMANF_V
                    BEZEICHNUNG = station0
                    STELLE_ART = "ANF"
                    KUERZEL = ""
                    GEOGR_BREITE = 0.0
                    GEOGR_LAENGE = 0.0 } ] :> seq<OperationalPointRailwayRoutePosition>
            else
                Seq.empty

        let station1 =
            split.[1]
            |> StringUtilities.replaceFromRegexToEmpty AdhocReplacements.regexRailroadSwitch

        let found1 =
            dbdata
            |> Seq.exists (fun d -> d.BEZEICHNUNG = station1)

        let x2 =
            if indexEnd.IsNone && not found1 then
                [ { STRECKE_NR = route.STRNR
                    RICHTUNG = 1
                    KM_I = route.KMEND_E
                    KM_L = route.KMEND_V
                    BEZEICHNUNG = split.[1]
                    STELLE_ART = "END"
                    KUERZEL = ""
                    GEOGR_BREITE = 0.0
                    GEOGR_LAENGE = 0.0 } ] :> seq<OperationalPointRailwayRoutePosition>
            else
                Seq.empty

        Seq.concat [ x1; dbdata; x2 ]
    else
        dbdata

/// load
/// name - route start op
/// km - reference of kilometering of line
let loadRouteStart routenr =
    match (loadStreckenCsvDataCached ()).TryGetValue routenr with
    | true, routes when routes.Count = 1 ->
        let route = routes.[0]
        let split = splitRoutename route.STRNAME

        if split.Length = 2 then
            // split.[0] may contain
            // - a junction 'Abzw j, W n'
            // - a junction with a station name 's, W n'
            // - a station name 's'

            let station =
                split.[0]
                |> StringUtilities.replaceFromRegexToEmpty AdhocReplacements.regexRailroadSwitch
                |> StringUtilities.replaceFromRegexToEmpty AdhocReplacements.regexTrackInStation
                |> StringUtilities.replaceFromListToEmpty [ "Abzw " ]

            let km = getKMI2Float route.KMANF_E
            if km >= 0.0 then Some(station, km) else Some("", km)
        else
            None
    | _ -> None

let private loadRoutePosition routenr =
    match (loadOperationalPointsCsvDataCached ()).TryGetValue routenr with
    | true, dbdata ->
        let dbdataOfRoute =
            dbdata
            |> Seq.filter (fun p -> bfStelleArt |> Array.contains p.STELLE_ART)

        if not (Seq.isEmpty dbdataOfRoute) then
            match (loadStreckenCsvDataCached ()).TryGetValue routenr with
            | true, routes when routes.Count = 1 -> addRouteEndpoints routes.[0] dbdataOfRoute // try fill incomplete db data
            | _ -> dbdataOfRoute
        else
            dbdataOfRoute
    | _ -> Seq.empty

let loadRoute routenr =
    loadRoutePosition routenr
    |> Seq.map (fun p ->
        { km = getKMI2Float p.KM_I
          name = p.BEZEICHNUNG
          STELLE_ART = p.STELLE_ART
          KUERZEL = p.KUERZEL })
    |> Seq.toArray

let checkPersonenzugStreckenutzung routenr =
    match (loadStreckenutzungCsvDataCached ()).TryGetValue routenr with
    | true, dbdata ->
        dbdata
        |> Seq.map (fun s -> s.bahnnutzung)
        |> Seq.forall (fun bn -> bn.Contains "Pz")
    | _ -> true

let dump (title: string) (strecke: int) (stations: DbOpPointOfRoute []) =
    DataAccess.DbOpPointOfRoute.insert title strecke stations
    |> ignore
