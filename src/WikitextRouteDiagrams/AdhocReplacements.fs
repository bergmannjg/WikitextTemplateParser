/// replacements in wiki data to pass the matchings
[<RequireQualifiedAccess>]
module AdhocReplacements

open Types
open System.Text.RegularExpressions

let regexRef = Regex(@"<ref[^>]*>.+?</ref>")
let regexRefSelfClosed = Regex(@"<ref[^/]*/>")
let regexComment = Regex(@"<!--.*?-->")
let regexSpanOPen = Regex(@"<span[^>]+>")
let regexRailroadSwitch = Regex(@",\s* W.*$")
let regexTrackInStation = Regex(@",\s* Gl.*$")
let regexChangeOfRoute1 = Regex(@",\s* Streckenwechsel.*$")
let regexChangeOfRoute2 = Regex(@"\s* Strw .*$")
let regexYear = Regex(@"ab 19\d{2}")
let regexYearDiff = Regex(@"19\d{2}–19\d{2}")

module Wikitext =
    /// maybe errors in wikitext
    let replacements =
        [| ("Berliner Nordbahn", "{{BS2||", "{{BS2|")
           ("Bahnstrecke Lübbenau–Kamenz", "{{BS|BHF|T=STR|", "{{BS2|BHF|T=STR|") |]

    /// errors in wikidata
    let maybeWrongDistances =
        [| (1100, "Schwartau-Waldhalle", [| 5.6 |], [| 4.6 |])
           (2670, "Köln Messe/Deutz", [| 0.0 |], [| 1.1 |])
           (6186, "Selchow West", [||], [| 29.9 |]) // fill empty distance
           (6078, "Biesdorfer Kreuz West", [||], [| 6.1 |]) // fill empty distance
           (6067, "Biesdorfer Kreuz mit der Ostbahn", [||], [| 0.8 |]) // fill empty distance
           (3012, "Koblenz Hbf", [||], [| 14.3 |]) // fill empty distance
           (6126, "Berliner Außenring zum Grünauer Kreuz", [||], [| 40.7 |]) |] // fill empty distance

