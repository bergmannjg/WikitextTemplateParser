# Wikitext Template Parser

Parse wikitext [Route diagram](https://de.wikipedia.org/wiki/Wikipedia:Formatvorlage_Bahnstrecke) [templates](https://www.mediawiki.org/wiki/Help:Templates) and compare with [data](https://data.deutschebahn.com/dataset/geo-betriebsstelle) from Open-Data-Portal of Deutsche Bahn.

* [parse](./src/WikitextTemplateParser/readme.md),
* [compare](./src/WikitextDbComparer/readme.md).

## Preliminary results

Currently the comparison gives the follwing results:

| Count | Value | Remarks |
|---|---|---|
|articles total|1185|all articles from the SPARGL query with route templates are parsed|
|routes total|1577|articles may contain multiple routes or routes are in multiple articles |
|routes with wikidata complete|520|all db stations of a route found in wikidata stations|
|routes with no wikidata found in templates|182|failures in comparer, needs further investigation|
|routes with db data not found in wikidata|147|some db stations of a route not found in wikidata stations or failures in comparer|
|routes with no db data found|728|articles with shut down routes or missing db data|