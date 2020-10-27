# Wikitext Template Parser

Parse wikitext [Route diagram](https://de.wikipedia.org/wiki/Wikipedia:Formatvorlage_Bahnstrecke) [templates](https://www.mediawiki.org/wiki/Help:Templates) and compare with [data](https://data.deutschebahn.com/dataset/geo-betriebsstelle) from Open-Data-Portal of Deutsche Bahn.

* [parse](./src/WikitextTemplateParser/readme.md),
* [compare](./src/WikitextDbComparer/readme.md).

## Preliminary results

Currently the comparison gives the follwing results:

| Count | Value | Remarks |
|---|---:|---|
|articles total|1568|all articles from the SPARGL query with route templates are parsed|
|articles with empty route parameter|364||
|distinct routes total|1434|articles may contain multiple routes or routes are in multiple articles |
|route is no passenger train|205|urban trains and freight trains are not checked|
|start/stop stations of route not found|53|stations from route parameters not found, needs further investigation|
|routes with wikidata complete|440|all db stations of a route found in wikidata stations|
|routes with db data not found in wikidata|132|some db stations of a route not found in wikidata stations|
|routes with no db data found|738|articles with shut down routes or missing db data|