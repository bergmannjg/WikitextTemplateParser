# Wikitext Template Parser

Parse wikitext [Route diagram](https://de.wikipedia.org/wiki/Wikipedia:Formatvorlage_Bahnstrecke) [templates](https://www.mediawiki.org/wiki/Help:Templates) and compare with [data](https://data.deutschebahn.com/dataset/geo-betriebsstelle) from Open-Data-Portal of Deutsche Bahn.

* [parse](./src/WikitextTemplateParser/readme.md),
* [compare](./src/WikitextDbComparer/readme.md),
* [view results](./src/ResultsViewer/readme.md).

## Preliminary results

Currently the comparison of wiki data with db data gives the follwing results:

| Count | Value | Remarks | Example |
|---|-----:|---|---|
|articles total|1563|all articles from the SPARGL query with route templates are parsed||
|articles with empty route parameter|382||[Schluff Eisenbahn](https://de.wikipedia.org/wiki/Schluff_(Eisenbahn))|
|distinct routes total|1465|articles may contain multiple routes or routes are in multiple articles |[1700](https://de.wikipedia.org/wiki/Bahnstrecke_Hamm%E2%80%93Minden) [1700](https://de.wikipedia.org/wiki/Bahnstrecke_Hannover%E2%80%93Minden)|
|route is no passenger train|230|urban trains and freight trains are not checked|[Route 1734](https://de.wikipedia.org/wiki/Bahnstrecke_Hannover%E2%80%93Braunschweig)|
|start/stop stations of route not found|0|stations from route parameters should match with stations of templates||
|routes with wikidata complete|566|all db stations of a route found in wikidata stations|[Route 1700](https://de.wikipedia.org/wiki/Bahnstrecke_Hamm%E2%80%93Minden)|
|routes with db data not found in wikidata|146|some db stations of a route not found in wikidata stations|[Villingen Dietrich, Route 4250](https://de.wikipedia.org/wiki/Schwarzwaldbahn_(Baden))|
|routes shutdown|34|remark in railway guide (KBS)|[Route 3745](https://de.wikipedia.org/wiki/Oberwaldbahn)|
|routes with no db data found, <br/>but article has railway guide (KBS)|103|missing db data|[Route 9560 missing](https://de.wikipedia.org/wiki/Bahnstrecke_Schaftlach%E2%80%93Tegernsee)|
|routes with no db data found|505|articles with shut down routes or missing db data|[Route 6603 down](https://de.wikipedia.org/wiki/Bahnstrecke_Pirna%E2%80%93Gottleuba)|

<br/>
The extraction of the route info (i.e. route number, start and stop station) in 'STRECKENNR' template gives the follwing results:

| Count | Value | Example |
|---|-----:|---|
| route number withpout station names|1014|4250|
| route number withpout station names and text ignored|124|6967; sä.MN|
| station names in &lt;small&gt; format tags|571|6135 &lt;small>(Bln. Südkreuz–Elsterwerda)&lt;/small>|
| station names in text|22|1101 Lütjenbrode–Heiligenhafen|
| route info not matched|0||

<br/>
The matching of db station names with wiki station names gives the follwing results:

| Count | Value | Example |
|---|-----:|---|
|equal names|5722|Vaihingen (Enz) - Vaihingen (Enz)|
|equal names with some fixed parts removed (i.e. Hbf) |169|Stendal - Stendal Hbf|
|equal names with parentheses removed |38|Reichenbach (Oberlausitz) - Reichenbach (OL)|
|names starts with equal substring|847|Bamberg, W 112 - Bamberg|
|names ends with equal substring|121|Berlin Potsdamer Platz - Potsdamer Platz|
|Levenshtein distance <= 3|57|Sersheim, Streckenwechsel - Sersheim Streckenwechsel|
|substring with at least 5 chars|166|Illingen, Streckenw. 4842/4800 - Illingen Streckenwechsel 4842/4800|