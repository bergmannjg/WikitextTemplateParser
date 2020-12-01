# Wikitext Template Parser

Parse wikitext [Route diagram](https://de.wikipedia.org/wiki/Wikipedia:Formatvorlage_Bahnstrecke) [templates](https://www.mediawiki.org/wiki/Help:Templates) and compare with [data](https://data.deutschebahn.com/dataset/geo-betriebsstelle) from Open-Data-Portal of Deutsche Bahn.

* [parse](./src/WikitextTemplateParser/readme.md),
* [compare](./src/WikitextDbComparer/readme.md),
* [view results](./src/ResultsViewer/readme.md).

## Preliminary results

### Comparison

Currently the comparison of wiki data with db data gives the follwing results:

| Count | Value | Remarks | Example |
|---|-----:|---|---|
|articles total|1563|all articles from the SPARGL query with route templates are parsed||
|articles with empty route parameter|382||[Schluff Eisenbahn](https://de.wikipedia.org/wiki/Schluff_(Eisenbahn))|
|distinct routes total|1465|articles may contain multiple routes or routes are in multiple articles |[1700](https://de.wikipedia.org/wiki/Bahnstrecke_Hamm%E2%80%93Minden) [1700](https://de.wikipedia.org/wiki/Bahnstrecke_Hannover%E2%80%93Minden)|
|route is no passenger train|230|urban trains and freight trains are not checked|[Route 1734](https://de.wikipedia.org/wiki/Bahnstrecke_Hannover%E2%80%93Braunschweig)|
|start/stop stations of route not found|1|stations from route parameters should match with stations from templates having distances, cycles are not yet implemented|[Berliner Ringbahn](http://localhost:59283/stationOfInfobox/Berliner%20Ringbahn)|
|routes with wikidata complete|573|all db stations of a route found in wikidata stations|[Route 1700](https://de.wikipedia.org/wiki/Bahnstrecke_Hamm%E2%80%93Minden)|
|routes with db data not found in wikidata|147|some db stations of a route not found in wikidata stations (possibly incorrect db data)|[Villingen Dietrich, Route 4250](https://de.wikipedia.org/wiki/Schwarzwaldbahn_(Baden))|
|routes shutdown|437|remark in railway guide (KBS) or stations out of service|[Route 3745](https://de.wikipedia.org/wiki/Oberwaldbahn)|
|routes with no db data found, <br/>but article has railway guide (KBS)|173|missing db data|[Route 9560 missing](https://de.wikipedia.org/wiki/Bahnstrecke_Schaftlach%E2%80%93Tegernsee)|
|routes with no db data found|45|articles with shut down routes or missing db data|[Route 6603 down](https://de.wikipedia.org/wiki/Bahnstrecke_Pirna%E2%80%93Gottleuba)|

### Extracting the route infos

Extracting the route infos (i.e. route number, start and stop station) in 'STRECKENNR' template gives the follwing results:

| Count | Value | Example |
|---|-----:|---|
| route number without station names|1014|4250|
| route number without station names and text ignored|124|6967; sä.MN|
| station names in &lt;small&gt; format tags|571|6135 &lt;small>(Bln. Südkreuz–Elsterwerda)&lt;/small>|
| station names in text|22|1101 Lütjenbrode–Heiligenhafen|
| route info not matched|0||

### Matching station names

Matching the db station names with wiki station names gives the follwing results:

| Count | Value | Example |
|---|-----:|---|
|equal shortnames|1848|Hamm (Westf) Pbf - EHM|
|equal shortnames with incorrect distance|49|Overath [2657](https://de.wikipedia.org/wiki/Oberbergische_Bahn)|
|equal names|3970|Vaihingen (Enz) - Vaihingen (Enz)|
|equal names with some fixed parts removed (i.e. Hbf) |138|Schönwalde - Abzw Schönwalde|
|equal names with parentheses removed |27|Reichenbach (Oberlausitz) - Reichenbach (OL)|
|names starts with equal substring|847|Köln-Mülheim, W 233 - Köln-Mülheim|
|names ends with equal substring|121|Mosbach-Neckarelz - Neckarelz|
|Levenshtein distance <= 3|41|Sersheim, Streckenwechsel - Sersheim Streckenwechsel|
|substring with at least 5 chars|161|Illingen, Streckenw. 4842/4800 - Illingen Streckenwechsel 4842/4800|

### Usage of shortnames

Analyzing the articles of the wiki stations in the route diagrams gives the follwing results:

| Count | Value | Example |
|---|-----:|---|
|distinct stations with links to articles |12753||
|articles with infobox Bahnhof and station [shortname](https://fahrweg.dbnetze.com/fahrweg-de/kunden/betrieb/betriebsstellen-1393360)|2143|[Wuppertal](https://de.wikipedia.org/wiki/Wuppertal_Hauptbahnhof) in route [Düsseldorf–Elberfeld](https://de.wikipedia.org/wiki/Bahnstrecke_D%C3%BCsseldorf%E2%80%93Elberfeld)|
|articles without infobox Bahnhof |10610|[Troisdorf](https://de.wikipedia.org/wiki/Troisdorf#Eisenbahnverkehr) in route [Rechte Rheinstrecke](https://de.wikipedia.org/wiki/Rechte_Rheinstrecke)|