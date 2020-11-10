/// types
module Types

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

