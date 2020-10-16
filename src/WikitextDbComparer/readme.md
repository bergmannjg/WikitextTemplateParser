# Wikitext Db Comparer

Compare wikitext [Route diagram](https://de.wikipedia.org/wiki/Wikipedia:Formatvorlage_Bahnstrecke) templates with [data](https://data.deutschebahn.com/dataset/geo-betriebsstelle) from Open-Data-Portal of Deutsche Bahn.

This is done in three steps.

## From Templates to PrecodedStations

All templates with station and  are extracted and transformed in PrecodedStation type.

There are several codings of kilometre data when a wikitext handles multiple routes (DistanceCoding type):

* single value for every station, see [München Augsburg](https://de.wikipedia.org/wiki/Bahnstrecke_M%C3%BCnchen%E2%80%93Augsburg)
* separation of routes with the **Bskm** template, see [Bassum Herford](https://de.wikipedia.org/wiki/Bahnstrecke_Bassum%E2%80%93Herford)
* multiple values for some stations, see [Haan-Gruiten–Köln-Deutz](https://de.wikipedia.org/wiki/Bahnstrecke_Haan-Gruiten%E2%80%93K%C3%B6ln-Deutz)
* todo.

## From PrecodedStations to Stations of a route

The stations of a route are filtered from the PrecodedStation data.

## Compare Wikitext Stations with DB Stations

Compare the wikitext station data of a route with the corresponding db data:

* are the stations in db data a subset of the stations in wiki data
* etc.

### Usage

Usage example of comparer:

```
dotnet run --project src/WikitextDbComparer/WikitextDbComparer.fsproj -comparetitle Bahnstrecke_Nürnberg–Feucht
```

This give the follwoing output

```
{ route = 5970
  title = "Bahnstrecke_Nürnberg–Feucht"
  fromToName = [|"Nürnberg Hbf"; "Feucht"|]
  fromToKm = [|0.0; 12.5|]
  countWikiStops = 8
  countDbStops = 5
  countDbStopsNotFound = 0
  resultKind = WikidataFound }
```