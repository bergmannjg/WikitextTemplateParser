# Wikitext Template Parser

Parse wikitext [Route diagram](https://de.wikipedia.org/wiki/Wikipedia:Formatvorlage_Bahnstrecke) [templates](https://www.mediawiki.org/wiki/Help:Templates) and compare with [DB register of infrastructure](https://geovdbn.deutschebahn.com/isr) and [Open-Data](https://data.deutschebahn.com/dataset/geo-betriebsstelle) of Deutsche Bahn.

* [parse](./src/WikitextRouteDiagrams/readme.md),
* [compare](./src/WikitextRouteDiagrams/readme.md#wikitext-db-comparer),
* [view results](./src/ResultsViewer/readme.md).

## Introduction

* a route is a route in a wikipedia article, an article may contain multiple routes,
* entities compared are operational points (Betriebsstellen) like stations and stops,
* reference is the [DB register of infrastructure](https://geovdbn.deutschebahn.com/isr), data from [RINF](https://rinf.era.europa.eu/RINF). 

## Comparison

The comparison of wiki data with available db data gives the follwing results:

| Count | Value | Remarks | Example |
|---|-----:|---|---|
|routes with all db data found in wikidata|595|all db operational points of a route found in wikidata|[Route 1700](https://de.wikipedia.org/wiki/Bahnstrecke_Hamm%E2%80%93Minden)|
|routes with some db data not found in wikidata|35|some db operational points of a route not found in wikidata|[Weinheim-Sulzbach, Route 3601](https://de.wikipedia.org/wiki/Main-Neckar-Eisenbahn)|

## Statistics about operational points not found

How many operational points are not found:

| Count | Value | Routes| 
|--|-----:|----:|
| operational points matched |6094|595|
| operational points missing |40|27|
| operational points with specified matching |15| 11|

## Statistics about articles

There are several reasons why it is not posssible to compare the data:

| Count | Value | Remarks | Example |
|---|-----:|---|---|
|articles total|1499|all articles from the SPARGL query with route templates are parsed||
|articles with empty route parameter|317||[Schluff Eisenbahn](https://de.wikipedia.org/wiki/Schluff_(Eisenbahn))|
|route is no passenger train|305|urban trains and freight trains are not checked|[Route 1734](https://de.wikipedia.org/wiki/Bahnstrecke_Hannover%E2%80%93Braunschweig)|
|start/stop operational points of route not found|0|operational points from route parameters should match with operational points <br/>from templates having distances, 100 entries specified manually|
|routes compared with db data|630|routes with available db data are comapred|[Route 1700](https://de.wikipedia.org/wiki/Bahnstrecke_Hamm%E2%80%93Minden)|
|routes shutdown|473|remark in railway guide (KBS) or operational points out of service|[Route 3745](https://de.wikipedia.org/wiki/Oberwaldbahn)|
|routes with no db data found, <br/>but article has railway guide (KBS)|169|articles with shut down routes|[Route 9560 missing](https://de.wikipedia.org/wiki/Bahnstrecke_Schaftlach%E2%80%93Tegernsee)|
|routes with no db data found|16|articles with shut down routes|[Route 6603 down](https://de.wikipedia.org/wiki/Bahnstrecke_Pirna%E2%80%93Gottleuba)|

## Extracting the route infos

Extracting the route infos (i.e. route number, start and stop operational point) in 'STRECKENNR' template gives the follwing results:

| Count | Value | Example |
|---|-----:|---|
| route number without operational point names|1014|4250|
| route number without operational point names and text ignored|124|6967; sä.MN|
| operational point names in &lt;small&gt; format tags|571|6135 &lt;small>(Bln. Südkreuz–Elsterwerda)&lt;/small>|
| operational point names in text|22|1101 Lütjenbrode–Heiligenhafen|
| route info not matched|0||

### Usage of shortnames

Analyzing the shortnames of operational points in the route diagrams gives the follwing results:

| Count | Value | Example |
|---|-----:|---|
|distinct operational points with links to articles |12753||
|articles with infobox Bahnhof and  [shortname](https://fahrweg.dbnetze.com/fahrweg-de/kunden/betrieb/betriebsstellen-1393360)|2143|[Wuppertal](https://de.wikipedia.org/wiki/Wuppertal_Hauptbahnhof) in route [Düsseldorf–Elberfeld](https://de.wikipedia.org/wiki/Bahnstrecke_D%C3%BCsseldorf%E2%80%93Elberfeld)|
|articles without infobox Bahnhof |10610|[Troisdorf](https://de.wikipedia.org/wiki/Troisdorf#Eisenbahnverkehr) in route [Rechte Rheinstrecke](https://de.wikipedia.org/wiki/Rechte_Rheinstrecke)|