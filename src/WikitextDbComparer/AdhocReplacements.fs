/// replacements in wiki data to pass the matchings 
module AdhocReplacements

open Types

let ignoreStringsInRoutename =
    [| ", 3. Gl."
       "3. Gleis"
       ", Ferngleise"
       ", Vorortgleise"
       ", P-Bahn"
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
       "Gütergl."
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
       ("Mainbahn", 3520, "Frankfurt Hbf", "Frankfurt (Main) Hbf")
       ("Mainbahn", 3650, "Frankfurt Stadion", "Frankfurt am Main Stadion")
       ("Mainbahn", 3650, "Frankfurt Süd", "Frankfurt (Main) Süd")
       ("Mainbahn", 3538, "Gustavsburg", "Mainz-Gustavsburg")
       ("Mainbahn", 3538, "Bischofsheim", "Mainz-Bischofsheim")
       ("Bahnstrecke_Haan-Gruiten–Köln-Deutz", 2660, "Köln-Deutz", "Köln Messe/Deutz")
       ("Rhein-Main-Bahn", 3520, "Mz-Bischofsh Pbf", "Mainz-Bischofsheim")
       ("Rhein-Main-Bahn", 3530, "Mz-Bischofsh Pbf", "Mainz-Bischofsheim")
       ("Siegstrecke", 2651, "Köln-Deutz", "Köln Messe/Deutz")
       ("Bahnstrecke_Mannheim–Saarbrücken", 3280, "Ludwigsh. (Rh) ÜbS", "Ludwigshafen (Rhein) Hbf")
       ("Frankenbahn", 4800, "Bietigheim-B.", "Bietigheim-Bissingen")
       ("Frankenbahn", 4900, "Bietigheim-B.", "Bietigheim-Bissingen")
       ("Bahnstrecke_Mannheim–Frankfurt_am_Main", 3628, "Ffm Stadion", "Frankfurt am Main Stadion")
       ("Bahnstrecke_Mannheim–Frankfurt_am_Main", 3658, "Ffm Stadion", "Frankfurt am Main Stadion")
       ("Bahnstrecke_Mannheim–Frankfurt_am_Main", 3658, "Ffm Stadion Süd", "Frankfurt am Main Stadion Süd")
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
       ("Bahnstrecke_Köln–Duisburg", 2400, "Köln-Deutz", "Köln Messe/Deutz")
       ("Bahnstrecke_Berlin_Frankfurter_Allee–Berlin-Rummelsburg", 6140, "Frankfurter Allee", "Berlin Frankfurter Allee")
       ("Bahnstrecke_Berlin_Frankfurter_Allee–Berlin-Rummelsburg", 6140, "Rummelsburg", "Berlin-Rummelsburg")
       ("Bahnstrecke_Wuppertal-Oberbarmen–Solingen", 2700, "W-Oberbarmen", "Wuppertal-Oberbarmen")
       ("Bahnstrecke_Wuppertal-Oberbarmen–Solingen", 2700, "RS-Lennep", "Remscheid-Lennep")
       ("Bahnstrecke_Wuppertal-Oberbarmen–Solingen", 2705, "RS-Lennep", "Remscheid-Lennep")
       ("Bahnstrecke_Wuppertal-Oberbarmen–Solingen", 2705, "RS", "Remscheid Hbf")
       ("Bahnstrecke_Wuppertal-Oberbarmen–Solingen", 2675, "RS", "Remscheid Hbf")
       ("Bahnstrecke_Wuppertal-Oberbarmen–Solingen", 2675, "SG", "Solingen Hbf")
       ("Bahnstrecke_Appenweier–Strasbourg", 4261, "App.-Muhrhaag", "Appenweier-Muhrhaag")
       ("Bahnstrecke_Appenweier–Strasbourg", 4261, "App. Kurve", "Appenweier Kurve")
       ("Außerfernbahn", 5403, "Grenze", "Pfronten-Steinach")
       ("Bahnstrecke_Mannheim–Frankfurt_am_Main", 3533, "Gr-Gerau-Dornbg", "Groß Gerau-Dornberg")
       ("Bahnstrecke_Osterath–Dortmund_Süd", 2505, "DU-Rheinhausen", "Rheinhausen")
       ("Bahnstrecke_Osterath–Dortmund_Süd", 2505, "BO Nord", "Bochum Nord")
       ("Bahnstrecke_Osterath–Dortmund_Süd", 2312, "DU-Hochfeld Süd", "Duisburg-Hochfeld Süd")
       ("Bahnstrecke_Osterath–Dortmund_Süd", 2312, "DU Hbf", "Duisburg Hbf")
       ("Bahnstrecke_Osterath–Dortmund_Süd", 2326, "DU Hbf", "Duisburg Hbf")
       ("Bahnstrecke_Osterath–Dortmund_Süd", 2326, "DU-Hochfeld Süd Vorbf", "Duisburg-Hochfeld Süd Vorbf")
       ("Ardeybahn", 2103, "Soest", "Dortmund-Hörde")
       ("Bahnstrecke_Dortmund–Enschede", 2014, "Gronau", "Gronau (Westf)")
       ("Bahnstrecke_Berlin–Wriezen", 6078, "B Wriezener Gbf", "Berlin Wriezener Bf")
       ("Bahnstrecke_Berlin–Wriezen", 6078, "Biesdf Kr West", "Biesdorfer Kreuz West")
       ("Bahnstrecke_Solingen–Wuppertal-Vohwinkel", 2675, "SG-Ohligs", "Solingen Hbf")
       ("Bahnstrecke_Solingen–Wuppertal-Vohwinkel", 2675, "SG Süd", "Solingen Süd")
       ("Bahnstrecke_Oberhausen-Osterfeld–Hamm", 2250, "OB-Osterfeld", "Oberhausen-Osterfeld")
       ("Bahnstrecke_Oberhausen-Osterfeld–Hamm", 2248, "E-Dellwig Ost", "Essen-Dellwig Ost")
       ("Bahnstrecke_Eberswalde–Frankfurt_(Oder)", 6156, "Frankfurt [Oder]", "Frankfurt (Oder)")
       ("Bahnstrecke_Essen–Gelsenkirchen", 2237, "GE-Rotthausen", "Gelsenkirchen-Rotthausen")
       ("Bahnstrecke_Essen–Gelsenkirchen", 2237, "Gelsenkirchen", "Gelsenkirchen Hbf")
       ("Main-Neckar-Eisenbahn", 4061, "N-Edingen/F-feld", "Neu-Edingen/Mhm-Friedrichsfeld")
       ("Main-Neckar-Eisenbahn", 4061, "M-Fr Südein/Ausf", "Mannheim-Friedrichsfeld Südeinf/Ausf")
       ("Frankenbahn", 4802, "S Hbf", "Stuttgart Hbf")
       ("Frankenbahn", 4802, "S Nord", "Bft Stuttgart Nord")
       ("Oberbergische_Bahn", 2670, "Köln Posthof", "Köln Messe/Deutz")
       ("Oberbergische_Bahn", 2692, "Frankfurter Str.", "Köln Frankfurter Straße")
       ("Oberbergische_Bahn", 2692, "Flughafen NO", "Köln Frankfurter Straße") // 'Köln Flughafen Nordost' missing
       ("Bahnstrecke_Düsseldorf–Solingen", 2413, "D Hbf", "Düsseldorf Hbf")
       ("Bahnstrecke_Düsseldorf–Solingen", 2413, "D-Eller", "Düsseldorf-Eller")
       ("Bahnstrecke_Hagen–Dieringhausen", 2810, "Oberhagen", "Hagen-Oberhagen")
       ("Bahnstrecke_Hagen–Dieringhausen", 2810, "GM-Dieringhausen", "Gummersbach-Dieringhausen")
       ("Bahnstrecke_Crailsheim–Königshofen", 4953, "ehem. Infrastrukturgrenze", "Bad Mergentheim")
       ("Bahnstrecke_Crailsheim–Königshofen", 4922, "ehem. Infrastrukturgrenze", "Edelfingen")
       ("Bahnstrecke_Elberfeld–Dortmund", 2701, "W-Oberbarmen", "Wuppertal-Oberbarmen")
       ("Bahnstrecke_Duisburg–Quakenbrück", 2280, "Osterfeld", "Oberhausen-Osterfeld Abzw")
        |]

/// errors in wikidata
let maybeWrongRouteNr =
    [| ("Bahnstrecke_Lübeck–Lüneburg", 1122, 1120) |]

/// errors in wikidata
let maybeWrongRouteStation =
    [| ("Ostfriesische_Küstenbahn", 1570, "Emden", "Norden") |]

/// errors in wikidata
let maybeWrongDistances =
    [| (1100, "Schwartau-Waldhalle", [| 5.6 |], [| 4.6 |])
       (6078, "Biesdorfer Kreuz West", [||], [| 6.1 |])  // fill empty distance
       (6067, "Biesdorfer Kreuzmit derOstbahn", [||], [| 0.8 |])  // fill empty distance
       (6126, "Berliner AußenringzumGrünauer Kreuz", [||], [| 40.7 |])  // fill empty distance
    |]

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
       ("Ruhr-Sieg-Strecke", 2880, WikidataNotFoundInDbData, NoDbDataFoundWithRailwayGuide)
       ("Güteraußenring", 6126, WikidataNotFoundInDbData, WikidataWithoutDistancesInDbData)  |]
