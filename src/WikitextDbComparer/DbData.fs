module DbData

open FSharp.Data

type DbStationOfRoute = { km: float; name: string }

type BetriebsstelleRailwayRoutePosition =
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

let private loadCsvData (path: string) (cp: int) (loader: CsvRow -> 'a): 'a [] =
    try
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance)

        use stream =
            System.IO.File.Open(path, System.IO.FileMode.Open)

        let streamReader =
            new System.IO.StreamReader(stream, System.Text.Encoding.GetEncoding(cp))

        CsvFile.Load(streamReader).Rows
        |> Seq.map loader
        |> Seq.toArray
    with ex ->
        fprintfn stderr "loadCsvData: %s %A" path ex
        Array.empty

let private loadBetriebsstellenCsvData () =
    loadCsvData "./dbdata/original/betriebsstellen_open_data.csv" 852 (fun row ->
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
          GEOGR_LAENGE = row.["GEOGR_LAENGE"].AsFloat() })

let private loadStreckenutzungCsvData () =
    loadCsvData "./dbdata/original/strecken_nutzung.csv" 1252 (fun row ->
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
          kmspru_typ_end = row.[15] })

let private loadDBRoutePosition routenr =
    let dbdata = loadBetriebsstellenCsvData ()

    dbdata
    |> Array.filter (fun p ->
        p.STRECKE_NR = routenr
        && bfStelleArt |> Array.contains p.STELLE_ART)

let loadDBStations routenr =
    loadDBRoutePosition routenr
    |> Array.map (fun p ->
        { km = getKMI2Float p.KM_I
          name = p.BEZEICHNUNG })

let checkPersonenzugStreckenutzung (strecke: int) =
    let dbdata = loadStreckenutzungCsvData ()

    let bahnnutzung =
        dbdata
        |> Array.filter (fun p -> p.strecke_nr = strecke)
        |> Array.map (fun s -> s.bahnnutzung)

    let hasBahnnutzung =
        bahnnutzung
        |> Array.exists (fun bn -> bn.Contains "Pz")

    hasBahnnutzung || bahnnutzung.Length = 0
