# Wikitext Template Parser

Parse wikitext [Route diagram](https://de.wikipedia.org/wiki/Wikipedia:Formatvorlage_Bahnstrecke) [templates](https://www.mediawiki.org/wiki/Help:Templates) and compare with [data](https://data.deutschebahn.com/dataset/geo-betriebsstelle) from Open-Data-Portal of Deutsche Bahn.

* [parse](./src/WikitextTemplateParser/readme.md),
* [compare](./src/WikitextDbComparer/readme.md).

## Preliminary results

Currently the comparison of wiki data with db data gives the follwing results:

| Count | Value | Remarks |
|---|-----:|---|
|articles total|1565|all articles from the SPARGL query with route templates are parsed|
|articles with empty route parameter|381||
|distinct routes total|1428|articles may contain multiple routes or routes are in multiple articles |
|route is no passenger train|219|urban trains and freight trains are not checked|
|start/stop stations of route not found|36|stations from route parameters not found, needs further investigation|
|routes with wikidata complete|405|all db stations of a route found in wikidata stations|
|routes with db data not found in wikidata|209|some db stations of a route not found in wikidata stations|
|routes with no db data found|695|articles with shut down routes or missing db data|

<br/>
The extraction of the route info (i.e. route number, start and stop station) in 'STRECKENNR' template gives the follwing results:

| Count | Value | Example |
|---|-----:|---|
| route number withpout station names|974|4250|
| station names in &lt;small&gt; format tags|558|6135 &lt;small>(Bln. Südkreuz–Elsterwerda)&lt;/small>|
| station names in parenthesis:|14|5520 (München–Buchloe)|
| station names in text|10|1101 Lütjenbrode–Heiligenhafen|
| route info not matched|8|2691 (2692/3/4)|
