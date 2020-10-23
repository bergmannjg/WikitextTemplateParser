module DbData

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

// 112240006 -> 122,4
let getKMI2Float (kmi: int) =
    let km = float (kmi - 100000000) / 100000.0
    System.Math.Round(km, 1)

let bfStelleArt = [| "Bf"; "Bft"; "Hp" |]

let loadDBRoutePosition routenr =
    let dbtext =
        System.IO.File.ReadAllText("./dbdata/original/betriebsstellen_open_data.json")

    let dbdata =
        Serializer.Deserialize<BetriebsstelleRailwayRoutePosition []>(dbtext)

    dbdata
    |> Array.filter (fun p ->
        p.STRECKE_NR = routenr
        && bfStelleArt |> Array.contains p.STELLE_ART)

let loadDBStations routenr =
    loadDBRoutePosition routenr
    |>Array.map (fun p ->  {km = getKMI2Float p.KM_I; name = p.BEZEICHNUNG})

let checkPersonenzugStreckenutzung (strecke: int) =
    let dbtext =
        System.IO.File.ReadAllText("./dbdata/original/strecken_nutzung.json")

    let dbdata =
        Serializer.Deserialize<Streckenutzung []>(dbtext)

    let bahnnutzung =
        dbdata
        |> Array.filter (fun p -> p.strecke_nr = strecke)
        |> Array.map (fun s -> s.bahnnutzung)

    let hasBahnnutzung = bahnnutzung |> Array.exists (fun bn -> bn.Contains "Pz")

    hasBahnnutzung || bahnnutzung.Length = 0
