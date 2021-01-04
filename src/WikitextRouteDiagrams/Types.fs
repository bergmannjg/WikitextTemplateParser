/// types
module Types

// fsharplint:disable RecordFieldNames

/// operational point of db data
type DbOpPointOfRoute =
    { km: float
      name: string
      STELLE_ART: string
      KUERZEL: string }

/// operational point of wiki infobox
type OpPointOfInfobox =
    { symbols: string []
      distances: float []
      name: string
      link: string
      shortname: string } // ds100

/// operational point of wiki data
type WkOpPointOfRoute =
    { kms: float []
      name: string
      shortname: string }

/// result kind of route match
type ResultKind =
    | WikidataFoundInDbData
    | StartStopOpPointsNotFound
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

/// kind of match of wk operational point and db operational point
type MatchKind =
    | EqualShortNames
    | EqualShortNamesNotDistance
    | EqualNames
    | EqualtNamesNotDistance
    | EqualWithoutIgnoredNotDistance
    | EqualWithoutIgnored
    | EqualWithoutParentheses
    | EqualBorder
    | EqualBorderNotDistance
    | EqualOrderChanged
    | StartsWith
    | EndsWith
    | StartsWithNotDistance
    | EndsWithNotDistance
    | SameSubstring
    | SameSubstringNotDistance
    | SpecifiedMatch
    | IgnoredDbOpPoint
    | IgnoredWkOpPoint
    | EqualDistanceShortSubstring
    | Failed
    
/// result of match of operational points
type ResultOfOpPoint =
    | Success of DbOpPointOfRoute * WkOpPointOfRoute * MatchKind
    | Failure of DbOpPointOfRoute

/// view of ResultOfOpPoint.Success
type OpPointOfDbWk =
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

/// info of a route in a wikipedia article
type RouteInfo =
    { nummer: int
      title: string
      von: string
      bis: string
      railwayGuide: string option
      routenameKind: RoutenameKind
      searchstring: string }
