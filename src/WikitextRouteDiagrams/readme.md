# Wikitext Template Parser

Parse wikitext [templates](https://www.mediawiki.org/wiki/Help:Templates) of type

* [Route diagram](https://de.wikipedia.org/wiki/Wikipedia:Formatvorlage_Bahnstrecke) - route diagrams in germany,
* [Station](https://de.wikipedia.org/wiki/Wikipedia:Formatvorlage_Bahnhof) - the stations from the route diagrams.

The wikipedia articles using the Route diagram templates can be retrieved with

* the [wikidata query service](https://query.wikidata.org/) and
* the [CirrusSearch](https://www.mediawiki.org/wiki/Help:CirrusSearch) MediaWiki extension.

## Using wikidata query service

The wikipedia articles are collected from wikidata with the following SPARGL query:

```
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

The text of a wikipedia article is downloaded with https://de.wikipedia.org/wiki/Spezial:Exportieren.

The {{BS-header}} and {{BS-table}} templates are parsed. The Lua-based {{Routemap}} template is not yet covered. It is not used in germany (see this [link](https://de.wikipedia.org/w/index.php?search=hastemplate%3ARoutemap&title=Spezial:Suche&profile=advanced&fulltext=1&advancedSearch-current=%7B%7D&ns0=1)).

The parser uses the parser combinator library [fparsec](https://github.com/stephan-tolksdorf/fparsec).

The following AST is generated

```
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

Compare wikitext [Route diagram](https://de.wikipedia.org/wiki/Wikipedia:Formatvorlage_Bahnstrecke) templates with [route diagram data](https://data.deutschebahn.com/dataset?groups=datasets) from Open-Data-Portal of Deutsche Bahn.

This is done in several steps.

## Load Db Data

* [Geo-Betriebsstelle](https://data.deutschebahn.com/dataset/geo-betriebsstelle): Stops with positions of all routes,
* [Geo-Streckennetz](https://data.deutschebahn.com/dataset/geo-strecke) Start/Stop station and usage (i.e. is passenger train) of a route.

The data is restored with this [script](../../scripts/restore.sh).

## Extract route info in 'STRECKENNR' Template

The route number and start/stop stations of a route are given in the 'STRECKENNR' template parameter in multiple ways, for example:

```
1730 (Hannover–Lehrte)
6107 (Lehrte–Oebisfelde)
6185 (Oebisfelde–Berlin-Spandau)
```

The content of the template parameter is transformed in RouteInfo type (**matching step 1**, see function RouteInfo.findRouteInfoInTemplates). 

## From Templates to StationsOfInfobox

All templates of an wiki info box with station data are extracted and transformed in StationOfInfobox type.

## From StationsOfInfobox to StationsOfRoute

The stations of a route are filtered from the StationOfInfobox data with the RouteInfo and transformed in StationOfRoute type:

* find the stations from RouteInfo in the list of StationOfInfobox (**matching step 2**, see function StationsOfRoute.findRouteInfoStations)
* filter the stations in the list of StationOfInfobox (see function StationsOfRoute.filterStations)

## Compare Wikitext Stations with DB Stations

Compare the wikitext station data of a route with the corresponding db data, i.e. are the stations in db data a subset of the stations in wiki data.

A wikitext station matches with a db station (**matching step 3**, see function StationMatch.matchesWkStationWithDbStation)

* if the distance differences are small 
* and station shortnames ([DS100](https://fahrweg.dbnetze.com/fahrweg-de/kunden/betrieb/betriebsstellen-1393360)) are equal or the station names have a common substring.
