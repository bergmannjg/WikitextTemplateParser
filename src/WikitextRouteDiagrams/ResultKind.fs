namespace WikitextRouteDiagrams

open System.Text.RegularExpressions

// fsharplint:disable RecordFieldNames

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

module ResultKind =

    let private guessRailwayGuideIsValid (value: string option) =
        match value with
        | Some v -> Regex("\d{3}").Match(v).Success
        | None -> false

    let private guessRouteIsShutdown (railwayGuide: string option) =
        match railwayGuide with
        | Some v ->
            v.StartsWith "ehem"
            || v.StartsWith "alt"
            || v.StartsWith "zuletzt"
            || v.StartsWith "ex"
            || Regex("\(\d{4}\)").Match(v).Success
        | None -> false

    let getResultKind
        countWikiStops
        countDbStops
        countDbStopsFound
        countDbStopsNotFound
        (railwayGuide: string option)
        (unmatched: bool)
        (countAciveStations: int)
        (countShutdownStations: int)
        =
        let dbStopsWithRoute = countDbStops > 0

        if countWikiStops = 0 && dbStopsWithRoute then
            StartStopOpPointsNotFound
        else if dbStopsWithRoute && unmatched then
            StartStopOpPointsNotFound
        else if countDbStopsFound > 0
                && dbStopsWithRoute
                && countDbStopsNotFound = 0 then
            WikidataFoundInDbData
        else if guessRouteIsShutdown railwayGuide then
            RouteIsShutdown
        else if countWikiStops > 0
                && dbStopsWithRoute
                && countDbStopsNotFound > 0 then
            WikidataNotFoundInDbData
        else if countWikiStops = 0 && dbStopsWithRoute then
            WikidataNotFoundInTemplates
        else if not dbStopsWithRoute then
            if countShutdownStations >= 2
               && countAciveStations <= 2 then
                RouteIsShutdown
            else if guessRailwayGuideIsValid railwayGuide then
                NoDbDataFoundWithRailwayGuide
            else
                NoDbDataFoundWithoutRailwayGuide
        else
            Undef
