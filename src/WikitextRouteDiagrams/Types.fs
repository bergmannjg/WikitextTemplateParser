/// types
module Types

// fsharplint:disable RecordFieldNames

type DbStationOfRoute =
    { km: float
      name: string
      STELLE_ART: string
      KUERZEL: string }

type StationOfInfobox =
    { symbols: string []
      distances: float []
      name: string
      link: string
      shortname: string } // ds100

type StationOfRoute =
    { kms: float []
      name: string
      shortname: string }

type ResultKind =
    | WikidataFoundInDbData
    | StartStopStationsNotFound
    | WikidataNotFoundInTemplates
    | WikidataNotFoundInDbData
    | WikidataWithoutDistancesInDbData
    | NoDbDataFoundWithRailwayGuide
    | NoDbDataFoundWithoutRailwayGuide
    | RouteParameterNotParsed
    | RouteParameterEmpty
    | RouteIsNoPassengerTrain
    | RouteIsShutdown
    | Undef

/// result of route match
type ResultOfRoute =
    { route: int
      title: string
      fromToNameOrig: string []
      fromToNameMatched: string []
      fromToKm: float []
      resultKind: ResultKind
      countWikiStops: int
      countDbStops: int
      countDbStopsFound: int
      countDbStopsNotFound: int
      railwayGuide: string
      isCompleteDbRoute: bool }

/// kind of match of wk station name and db station name
type MatchKind =
    | Failed
    | EqualShortNames
    | EqualShortNamesNotDistance
    | EqualNames
    | StartsWith
    | EndsWith
    | EqualWithoutIgnored
    | EqualWithoutParentheses
    | Levenshtein
    | SameSubstring

/// result of station match
type ResultOfStation =
    | Success of DbStationOfRoute * StationOfRoute * MatchKind
    | Failure of DbStationOfRoute

/// view of ResultOfStation.Success
type StationOfDbWk =
    { dbname: string
      dbkm: float
      wkname: string
      wkkms: float []
      matchkind: MatchKind }

type RoutenameKind =
    | Empty
    | EmptyWithIgnored
    | SmallFormat
    | Parenthesis
    | Text
    | Unmatched

type RouteInfo =
    { nummer: int
      title: string
      von: string
      bis: string
      railwayGuide: string option
      routenameKind: RoutenameKind
      searchstring: string }
