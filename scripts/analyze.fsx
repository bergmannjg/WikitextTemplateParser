#r "nuget: FSharp.Data"
#r "nuget: FSharp.SystemTextJson"
#r "nuget: LiteDB"
#r "../src/WikitextRouteDiagrams/bin/Debug/net5.0/WikitextRouteDiagrams.dll"

open ResultsOfMatch
open RouteInfo

Serializer.addConverters ([| |])

showComparisonResults()
showMatchKindStatistics()
showRouteInfoResults()