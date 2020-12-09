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
    | DbDataMissing
    | RouteParameterNotParsed
    | RouteParameterEmpty
    | RouteIsNoPassengerTrain
    | RouteIsShutdown
    | Undef

/// result of route match
type ResultOfRoute =
    { route: int
      title: string
      routesInTitle: int
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
    | EqualShortNames
    | EqualShortNamesNotDistance
    | EqualNames
    | EqualtNamesNotDistance
    | EqualWithoutIgnored
    | EqualWithoutParentheses
    | EqualOrderChanged
    | StartsWith
    | EndsWith
    | Levenshtein
    | SameSubstring
    | SameSubstringNotDistance
    | Failed

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
