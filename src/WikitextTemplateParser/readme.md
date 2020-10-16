# Wikitext Template Parser

Parse wikitext [Route diagram](https://de.wikipedia.org/wiki/Wikipedia:Formatvorlage_Bahnstrecke) [templates](https://www.mediawiki.org/wiki/Help:Templates)

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

and Template = string * Parameter list

type Templates = Template list
```

Usage example of parser:

```
dotnet run --project src/WikitextTemplateParser/WikitextTemplateParser.fsproj -parsetitle Bahnstrecke_Nürnberg–Feucht
```

