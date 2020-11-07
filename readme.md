# Wikitext Template Parser

Parse wikitext [Route diagram](https://de.wikipedia.org/wiki/Wikipedia:Formatvorlage_Bahnstrecke) [templates](https://www.mediawiki.org/wiki/Help:Templates) and compare with [data](https://data.deutschebahn.com/dataset/geo-betriebsstelle) from Open-Data-Portal of Deutsche Bahn.

* [parse](./src/WikitextTemplateParser/readme.md),
* [compare](./src/WikitextDbComparer/readme.md),
* [view results](./src/ResultsViewer/readme.md).

## Preliminary results

Currently the comparison of wiki data with db data gives the follwing results:

| Count | Value | Remarks | Example |
|---|-----:|---|---|
|articles total|1565|all articles from the SPARGL query with route templates are parsed||
|articles with empty route parameter|381||[Schluff Eisenbahn](https://de.wikipedia.org/wiki/Schluff_(Eisenbahn))|
|distinct routes total|1428|articles may contain multiple routes or routes are in multiple articles ||
|route is no passenger train|219|urban trains and freight trains are not checked|[Route 1734](https://de.wikipedia.org/wiki/Bahnstrecke_Hannover%E2%80%93Braunschweig)|
|start/stop stations of route not found|40|stations from route parameters not found, needs further investigation|[Route 5403](https://de.wikipedia.org/wiki/Au%C3%9Ferfernbahn)|
|routes with wikidata complete|527|all db stations of a route found in wikidata stations|[Route 1700](https://de.wikipedia.org/wiki/Bahnstrecke_Hamm%E2%80%93Minden)|
|routes with db data not found in wikidata|131|some db stations of a route not found in wikidata stations|[Villingen Dietrich, Route 4250](https://de.wikipedia.org/wiki/Schwarzwaldbahn_(Baden))|
|routes shutdown|34|remark in railway guide (KBS)|[Route 3745](https://de.wikipedia.org/wiki/Oberwaldbahn)|
|routes with no db data found, <br/>but article has railway guide (KBS)|98|missing db data|[Route 9560 missing](https://de.wikipedia.org/wiki/Bahnstrecke_Schaftlach%E2%80%93Tegernsee)|
|routes with no db data found|502|articles with shut down routes or missing db data|[Route 6603 down](https://de.wikipedia.org/wiki/Bahnstrecke_Pirna%E2%80%93Gottleuba)|

<br/>
The extraction of the route info (i.e. route number, start and stop station) in 'STRECKENNR' template gives the follwing results:

| Count | Value | Example |
|---|-----:|---|
| route number withpout station names|974|4250|
| station names in &lt;small&gt; format tags|558|6135 &lt;small>(Bln. Südkreuz–Elsterwerda)&lt;/small>|
| station names in parenthesis:|14|5520 (München–Buchloe)|
| station names in text|10|1101 Lütjenbrode–Heiligenhafen|
| route info not matched|8|2691 (2692/3/4)|