module RouteInfo =
    let ignoreStringsInRoutename =
        [ ", 3. Gl."
          ", 3./4. Gleis"
          "3. Gleis"
          "/9"
          ", Ferngleise"
          ", Vorortgleise"
          ", P-Bahn"
          "+EVS"
          "sä.WB"
          "; sä.DEH"
          "; sä.DE"
          "sä.DE"
          "(parallel)"
          "parallel"
          "ABS"
          "SFS"
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
          "Außengl."
          "Neutrassierung"
          "Personenverkehr"
          "Umgehungsstrecke"
          "S-Bahn"
          "Schnellfahrstrecke"
          "Südgleis"
          "Verbindungskurve" ]

    let prefixesOfEmptyRouteNames =
        [| "; sä"
           ";sä"
           "(sä"
           "+"
           "–"
           ","
           "ex"
           "()"
           "/"
           "(ex"
           "PLK"
           "NBS"
           "ABS"
           "S-Bahn"
           "Güterstr"
           "Ostseite"
           "Güterumfahrung"
           "alte Trasse"
           "Vorortgleise"
           "Ortsgleise" |]

    /// replace matched strings in a specific route
    let replacementsInRouteStation =
        [| ("Bahnstrecke Berlin–Dresden", 6135, "Bln. Südkreuz", "Berlin Südkreuz")
           ("Bahnstrecke Berlin–Dresden", 6248, "Dr.-Friedrichst.", "Dresden-Friedrichstadt")
           ("Main-Neckar-Eisenbahn", 3601, "Frankfurt(M) Hbf", "Frankfurt (Main) Hbf")
           ("Main-Neckar-Eisenbahn", 3601, "Hdbg Hbf", "Heidelberg Hbf")
           ("Mainbahn", 3520, "Frankfurt Hbf", "Frankfurt (Main) Hbf")
           ("Mainbahn", 3650, "Frankfurt Stadion", "Frankfurt am Main Stadion")
           ("Mainbahn", 3650, "Frankfurt Süd", "Frankfurt (Main) Süd")
           ("Mainbahn", 3538, "Gustavsburg", "Mainz-Gustavsburg")
           ("Mainbahn", 3538, "Bischofsheim", "Mainz-Bischofsheim Pbf")
           ("Bahnstrecke Haan-Gruiten–Köln-Deutz", 2660, "Köln-Deutz", "Köln Messe/Deutz")
           ("Rhein-Main-Bahn", 3520, "Mz-Bischofsh Pbf", "Mainz-Bischofsheim Pbf")
           ("Rhein-Main-Bahn", 3530, "Mz-Bischofsh Pbf", "Mainz-Bischofsheim")
           ("Siegstrecke", 2651, "Köln-Deutz", "Köln Gummersbacher Str.")
           ("Siegstrecke", 2651, "Betzdorf", "Betzdorf (Sieg)")
           ("Bahnstrecke Mannheim–Saarbrücken", 3280, "Ludwigsh. (Rh) ÜbS", "Ludwigshafen (Rhein) Hbf")
           ("Frankenbahn", 4800, "Bietigheim-B.", "Bietigheim-Bissingen")
           ("Frankenbahn", 4900, "Bietigheim-B.", "Bietigheim-Bissingen")
           ("Bahnstrecke Mannheim–Frankfurt am Main", 3628, "Ffm Stadion", "Frankfurt am Main Stadion")
           ("Bahnstrecke Mannheim–Frankfurt am Main", 3658, "Ffm Stadion", "Frankfurt am Main Stadion")
           ("Bahnstrecke Mannheim–Frankfurt am Main", 3658, "Ffm Stadion Süd", "Zeppelinheim")
           ("Bahnstrecke Mannheim–Frankfurt am Main", 4010, "Ffm Stadion", "Frankfurt am Main Stadion")
           ("Bahnstrecke Solingen–Wuppertal-Vohwinkel", 2675, "SG-Ohligs", "Solingen Hbf")
           ("Bahnstrecke Solingen–Wuppertal-Vohwinkel", 2734, "SG Süd", "Solingen Süd")
           ("Bahnstrecke Worms–Bingen Stadt", 3560, "Gens-Horrw", "Gensingen-Horrweiler")
           ("Bahnstrecke Worms–Bingen Stadt", 3512, "Gens-Horrw", "Gensingen-Horrweiler")
           ("Bahnstrecke Worms–Bingen Stadt", 3512, "Büdesh-Dromersh", "Büdesheim-Dromersheim")
           ("Bahnstrecke Worms–Bingen Stadt", 3569, "Büdesh-Dromersh", "Büdesheim-Dromersheim")
           ("Bahnstrecke Worms–Bingen Stadt", 3569, "Bingen(Rh) Stadt", "Bingen (Rhein) Stadt")
           ("Bahnstrecke Wuppertal-Vohwinkel–Essen-Überruhr", 2400, "E-Kupferdreh", "Essen-Kupferdreh")
           ("Bahnstrecke Wuppertal-Vohwinkel–Essen-Überruhr", 2400, "E-Überruhr", "Essen-Überruhr")
           ("Berliner Außenring", 6067, "Karower Kreuz", "Abzw Berlin-Karow West")
           ("Berliner Außenring", 6087, "Karower Kreuz", "Abzw Berlin-Karow West")
           ("Neckartalbahn", 4110, "HD Hbf (alt)", "Heidelberg-Altstadt")
           ("Westbahn (Württemberg)", 4800, "Bietigheim-B.", "Bietigheim-Bissingen")
           ("Bahnstrecke Köln–Duisburg", 0, "Köln-Deutz", "Köln Messe/Deutz")
           ("Bahnstrecke Berlin Frankfurter Allee–Berlin-Rummelsburg",
            6140,
            "Frankfurter Allee",
            "Berlin Frankfurter Allee")
           ("Bahnstrecke Berlin Frankfurter Allee–Berlin-Rummelsburg", 6140, "Rummelsburg", "Berlin-Rummelsburg")
           ("Bahnstrecke Wuppertal-Oberbarmen–Solingen", 2700, "W-Oberbarmen", "Wuppertal-Oberbarmen")
           ("Bahnstrecke Wuppertal-Oberbarmen–Solingen", 2700, "RS-Lennep", "Remscheid-Lennep")
           ("Bahnstrecke Wuppertal-Oberbarmen–Solingen", 2705, "RS-Lennep", "Remscheid-Lennep")
           ("Bahnstrecke Wuppertal-Oberbarmen–Solingen", 2705, "RS", "Remscheid Hbf")
           ("Bahnstrecke Wuppertal-Oberbarmen–Solingen", 2675, "RS", "Remscheid Hbf")
           ("Bahnstrecke Wuppertal-Oberbarmen–Solingen", 2675, "SG", "Solingen Hbf")
           ("Bahnstrecke Appenweier–Strasbourg", 4261, "App.-Muhrhaag", "Appenweier-Muhrhaag")
           ("Bahnstrecke Appenweier–Strasbourg", 4261, "App. Kurve", "Appenweier Kurve")
           ("Außerfernbahn", 5403, "Grenze", "Pfronten-Steinach")
           ("Bahnstrecke Mannheim–Frankfurt am Main", 3533, "Gr-Gerau-Dornbg", "Groß Gerau-Dornberg")
           ("Bahnstrecke Osterath–Dortmund Süd", 2505, "DU-Rheinhausen", "Rheinhausen")
           ("Bahnstrecke Osterath–Dortmund Süd", 2505, "BO Nord", "Bochum Nord")
           ("Bahnstrecke Osterath–Dortmund Süd", 2312, "DU-Hochfeld Süd", "Duisburg-Hochfeld Süd")
           ("Bahnstrecke Osterath–Dortmund Süd", 2312, "DU Hbf", "Duisburg Hbf")
           ("Bahnstrecke Osterath–Dortmund Süd", 2326, "DU Hbf", "Duisburg Hbf")
           ("Bahnstrecke Osterath–Dortmund Süd", 2326, "DU-Hochfeld Süd Vorbf", "Duisburg-Hochfeld Süd Vorbf")
           ("Ardeybahn", 2103, "Soest", "Dortmund-Hörde")
           ("Bahnstrecke Dortmund–Enschede", 2014, "Gronau", "Gronau (Westf)")
           ("Bahnstrecke Berlin–Wriezen", 6078, "B Wriezener Gbf", "Berlin Wriezener Bf")
           ("Bahnstrecke Berlin–Wriezen", 6078, "Biesdf Kr West", "Biesdorfer Kreuz West")
           ("Bahnstrecke Solingen–Wuppertal-Vohwinkel", 2675, "SG-Ohligs", "Solingen Hbf")
           ("Bahnstrecke Solingen–Wuppertal-Vohwinkel", 2675, "SG Süd", "Solingen Süd")
           ("Bahnstrecke Oberhausen-Osterfeld–Hamm", 2250, "OB-Osterfeld", "Oberhausen-Osterfeld")
           ("Bahnstrecke Oberhausen-Osterfeld–Hamm", 2248, "E-Dellwig Ost", "Essen-Dellwig Ost")
           ("Bahnstrecke Eberswalde–Frankfurt (Oder)", 6156, "Frankfurt [Oder]", "Frankfurt (Oder)")
           ("Bahnstrecke Essen–Gelsenkirchen", 2237, "GE-Rotthausen", "Gelsenkirchen-Rotthausen")
           ("Bahnstrecke Essen–Gelsenkirchen", 2237, "Gelsenkirchen", "Gelsenkirchen Hbf")
           ("Main-Neckar-Eisenbahn", 4061, "N-Edingen/F-feld", "Neu-Edingen/Mhm-Friedrichsfeld")
           ("Main-Neckar-Eisenbahn", 4061, "M-Fr Südein/Ausf", "Mannheim-Friedrichsfeld Südeinf/Ausf")
           ("Frankenbahn", 4802, "S Hbf", "Stuttgart Hbf")
           ("Frankenbahn", 4802, "S Nord", "Bft Stuttgart Nord")
           ("Oberbergische Bahn", 2670, "Köln Posthof", "Köln Messe/Deutz")
           ("Oberbergische Bahn", 2692, "Frankfurter Str.", "Köln Frankfurter Straße")
           ("Oberbergische Bahn", 2692, "Flughafen NO", "Köln Frankfurter Straße") // 'Köln Flughafen Nordost' missing
           ("Bahnstrecke Düsseldorf–Solingen", 2413, "D Hbf", "Düsseldorf Hbf")
           ("Bahnstrecke Düsseldorf–Solingen", 2413, "D-Eller", "Düsseldorf-Eller")
           ("Bahnstrecke Hagen–Dieringhausen", 2810, "Oberhagen", "Hagen-Oberhagen")
           ("Bahnstrecke Hagen–Dieringhausen", 2810, "GM-Dieringhausen", "Gummersbach-Dieringhausen")
           ("Bahnstrecke Crailsheim–Königshofen", 4953, "ehem. Infrastrukturgrenze", "Bad Mergentheim")
           ("Bahnstrecke Crailsheim–Königshofen", 4922, "ehem. Infrastrukturgrenze", "Edelfingen")
           ("Bahnstrecke Elberfeld–Dortmund", 2701, "W-Oberbarmen", "Wuppertal-Oberbarmen")
           ("Bahnstrecke Duisburg–Quakenbrück", 2280, "Osterfeld", "Oberhausen-Osterfeld Abzw")
           ("Bahnstrecke Stuttgart–Tuttlingen", 4600, "Plochingen", "Horb")
           ("Bahnstrecke Stuttgart–Tuttlingen", 4600, "Tuttlingen", "Immendingen")
           ("Außerfernbahn", 5452, "Grenze", "Griesen (Oberbay)")
           ("Rhein-Main-Bahn", 3557, "Aschaffenbg Hbf", "Aschaffenburg Hbf")
           ("Bahnstrecke Mannheim–Saarbrücken", 3401, "Böhl-Iggelh Abzw", "Böhl-Iggelheim")
           ("Bahnstrecke Seckach–Miltenberg", 4124, "Landesgrenze", "Rippberg")
           ("Bahnstrecke Seckach–Miltenberg", 5223, "Landesgrenze", "Schneeberg im Odenwald")
           ("Bahnstrecke Mülheim-Heißen–Oberhausen-Osterfeld Nord", 2280, "Frintrop", "Essen-Frintrop")
           ("Bahnstrecke Meckesheim–Bad Friedrichshall", 4114, "Bad Fr’hall", "Bad Friedrichshall Hbf")
           ("Bahnstrecke Nürnberg–Crailsheim", 5902, "Landesgrenze", "Schnelldorf")
           ("Bahnstrecke Nürnberg–Crailsheim", 4951, "Landesgrenze", "Schnelldorf")
           ("Oberbergische Bahn", 2655, "Frankfurter Str", "Köln Frankfurter Straße")
           ("Bahnstrecke Kühnhausen–Bad Langensalza", 6714, "Döllstedt", "Döllstädt")
           ("Bahnstrecke Düsseldorf–Solingen", 2676, "D-Eller", "Düsseldorf-Eller")
           ("Bahnstrecke Gemünden–Ebenhausen", 5233, "Ebenhausen (Unterfranken)", "Ebenhausen (Unterfr)")
           ("Bahnstrecke Halle–Vienenburg", 6346, "Halle Gbf", "Halle (Saale) Gbf")
           ("Bahnstrecke Halle–Vienenburg", 6346, "Halle Thüringer Bf", "Halle (Saale) Hbf")
           ("Bahnstrecke Kassel–Warburg", 3913, "Kassel-Oberzwehren", "Güterstrecke nach Kassel-Wilhelmshöhe")
           ("Neckartalbahn", 4100, "HD-Altstadt", "Heidelberg-Altstadt")
           ("Bahnstrecke Berlin–Wriezen", 6072, "B-Lichtenberg", "Berlin-Lichtenberg")
           ("Bahnstrecke Gemünden–Ebenhausen", 5233, "Ebenhausen Unterfranken", "Ebenhausen (Unterfr)")
           ("Bahnstrecke Leipzig–Eilenburg", 6371, "Le Eilb Bf", "Leipzig Eilenburger Bf")
           ("Ostfriesische Küstenbahn", 1570, "Emden", "Norden")
           ("Bahnstrecke Oberhausen-Osterfeld–Hamm", 2248, "Bottrop", "Bottrop Hbf")
           ("Bahnstrecke Leipzig–Eilenburg", 6371, "Abzw Heiterblick", "Abzw Leipzig-Heiterblick") |]

    /// errors in wikidata
    let maybeWrongRouteNr =
        [| ("Bahnstrecke Lübeck–Lüneburg", 1122, 1120) |]

    /// replace strings in a specific route (ignore matching)
    let maybeReplaceRouteStation: (string * int * string option * string option) [] =
        [| ("Bahnstrecke Lübeck–Lübeck-Travemünde Strand", 1100, Some "Lübeck Hbf", Some "Schwartau Waldhalle")
           ("Bahnstrecke Düsseldorf–Elberfeld", 2550, Some "Wuppertal Hbf", Some "Düsseldorf Hbf")
           ("Bahnstrecke Neuwied–Koblenz", 3014, None, Some "Koblenz-Lützel")
           ("Bahnstrecke Mühldorf–Burghausen", 5723, Some "Mühldorf (Oberbay)", Some "Tüßling")
           ("Bahnstrecke Cottbus–Frankfurt (Oder)",
            6253,
            Some "Grunow (Niederlausitz)",
            Some "Frankfurt (Oder)-Neuberesinchen")
           ("Bahnstrecke Hagenow Land–Schwerin", 6442, Some "Hagenow", Some "Holthusen")
           ("Bahnstrecke Hagenow Land–Schwerin", 6441, Some "Holthusen", Some "Schwerin")
           ("Bahnstrecke Cottbus–Guben", 6345, Some "Peitz Ost", Some "Guben Süd")
           ("Bahnstrecke Gotteszell–Blaibach", 5811, Some "Blaibach", Some "Blaibach") |]

