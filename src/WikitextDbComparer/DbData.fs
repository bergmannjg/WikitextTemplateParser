module DbData

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

// 112240006 -> 122,4
let getKMI2Float (kmi: int) =
    let km = float (kmi - 100000000) / 100000.0
    System.Math.Round(km, 1)
