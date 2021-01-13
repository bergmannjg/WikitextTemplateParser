# Wikitext Template Parser

Parse wikitext [templates](https://www.mediawiki.org/wiki/Help:Templates) of type

* [Bahnstrecke](https://de.wikipedia.org/wiki/Wikipedia:Formatvorlage_Bahnstrecke) - route diagrams in germany,
* [Bahnhof](https://de.wikipedia.org/wiki/Wikipedia:Formatvorlage_Bahnhof) - the operational points from the route diagrams.

The wikipedia articles using the Route diagram templates can be retrieved with

* the [wikidata query service](https://query.wikidata.org/) and
* the [CirrusSearch](https://www.mediawiki.org/wiki/Help:CirrusSearch) MediaWiki extension.

## Using wikidata query service

The wikipedia articles are collected from wikidata with the following SPARGL query:

```SPARQL
SELECT ?railway ?railwayLabel ?article WHERE {
  SERVICE wikibase:label { bd:serviceParam wikibase:language "[AUTO_LANGUAGE],de". }
  ?railway wdt:P31 wd:Q728937;
    wdt:P17 wd:Q183.
  ?article schema:about ?railway;
    schema:isPartOf <https://de.wikipedia.org/>.
}
```

* wdt:P31 wd:Q728937 'ist eine Eisenbahnstrecke'
* wdt:P17 wd:Q183 'Staat Deutschland'

## Using CirrusSearch

The wikipedia articles are collected from CirrusSearch with the following parameters

* hastemplate:BS-header
* deepcat:Bahnstrecke in Deutschland

in this [link](https://de.wikipedia.org/w/index.php?search=hastemplate%3ABS-header+deepcat%3A%22Bahnstrecke+in+Deutschland%22&title=Spezial%3ASuche&profile=advanced&fulltext=1&advancedSearch-current=&ns0=1&limit=2000).

## Download and parse

The text of a wikipedia article is downloaded with [Spezial:Exportieren](https://de.wikipedia.org/wiki/Spezial:Exportieren).

The {{BS-header}} and {{BS-table}} templates are parsed. The Lua-based {{Routemap}} template is not yet covered. It is not used in germany (see this [link](https://de.wikipedia.org/w/index.php?search=hastemplate%3ARoutemap&title=Spezial:Suche&profile=advanced&fulltext=1&advancedSearch-current=%7B%7D&ns0=1)).

The parser uses the parser combinator library [fparsec](https://github.com/stephan-tolksdorf/fparsec).

The following AST is generated

```SPARQL
type Link = string * string

and Composite =
    | String of string
    | Link of Link
    | Template of Template

and Parameter =
    | Empty
    | String of string * string
    | Composite of string * Composite list

and FunctionParameter =
    | Empty
    | String of string

and Template = string * FunctionParameter list * Parameter list

type Templates = Template list
```

# Wikitext Db Comparer

Compare wikitext [Route diagram](https://de.wikipedia.org/wiki/Wikipedia:Formatvorlage_Bahnstrecke) templates with [route diagram data](https://rinf.era.europa.eu/RINF) from Register of infrastructure of Deutsche Bahn.

This is done in several steps.

## Load Db Data

* [Section of Line](https://rinf.era.europa.eu/RINF): Section of Line of register of infrastructure,
* [Operational points](https://rinf.era.europa.eu/RINF): Operational points of register of infrastructure,
* [Geo-Streckennetz](https://data.deutschebahn.com/dataset/geo-strecke) Start/Stop of a route and usage (i.e. is passenger train) of a route.

The Geo-Streckennetz data is restored with this [script](../../scripts/restore.sh).
Data from the register of infrastructure should be downloaded from RINF website.

## Extract route info in 'STRECKENNR' Template

The route number and start/stop operational point of a route are given in the 'STRECKENNR' template parameter in multiple ways, for example:

```
1730 (Hannover–Lehrte)
6135 <small>(Bln. Südkreuz–Elsterwerda)</small>
1101 Lütjenbrode–Heiligenhafen
```

 Operational points from route parameters should match with operational points from templates having distances. The content of the template parameter is transformed in RouteInfo type (**matching step 1**, see function RouteInfo.findRouteInfoInTemplates).

## From Templates to StationsOfInfobox

All templates of an wiki info box with station data are extracted and transformed in StationOfInfobox type.

## From StationsOfInfobox to StationsOfRoute

The operational points of a route are filtered from the StationOfInfobox data with the RouteInfo and transformed in StationOfRoute type:

* find the operational points from RouteInfo in the list of StationOfInfobox (**matching step 2**, see function OpPointOfRoute.findOpPointsOfRoute)
* filter the operational points in the list of StationOfInfobox (see function StationsOfRoute.filterStations)

## Compare Wikitext operational points with DB operational points

Compare the wikitext data of a route with the corresponding db data, i.e. are the operational points in db data a subset of the operational points in wiki data.

There are 3 phases to check if a wikitext operational point matches with a db operational point (**matching step 3**, see function OpPointMatch.matchStationName)

1. check if names or shortnames ([DS100](https://fahrweg.dbnetze.com/fahrweg-de/kunden/betrieb/betriebsstellen-1393360)) are equal,
2. check if names have a significant common substring,
3. check if distances are equal and names have a less significant common substring.

Phase 1 gives 90% of successful matches.
