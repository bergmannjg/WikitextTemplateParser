# Wikitext Template Parser

Parse wikitext [Route diagram](https://de.wikipedia.org/wiki/Wikipedia:Formatvorlage_Bahnstrecke) [templates](https://www.mediawiki.org/wiki/Help:Templates) and compare with [data](https://data.deutschebahn.com/dataset/geo-betriebsstelle) from Open-Data-Portal of Deutsche Bahn.

## Parser

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

The text of the wikipedia article ist downloaded with https://de.wikipedia.org/wiki/Spezial:Exportieren.

Usage example of parser:

```
dotnet src/WikitextTemplateParser/bin/Debug/netcoreapp3.1/WikitextTemplateParser.dll -parsetitle Bahnstrecke_Nürnberg–Feucht
```

## Comparer

The routes in wikitext data are compared with the routes in db data:

* are the stations in db data a subset of the stations in wiki data
* etc.

Usage example of comparer:

```
dotnet src/WikitextDbComparer/bin/Debug/netcoreapp3.1/WikitextDbComparer.dll -comparetitle Bahnstrecke_Nürnberg–Feucht
```