module OpPointMatch =
    let ignoreStringsInStationname =
        [ "Hbf"
          "Pbf"
          "Vorbahnhof"
          "Awanst"
          "Abzweig"
          "Anschluss"
          ", ehem. Bf"
          "Str."
          " Hp"
          "Abzw."
          "Abzw" ]

/// changes of ResultKind by case analysis
let adhocResultKindChanges =
    [| ("Bahnstrecke Ingolstadt–Treuchtlingen", 5851, WikidataNotFoundInDbData, WikidataWithoutDistancesInDbData)
       ("Bahnstrecke Köln–Duisburg", 2400, WikidataNotFoundInDbData, WikidataWithoutDistancesInDbData)
       ("Bahnstrecke München Ost–München Flughafen", 5560, WikidataNotFoundInDbData, WikidataWithoutDistancesInDbData)
       ("Kraichgaubahn", 4950, StartStopOpPointsNotFound, NoDbDataFoundWithRailwayGuide)
       ("Donnersbergbahn", 3523, WikidataNotFoundInDbData, NoDbDataFoundWithRailwayGuide)
       ("Bahnstrecke Jerxheim–Börßum", 1940, WikidataNotFoundInDbData, RouteIsShutdown)
       ("Bahnstrecke Helmstedt–Börßum", 1940, WikidataNotFoundInDbData, RouteIsShutdown)
       ("Bahnstrecke Bremervörde–Walsrode", 1711, WikidataNotFoundInDbData, RouteIsShutdown)
       ("Bahnstrecke Gotteszell–Blaibach", 9581, NoDbDataFoundWithRailwayGuide, DbDataMissing)
       ("VnK-Strecke", 6070, WikidataNotFoundInDbData, RouteIsShutdown)
       ("Bahnstrecke Wuppertal-Oberbarmen–Wuppertal-Wichlinghausen", 2701, WikidataNotFoundInDbData, RouteIsShutdown)
       ("Bahnstrecke Duisburg–Quakenbrück", 2280, WikidataNotFoundInDbData, Undef)
       ("Bahnstrecke Osterath–Dortmund Süd", 2326, WikidataNotFoundInDbData, Undef)
       ("Bahnstrecke Neuwied–Koblenz", 3012, WikidataNotFoundInDbData, Undef) |]

