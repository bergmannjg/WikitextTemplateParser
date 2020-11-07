/// replacements with no general rule
module AdhocReplacements

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

let abbreviationsInRoutename =
    [| ("Bietigheim-B.", "Bietigheim-Bissingen")
       ("Bingen(Rh) Stadt", "Bingen (Rhein) Stadt")
       ("Bln.", "Berlin")
       ("Büdesh-Dromersh", "Büdesheim-Dromersheim")
       ("Dr.-Friedrichst.", "Dresden-Friedrichstadt")
       ("E-Kupferdreh", "Essen-Kupferdreh")
       ("E-Überruhr", "Essen-Überruhr") 
       ("Frankfurt(M) Hbf", "Frankfurt (Main) Hbf")
       ("Frankfurt Hbf", "Frankfurt (Main) Hbf")
       ("Gens-Horrw", "Gensingen-Horrweiler")
       ("Hdbg Hbf", "Heidelberg Hbf")
       ("Köln-Deutz", "Köln Messe/Deutz")
       ("Ludwigsh. (Rh) ÜbS", "Ludwigshafen (Rhein) Hbf")
       ("SG-Ohligs", "Solingen Hbf")
       ("SG Süd", "Solingen Süd")
       |]

let maybeWrongRoutes =
    [| ("Bahnstrecke_Lübeck–Lüneburg", 1122, 1120) |]
