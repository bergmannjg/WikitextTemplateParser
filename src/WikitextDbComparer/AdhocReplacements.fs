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
       ("Frankfurt(M) Hbf", "Frankfurt (Main) Hbf")
       ("Frankfurt Hbf", "Frankfurt (Main) Hbf")
       ("Gens-Horrw", "Gensingen-Horrweiler")
       ("Hdbg Hbf", "Heidelberg Hbf")
       ("Köln-Deutz", "Köln Messe/Deutz")
       ("SG-Ohligs", "Solingen Hbf")
       ("SG Süd", "Solingen Süd") |]