module Comparer =
    /// fixed matchings of db and wiki operational points, maybe error in wikidata
    let matchingsOfDbWkOpPoints =
        [| ("Bahnstrecke Lübeck–Puttgarden", 1100, "Burg (Fehmarn) West", "Bbf Burg-West")
           ("Bahnstrecke Dortmund–Soest", 2103, "Dortmund Dfd", "Strecke von Dortmund-Dortmunderfeld")
           ("Bahnstrecke Wanne-Eickel–Hamburg", 2200, "Hörne", "Osnabrück-Hörne Bbf")
           ("Bahnstrecke Wanne-Eickel–Hamburg", 2200, "Bremen Utbremen", "Strecke von/nach Bremerhaven")
           ("Bahnstrecke Hagen–Hamm", 2550, "Schwerte (Ruhr) Ost", "Schwerte Heide")
           ("Bahnstrecke Düsseldorf–Elberfeld", 2550, "Wuppertal Linden", "Linden")
           ("Bahnstrecke Elberfeld–Dortmund", 2801, "Dortmund Dfd", "Strecke nach Soest")
           ("Bahnstrecke Göttingen–Bebra", 3600, "Eschwege-Stegmühle", "Eschwege West Stegmühle")
           ("Bahnstrecke Göttingen–Bebra", 3600, "Eschwege-Wehre", "Eschwege West Wehre")
           ("Bahnstrecke Stuttgart–Tuttlingen", 4600, "Rietheim-Weilheim", "Weilheim (Württ)")
           ("Bahnstrecke Plochingen–Tübingen", 4600, "Oberboihingen Abzw", "Wendlinger Kurve")
           ("Bahnstrecke Berlin–Blankenheim", 6118, "Potsdam Griebnitzsee", "Griebnitzsee Ost")
           ("Bahnstrecke Zittau–Löbau", 6214, "Mittelherwigsdorf (Sachs)", "Hp Abzw Mittelherwigsdorf")
           ("Bahnstrecke Magdeburg–Leipzig", 6403, "Halle-Kanena", "Kanena, Abzw Leuchtturm")
           ("Bahnstrecke Rostock–Rostock Seehafen Nord",
            6443,
            "Rostock Seehafen Bw",
            "Rostock Seehafen Bahnbetriebswerk") |]

    /// maybe error in wiki data, checked with DB Netze Infrastrukturregister (https://geovdbn.deutschebahn.com/isr)
    let nonexistentWkOpPoints =
        [| ("Bahnstrecke Hannover–Altenbeken", 1760, "Hannover-Waldhausen")
           ("Bahnstrecke Kreuztal–Cölbe", 2870, "Oberndorf (Kr Wittgenstein)")
           ("Bahnstrecke Altenbeken–Kreiensen", 2974, "Brakel")
           ("Eifelquerbahn", 3005, "Mayen Mitte")
           ("Moselstrecke", 3010, "Trier Mäusheckerweg              ")
           ("Nahetalbahn", 3511, "Martinstein West")
           ("Nahetalbahn", 3511, "Martinstein Ost")
           ("Bahnstrecke Göttingen–Bebra", 3600, "Grone")
           ("Main-Neckar-Eisenbahn", 3601, "Weinheim-Sulzbach")
           ("Taunus-Eisenbahn", 3603, "Wiesbaden Ost Gbf (B)")
           ("Lahntalbahn", 3710, "Wetzlar Propan Rheingas")
           ("Schiefe Ebene (Eisenbahnstrecke)", 5100, "Neuenmarkt-Wirsberg Ost")
           ("Allgäubahn (Bayern)", 5362, "Biessenhofen Abzweig")
           ("Bahnstrecke Nürnberg–Bamberg", 5900, "Fürth (Bay) Gbf")
           ("Güteraußenring", 6067, "Berlin-Hohenschönhausen")
           ("Güteraußenring", 6080, "Eichgestell Nord")
           ("Berliner Außenring", 6087, "Hennigsdorf Nord (Hdo)")
           ("Berliner Außenring", 6087, "Hennigsdorf Nord (Hdw)")
           ("Bahnstrecke Berlin–Magdeburg", 6110, "Potsdam Wildpark Ost")
           ("Bahnstrecke Berlin–Blankenheim", 6118, "Seddin Bla")
           ("Güteraußenring", 6126, "Grünauer Kreuz Süd")
           ("Güteraußenring", 6126, "Grünauer Kreuz Nord")
           ("Bahnstrecke Michendorf–Großbeeren", 6127, "Genshagener Heide Mitte")
           ("Bahnstrecke Berlin–Halle", 6132, "Großbeeren Süd")
           ("Bahnstrecke Berlin–Halle", 6132, "Roitzsch Abzw")
           ("Bahnstrecke Berlin–Halle", 6132, "Halle (Saale) Abzw Ab")
           ("Bahnstrecke Berlin–Halle", 6132, "Halle (Saale) Betriebsbahnhof Hnw")
           ("Bahnstrecke Berlin–Halle", 6132, "Halle (Saale) Gbf")
           ("Bahnstrecke Halle–Bebra", 6340, "Halle Thüringer Bahn")
           ("Bahnstrecke Halle–Bebra", 6340, "Gotha (16W03)")
           ("Bahnstrecke Halle–Cottbus", 6345, "Halle (Saale) Gbf")
           ("Bahnstrecke Halle–Cottbus", 6345, "Halle (Saale) Betriebsbahnhof As")
           ("Bahnstrecke Halle–Cottbus", 6345, "Falkenberg (Elster) ob Bf Stw W 2")
           ("Bahnstrecke Halle–Cottbus", 6345, "Cottbus Cfw")
           ("Bahnstrecke Leipzig–Hof", 6362, "Werdau Bogendreieck Werdauer Spitze")
           ("Bahnstrecke Magdeburg–Leipzig", 6403, "Magdeburg-Fermersleben")
           ("Bahnstrecke Magdeburg–Leipzig", 6403, "Schönebeck (Elbe) Gbf")
           ("Bahnstrecke Magdeburg–Leipzig", 6403, "Halle (Saale) Leuchtturm")
           ("Bahnstrecke Köthen–Aschersleben", 6420, "Giersleben Abzw")
           ("Bahnstrecke Velgast–Prerow", 6778, "Velgast DB-Grenze") |]
