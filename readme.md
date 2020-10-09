# Wikitext Template Parser

Parse wikitext [Route diagram](https://de.wikipedia.org/wiki/Wikipedia:Formatvorlage_Bahnstrecke) [templates](https://www.mediawiki.org/wiki/Help:Templates) and compare it with [data](https://data.deutschebahn.com/dataset/geo-betriebsstelle) from Open-Data-Portal of Deutsche Bahn.

The wikipedia articles are collected from wikidata with the following SPARGL query:

```
SELECT ?railway ?railwayLabel ?routeNumber ?article WHERE {
  SERVICE wikibase:label { bd:serviceParam wikibase:language "[AUTO_LANGUAGE],de". }
  ?railway wdt:P31 wd:Q728937;
    wdt:P17 wd:Q183;
    wdt:P1671 ?routeNumber.
  ?article schema:about ?railway;
    schema:isPartOf <https://de.wikipedia.org/>.
}
```
* wdt:P31 wd:Q728937 'ist eine Eisenbahnstrecke'
* wdt:P17 wd:Q183 'Staat Deutschland'
* wdt:P1671 'Streckennummer'

The text of the wikipedia article ist downloaded with https://de.wikipedia.org/wiki/Spezial:Exportieren.

Usage example of parser:

```
dotnet src/WikitextTemplateParser/bin/Debug/netcoreapp3.1/WikitextTemplateParser.dll -parsetitle Bahnstrecke_Nürnberg–Feucht
```

Usage example of comparer:

```
dotnet src/WikitextDbComparer/bin/Debug/netcoreapp3.1/WikitextDbComparer.dll -comparetitle Bahnstrecke_Nürnberg–Feucht
```