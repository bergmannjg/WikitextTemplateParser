/// replacements with no general rule
module AdhocReplacements

open Types

let ignoreStringsInRoutename =
    [| ", 3. Gl."
       "3. Gleis"
       ", Ferngleise"
       ", Vorortgleise"
       "+EVS"
       "sä.WB"
       "ABS"
       "äußeres Gleispaar"
       "aus Hbf"
       "Bestandsstrecke"
       "drittes Gleis"
       "drittes Gl."
       "ehem. Trasse"
       "Fernbahn"
       "Ferngleise"
       "Fern- und Gütergleise"
       "Gesamtstrecke"
       "Güterverkehr"
       "Güterstrecke"
       "NBS"
       "nach Hbf"
       "Neutrassierung"
       "Personenverkehr"
       "S-Bahn"
       "Schnellfahrstrecke"
       "Südgleis"
       "Verbindungskurve" |]

let replacementsInRouteStation =
    [| ("Bahnstrecke_Berlin–Dresden", 6135, "Bln. Südkreuz", "Berlin Südkreuz")
       ("Bahnstrecke_Berlin–Dresden", 6248, "Dr.-Friedrichst.", "Dresden-Friedrichstadt")
       ("Main-Neckar-Eisenbahn", 3601, "Frankfurt(M) Hbf", "Frankfurt (Main) Hbf")
       ("Main-Neckar-Eisenbahn", 3601, "Hdbg Hbf", "Heidelberg Hbf")
       ("Mainbahn", 3650, "Frankfurt Stadion", "Frankfurt am Main Stadion")
       ("Mainbahn", 3650, "Frankfurt Süd", "Frankfurt (Main) Süd")
       ("Bahnstrecke_Haan-Gruiten–Köln-Deutz", 2660, "Köln-Deutz", "Köln Messe/Deutz")
       ("Rhein-Main-Bahn", 3520, "Mz-Bischofsh Pbf", "Mainz-Bischofsheim")
       ("Rhein-Main-Bahn", 3530, "Mz-Bischofsh Pbf", "Mainz-Bischofsheim")
       ("Siegstrecke", 2651, "Köln-Deutz", "Köln Messe/Deutz")
       ("Bahnstrecke_Mannheim–Saarbrücken", 3280, "Ludwigsh. (Rh) ÜbS", "Ludwigshafen (Rhein) Hbf")
       ("Frankenbahn", 4800, "Bietigheim-B.", "Bietigheim-Bissingen")
       ("Frankenbahn", 4900, "Bietigheim-B.", "Bietigheim-Bissingen")
       ("Bahnstrecke_Mannheim–Frankfurt_am_Main", 3628, "Ffm Stadion", "Frankfurt am Main Stadion")
       ("Bahnstrecke_Mannheim–Frankfurt_am_Main", 3658, "Ffm Stadion", "Frankfurt am Main Stadion")
       ("Bahnstrecke_Mannheim–Frankfurt_am_Main", 4010, "Ffm Stadion", "Frankfurt am Main Stadion")
       ("Bahnstrecke_Solingen–Wuppertal-Vohwinkel", 2675, "SG-Ohligs", "Solingen Hbf")
       ("Bahnstrecke_Solingen–Wuppertal-Vohwinkel", 2734, "SG Süd", "Solingen Süd")
       ("Bahnstrecke_Worms–Bingen_Stadt", 3560, "Gens-Horrw", "Gensingen-Horrweiler")
       ("Bahnstrecke_Worms–Bingen_Stadt", 3512, "Gens-Horrw", "Gensingen-Horrweiler")
       ("Bahnstrecke_Worms–Bingen_Stadt", 3512, "Büdesh-Dromersh", "Büdesheim-Dromersheim")
       ("Bahnstrecke_Worms–Bingen_Stadt", 3569, "Büdesh-Dromersh", "Büdesheim-Dromersheim")
       ("Bahnstrecke_Worms–Bingen_Stadt", 3569, "Bingen(Rh) Stadt", "Bingen (Rhein) Stadt")
       ("Bahnstrecke_Wuppertal-Vohwinkel–Essen-Überruhr", 2400, "E-Kupferdreh", "Essen-Kupferdreh")
       ("Bahnstrecke_Wuppertal-Vohwinkel–Essen-Überruhr", 2400, "E-Überruhr", "Essen-Überruhr")
       ("Berliner_Außenring", 6067, "Karower Kreuz", "Abzw Berlin-Karow West")
       ("Berliner_Außenring", 6087, "Karower Kreuz", "Abzw Berlin-Karow West")
       ("Neckartalbahn", 4110, "HD Hbf (alt)", "Heidelberg-Altstadt")
       ("Westbahn_(Württemberg)", 4800, "Bietigheim-B.", "Bietigheim-Bissingen")
       ("Bahnstrecke_Köln–Duisburg", 2650, "Köln-Deutz", "Köln Messe/Deutz")
       ("Bahnstrecke_Köln–Duisburg", 2400, "Köln-Deutz", "Köln Messe/Deutz") |]

/// errors in wikidata
let maybeWrongRouteNr =
    [| ("Bahnstrecke_Lübeck–Lüneburg", 1122, 1120) |]

/// errors in wikidata
let maybeWrongRouteStation =
    [| ("Ostfriesische_Küstenbahn", 1570, "Emden", "Norden") |]

/// errors in wikidata
let maybeWrongDistances =
    [| (1100, "Schwartau-Waldhalle", [| 5.6 |], [| 4.6 |]) |]

/// changes of ResultKind by case analysis
let adhocResultKindChanges =
    [| ("Bahnstrecke_Ingolstadt–Treuchtlingen", 5851, WikidataNotFoundInDbData, WikidataWithoutDistancesInDbData)
       ("Bahnstrecke_Köln–Duisburg", 2400, WikidataNotFoundInDbData, WikidataWithoutDistancesInDbData)
       ("Bahnstrecke_Jerxheim–Börßum", 1940, WikidataNotFoundInDbData, RouteIsShutdown)
       ("Bahnstrecke_Helmstedt–Börßum", 1940, WikidataNotFoundInDbData, RouteIsShutdown)
       ("Kraichgaubahn", 4950, WikidataNotFoundInDbData, NoDbDataFoundWithRailwayGuide)
       ("Donnersbergbahn", 3523, WikidataNotFoundInDbData, NoDbDataFoundWithRailwayGuide)
       ("Oberbergische_Bahn", 2655, WikidataNotFoundInDbData, WikidataWithoutDistancesInDbData)
       ("Oberbergische_Bahn", 2810, WikidataNotFoundInDbData, WikidataWithoutDistancesInDbData)
       ("Bahnstrecke_München_Ost–München_Flughafen", 5560, WikidataNotFoundInDbData, WikidataWithoutDistancesInDbData)
       ("Ruhr-Sieg-Strecke", 2880, WikidataNotFoundInDbData, NoDbDataFoundWithRailwayGuide) |]
