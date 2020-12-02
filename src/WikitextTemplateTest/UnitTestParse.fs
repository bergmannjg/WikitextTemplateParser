// export VSTEST_HOST_DEBUG=1
module TestParse

open NUnit.Framework
open FParsec
open Templates
open System.Text.RegularExpressions

let daten0 = """
{{BS-header|Rheine–Norddeich Mole}}
{{BS-daten
| DE-STRECKENNR = 2931 <small>(Rheine–Emden Süd)</small><br /><!---->1570 <small>(Emden Süd–Norden)</small><br /><!---->1572 <small>(Emden Hbf–Außenhafen)</small><br /><!---->1574 <small>(Norden–Norddeich Mole)</small>
| DE-KBS = 395
| LÄNGE = 176
| SPURWEITE = 1435
| V-MAX = 160
| STRECKENKLASSE = D4
| STROMW = 15 kV 16,7 Hz
| ZWEIGLEISIG = Rheine–Dörpen<br /><!---->Dörpen-Lehe–Leer Süd<br /><!---->Leer Gbf–Emden Rbf Strw
| BILDPFAD_KARTE = Karte Emslandbahn.png
| PIXEL_KARTE = 270px
| TEXT_KARTE =
}}
{{BS-table}}
{{BS|STR|||[[Bahnstrecke Rheine–Norddeich Mole|Strecke von Emden]]}}
{{BS|ABZg+r|||[[Bahnstrecke Almelo–Salzbergen|Strecke von Almelo (NL)]]}}
{{BS|BHF||[[Salzbergen#Verkehr|Salzbergen]]}}
{{BS3|STR+l|KRZu||||[[Bahnstrecke Duisburg–Quakenbrück|Strecke von Spelle]]}}
{{BS3e|eABZg+r|STR||||ehem. [[Bahnstrecke Ochtrup–Rheine|Strecke von Ochtrup]]}}
{{BS3e|ABZg+l|ABZgr||||}}
{{BS3|BHF-L|BHF-R||179,9|'''[[Bahnhof Rheine|Rheine]]'''|{{Coordinate|text=ICON0|NS=52.276299|EW=7.434255|type=landmark|region=DE-NI|name=Rheine Bf}}}}
{{BS3|eABZg+l|eABZg+r||||[[Bahnbetriebswerk Rheine#Die Anlagen heute|ehem. Verbindungskurve]]}}
{{BS3e|eDST|STR|||[[Rangierbahnhof|Rheine Rbf]]|}}
{{BS3e|eABZgr|STR||||ehem. [[Bahnstrecke Duisburg–Quakenbrück|Strecke nach Dorsten]]}}
{{BS3|STRr|STR||||[[Bahnstrecke Münster–Rheine|Strecke nach Münster]]}}
{{BS|hKRZWae|178,1|[[Überleitstelle|Üst]] [[Ems]]brücke}}
{{BSe|BST|177,9|Kümpers|([[Gleisanschluss#Ausweichanschlussstelle|Awanst]])}}
{{BSe|BHF|173,0|[[Rodde (Rheine)|Rodde]]|(PV bis Juni 1991)}}
{{BS|hKRZWae|||[[Dortmund-Ems-Kanal]]}}
{{BS|SBRÜCKE|||[[Bundesautobahn 30|A 30]]}}
{{BS|HST|168,2|[[Hörstel#Schienenverkehr|Hörstel]]}}
{{BS|hKRZWae|||[[Mittellandkanal]]}}
{{BS3||ABZg+l|KDSTeq||Hafen [[Uffeln (Ibbenbüren)|Uffeln]]}}
{{BS|BHF|163,8|[[Bahnhof Ibbenbüren-Esch|Ibbenbüren-Esch]]|{{Coordinate|text=ICON0|NS=52.299746|EW=7.655968|type=landmark|region=DE-NW|name=Ibbenbüren-Esch Bf}}}}
{{BS3||ABZgl|KDSTeq||[[Bergwerk Ibbenbüren]]}}
{{BS|BHF|158,6|[[Bahnhof Ibbenbüren|Ibbenbüren]]}}
{{BS|ABZgr|||[[Bahnstrecke Ibbenbüren–Hövelhof|Strecke nach Gütersloh]]}}
{{BS|HST|154,1|[[Bahnhof Ibbenbüren-Laggenbeck|Ibbenbüren-Laggenbeck]]}}
{{BSe|ABZg+r|||ehem. [[Perm-Bahn|Perm-Bahn von Hasbergen]]}}
{{BS|DST|148,1|[[Hambüren#Siedlung Velpe|Velpe (Westf)]]}}
{{BS|DST|142,9|[[Lotte (Westfalen)|Lotte (Kr Tecklenburg)]] {{Coordinate|text=ICON0|NS=52.313464|EW=7.946012|type=landmark|region=DE-NW|name=Lotte Bf}}}}
{{BS|STR+GRZq|142,8||Landesgrenze [[Nordrhein-Westfalen|NRW]] / [[Niedersachsen|Nds]]}}
{{BS|hKRZWae|||[[Düte]]}}
{{BS|SBRÜCKE|||[[Bundesautobahn 1|A 1]], [[Dütebrücke]]}}
{{BS3||STR|STR+l|||[[Tecklenburger Nordbahn|Tecklenburger Nordbahn von Rheine]]}}
{{BS3||ABZg+l|ABZqr|||[[Bahnstrecke Oldenburg–Osnabrück|Strecke von Oldenburg]]}}
{{BS|DST|137,4|[[Bahnhof Osnabrück-Eversburg|Osnabrück-Eversburg]]|([[Keilbahnhof]])}}
{{BS|hKRZWae|||[[Hase (Fluss)|Hase]]}}
{{BS|ABZg+l|||[[Stadtwerke Osnabrück#Hafen|vom Hafen]]}}
{{BS|BRÜCKE1|||[[Bundesstraße 68|B 68]]}}
{{BS|HST|133,7|[[Haltepunkt Osnabrück Altstadt|Osnabrück Altstadt]]}}
{{BSe|DST||[[Hannoverscher Bahnhof (Osnabrück)|Hannoverscher Bahnhof]]}}
{{BS3||ABZgl|STR+r|||[[Bahnstrecke Wanne-Eickel–Hamburg|„Kluskurve“ nach Bremen]]}}
{{BS3|ABZq+lr|TBHFu|ABZql+r|132,4|'''[[Osnabrück Hauptbahnhof|Osnabrück Hbf]]''' {{Coordinate|text=ICON0|NS=52.272851|EW=8.061781|type=landmark|region=DE-NI|name=Hbf Osnabrück}} |([[Turmbahnhof]])}}
{{BS3|ABZg+l|ABZgr|STR|||[[Bahnstrecke Wanne-Eickel–Hamburg|„Münsterkurve“ von Münster]]}}
{{BS3|DST|STR|STR||Osnabrück Rbf Fledder}}
{{BS3|STR|ABZgl+l|STRr|||„Stahlwerkskurve“ bzw. „Schinkelkurve“}}
{{BS3|STRl|ABZg+r||129,3|Lüstringen|([[Abzweigstelle|Abzw]])}}
{{BS|SBRÜCKE|||[[Bundesautobahn 33|A 33]]}}
{{BSe|BHF|128,6|Lüstringen|(PV bis 1980)}}
{{BS|BHF|122,4|[[Bissendorf|Wissingen]]}}
{{BS|HST|116,8|[[Oldendorf (Melle)|Westerhausen]] ([[Melle]])}}
{{BS|BST|113,4|Melle-Euer Heide|(Awanst)}}
{{BS|BHF|111,3|[[Melle]] {{Coordinate|text=ICON0|NS=52.209263|EW=8.343043|type=landmark|region=DE-NW|name=Melle Bf}}}}
{{BS|BHF|104,0|[[Bruchmühlen (Melle)|Bruchmühlen]] {{Coordinate|text=ICON0|NS=52.20378|EW=8.451952|type=landmark|region=DE-NW|name=Bruchmühlen Bf}}}}
{{BS|STR+GRZq|103,8||Landesgrenze Nds / NRW {{Coordinate|text=ICON0|NS=52.203649|EW=8.452585|type=landmark|region=DE-NW|name=Landesgrenze Nds / NRW}}}}
{{BSe|BST|103,0|Möller|(Awanst)}}
{{BS|BST|98,5|Stadt Bünde|4= ([[Gleisanschluss#Anschlussstelle|Anst]]) <span style="color:#555555;">''ehem. Ahle (Kr Herford)''</span> {{Coordinate|text=ICON0|NS=52.196132|EW=8.521754|type=landmark|region=DE-NW|name=Ahle Bf}}}}
{{BS|ABZg+l|||[[Bahnstrecke Bassum–Herford|Strecke von Rahden]] {{Coordinate|text=ICON0|NS=52.19848|EW=8.560925|type=landmark|region=DE-NW|name=Abzweig nach Rahden}}}}
{{BS|BHF|95,2|'''[[Bahnhof Bünde (Westf)|Bünde (Westf)]]'''{{Coordinate|text=ICON0|NS=52.202075|EW=8.573876|type=landmark|region=DE-NW|name=Bünde (Westf) Bf}}}}
{{BS3||BHF|exKBHFa|90,3|[[Kirchlengern]] {{Coordinate|text=ICON0|NS=52.196363|EW=8.646884|type=landmark|region=DE-NW|name=Kirchlengern Bf}}}}
{{BS3||ABZgr|exSTR|||[[Bahnstrecke Bassum–Herford|Strecke nach Herford]] {{Coordinate|text=ICON0|NS=52.195481|EW=8.653708|type=landmark|region=DE-NW|name=Abzweig nach Herford}}}}
{{BS3e||SBRÜCKE|exSTR|||[[Bundesstraße 239|B 239]]}}
{{BS3e||hKRZWae|exSTR|||[[Else (Werre)|Else]]}}
{{BS3e||SBRÜCKE|exSTR|||[[Bundesautobahn 30|A 30]]}}
{{BS3e|STR+l|ABZgr|exABZgl+l|||ehem. [[Wallücker Willem]] (Schmalspur) {{Coordinate|text=ICON0|NS=52.196691|EW=8.64624|type=landmark|region=DE-NW|name=ehemals Abzweig Wallücker Willelm}}}}
{{BS3|KRZu|ABZg+r|exSTR|87,8|Löhne (Westf) Gbf West {{Coordinate|text=ICON0|NS=52.191496|EW=8.693125|type=landmark|region=DE-NW|name=Löhne (Westf) Gbf West}}}}
{{BS3|ABZg+r|STR|exSTR|||[[Bahnstrecke Hamm–Minden|Hauptstrecke von Herford]] {{Coordinate|text=ICON0|NS=52.193508|EW=8.7032|type=landmark|region=DE-NW|name=Abzweig nach Herford}}}}
{{BS3|STR|DST|exSTR|86,8|Löhne (Westf) Gbf {{Coordinate|text=ICON0|NS=52.193443|EW=8.701386|type=landmark|region=DE-NW|name=Löhne (Westf) Gbf}}}}
{{BS3|BHF|DST|exKBHFe|85,3|[[Bahnhof Löhne|Löhne (Westf) Pbf]] {{Coordinate|text=ICON0|NS=52.196586|EW=8.712759|type=landmark|region=DE-NW|name=Löhne (Westf) Pbf}}}}
{{BS3|ABZgr|STR||||[[Bahnstrecke Elze–Löhne|Strecke nach Hameln]]}}
{{BS3|STR|STR||||[[Bahnstrecke Hamm–Minden|Hauptstrecke nach Minden]]}}
"""

let daten1 = """
{{BS-header|Hamm–Minden}}
{{BS-daten
| DE-STRECKENNR = 1700 <small>(Personenverkehr)</small><br /><!-- -->2990 <small>(Güterverkehr)</small>
| DE-KBS = 400 <small>(Hamm–Bielefeld)</small><br />370 <small>(Bielefeld–Minden)</small>
| LÄNGE = 112
| SPURWEITE = 1435
| V-MAX = 200 bzw. 160
| STRECKENKLASSE = D4
| STROMW = 15 kV, 16,7 Hz
| ZWEIGLEISIG = <small>durchgehend</small>
| BILDPFAD_KARTE = kme2.png
| PIXEL_KARTE = 270px
| TEXT_KARTE = <div align="center">'''[[Stammstrecke der Köln-Mindener Eisenbahn-Gesellschaft|Stammstrecke Köln–Minden]] in Dunkelrot'''</div>
}}
{{BS-table}}
{{BS2||STR|||[[Bahnstrecke Hannover–Minden|Hauptstrecke von Hannover]]}}
{{BS2|STR+r|STR|||[[Bahnstrecke Nienburg–Minden|von Nienburg]]}}
{{BS2|KRZu|KRZu|||[[Mindener Kreisbahn]]}}
{{BS2|DST-L|DST-R|63,0|Minden (Westf) Gbf| {{Coordinate|text=ICON0|NS=52.292076|EW=8.934245|type=landmark|region=DE-NW|name=Minden Gbf}}}}
{{BS2|BHF-L|SBHF-R|64,4|'''[[Bahnhof Minden (Westfalen)|Minden (Westf)]]'''|([[Inselbahnhof]]) {{Coordinate|text=ICON0|NS=52.290567|EW=8.934824|type=landmark|region=DE-NW|name=Minden (Westf) Bf}}}}
{{BS2e|STR|eBST|67,6|Porta Po|(Abzw) {{Coordinate|text=ICON0|NS=52.262379|EW=8.932378|type=landmark|region=DE-NW|name=Abzw Porta Po}}}}
{{BS2e|eABZg+r|eABZgr|||ehem. [[Bahnstrecke Porta Westfalica–Häverstädt|Stichstrecke von/nach Häverstädt]]}}
{{BS2|DST|eBHF|68,5|Porta Westfalica|(Gbf) {{Coordinate|text=ICON0|NS=52.257717|EW=8.929224|type=landmark|region=DE-NW|name=Porta Westfalica Gbf}}}}
{{BS2e|eÜST|eÜST|69,1|Porta Westfalica Üst<!-- Welche Sttecke? -->| {{Coordinate|text=ICON0|NS=52.251044|EW=8.925555|type=landmark|region=DE-NW|name=Porta Westfalica Üst}}}}
{{BS2|STR|HST|69,9|[[Bahnhof Porta Westfalica|Porta Westfalica Hp]]| {{Coordinate|text=ICON0|NS=52.243201|EW=8.92004|type=landmark|region=DE-NW|name=Porta Westfalica Hp}}}}
{{BS2|DST|eBHF|73,9|[[Vennebeck]]|{{Coordinate|text=ICON0|NS=52.222121|EW=8.87352|type=landmark|region=DE-NW|name=Vennebeck Bf}}}}
{{BS2|hKRZWae|hKRZWae|||[[Weser]] {{Coordinate|text=ICON0|NS=52.212261|EW=8.85011|type=landmark|region=DE-NW|name=Querung Weser}}}}
{{BS2|KRWg+l|KRWgr|77,3|Bad Oeynhausen Gbf Abzw}}
{{BS2|DST|eBHF|78,2|Bad Oeynhausen Gbf| {{Coordinate|text=ICON0|NS=52.20666|EW=8.812194|type=landmark|region=DE-NW|name=Bad Oeynhausen Gbf}}}}
{{BS2|HST|HST|79,5|'''[[Bahnhof Bad Oeynhausen|Bad Oeynhausen]]'''|{{Coordinate|text=ICON0|NS=52.205279|EW=8.795671|type=landmark|region=DE-NW|name=Bad Oeynhausen Hp}}}}
{{BS2|DST|eBHF|82,1|[[Gohfeld#Verkehr|Gohfeld]]|{{Coordinate|text=ICON0|NS=52.200071|EW=8.759016|type=landmark|region=DE-NW|name=Gohfeld Bf}}}}
{{BS2|STR|ABZg+l|||[[Bahnstrecke Elze–Löhne|Weserbahn von Hameln]] {{Coordinate|text=ICON0|NS=52.198112|EW=8.738272|type=landmark|region=DE-NW|name=Abzweig nach Hameln}}}}
{{BS2|BHF-L|BHF-R|85,3|[[Bahnhof Löhne (Westfalen)|Löhne (Westf) Pbf]]|{{Coordinate|text=ICON0|NS=52.196363|EW=8.71381|type=landmark|region=DE-NW|name=Löhne (Westf) Bf}}}}
{{BS2|DST-L|DST-R|86,8|Löhne (Westf) Gbf| {{Coordinate|text=ICON0|NS=52.193245|EW=8.700464|type=landmark|region=DE-NW|name=Löhne (Westf) Gbf}}}}
{{BS2|KRZor|ABZgr|||[[Bahnstrecke Löhne–Rheine|Hannoversche Westbahn nach Bünde]] {{Coordinate|text=ICON0|NS=52.190957|EW=8.691323|type=landmark|region=DE-NW|name=Abzweig Hannoversche Westbahn nach Bünde}}}}
{{BS4||KRZo|KRZo|STR+r|||[[Ravensberger Bahn|Ravensberger Bahn von Bünde]]}}
{{BS4||STR|STR|HST|(91,8)|[[Haltepunkt Hiddenhausen-Schweicheln|Hiddenhausen-Schweicheln]]|{{Coordinate|text=ICON0|NS=52.158414|EW=8.668234|type=landmark|region=DE-NW|name=Hiddenhausen-Schweicheln Hp}}}}
{{BS4||DST-L|BHF-M|BHF-R|95,6|'''[[Bahnhof Herford|Herford]]'''|{{Coordinate|text=ICON0|NS=52.119274|EW=8.663428|type=landmark|region=DE-NW|name=Herford Bf}}}}
{{BS4||STR|STR|STRl|||[[Bahnstrecke Herford–Himmighausen|nach Detmold]] {{Coordinate|text=ICON0|NS=52.113463|EW=8.657849|type=landmark|region=DE-NW|name=Abzweig nach Detmold}}}}
{{BS2|BST|STR|98,0|[[Diebrock]]|(Anst) {{Coordinate|text=ICON0|NS=52.103197|EW=8.641198|type=landmark|region=DE-NW|name=Diebrock Anst}}}}
{{BS2|DST|HST|102,6|[[Haltepunkt Brake (Bielefeld)|Brake (b Bielefeld)]]|{{Coordinate|text=ICON0|NS=52.069522|EW=8.60472|type=landmark|region=DE-NW|name=Brake (b Bielefeld) Bf}}}}
{{BS2|hSTRae|hSTRae|||[[Schildescher Viadukt]] {{Coordinate|text=ICON0|NS=52.054866|EW=8.570044|type=landmark|region=DE-NW|name=Schildescher Viadukt}}}}
{{BS2|DST|STR|108,1|Bielefeld Hbf Vorbahnhof| {{Coordinate|text=ICON0|NS=52.03668|EW=8.549852|type=landmark|region=DE-NW|name=Bielefeld Vbf}}}}
{{BS2|STR|ABZg+l|||[[Begatalbahn|Begatalbahn von Lemgo]] {{Coordinate|text=ICON0|NS=52.033248|EW=8.539853|type=landmark|region=DE-NW|name=Abzweig nach Lemgo}}}}
{{BS2|BHF-L|BHF-R|109,5|'''[[Bielefeld Hauptbahnhof|Bielefeld Hbf Pbf]]'''|{{Coordinate|text=ICON0|NS=52.029802|EW=8.533781|type=landmark|region=DE-NW|name=Bielefeld Hbf}}}}
{{BS2|DST|STR|112,7|Brackwede Gbf| {{Coordinate|text=ICON0|NS=52.004751|EW=8.504984|type=landmark|region=DE-NW|name=Brackwede Gbf}}}}
{{BS2|BHF-L|BHF-R|113,8|[[Bahnhof Brackwede|Brackwede]]|{{Coordinate|text=ICON0|NS=51.997036|EW=8.498504|type=landmark|region=DE-NW|name=Brackwede Bf}}}}
{{BS2|ABZgr|STR|||[[Haller Willem|Haller Willem nach Osnabrück]] {{Coordinate|text=ICON0|NS=51.99553|EW=8.497281|type=landmark|region=DE-NW|name=Abzweig nach Osnabrück}}}}
{{BS2|ABZgl|KRZu|||[[Senne-Bahn|Sennebahn nach Paderborn]] {{Coordinate|text=ICON0|NS=51.987999|EW=8.496058|type=landmark|region=DE-NW|name=Abzweig nach Paderborn}}}}
{{BS2e|STR|eHST|118,9|Ummeln|{{Coordinate|text=ICON0|NS=51.957444|EW=8.466385|type=landmark|region=DE-NW|name=Ummeln ehm. Bf}}}}
{{BS2|BST|STR|121,7|Isselhorst-Avenwedde Anst| {{Coordinate|text=ICON0|NS=51.94117|EW=8.440461|type=landmark|region=DE-NW|name=Isselhorst-Avenwedde Anst}}}}<!-- ist eigentlich eine Awanst! -->
{{BS2|STR|HST|121,8|[[Isselhorst#Verkehr|Isselhorst]]-[[Avenwedde#Verkehr|Avenwedde]]|{{Coordinate|text=ICON0|NS=51.939569|EW=8.437886|type=landmark|region=DE-NW|name=Isselhorst-Avenwedde Hp}}}}
{{BS2|STR2|STR3u|||[[Überwerfungsbauwerk]] Avenwedde {{Coordinate|text=ICON0|NS=51.933760|EW=8.428610|type=landmark|dim=500|name=Überwerfungsbauwerk Avenwedde|region=DE-NW|}}}}
{{BS2|STR+1u|STR+4||||}}
{{BS2|KRZo|KRZo|||[[Bahnstrecke Ibbenbüren–Hövelhof|Ibbenbüren–Hövelhof]] {{Coordinate|text=ICON0|NS=51.918676|EW=8.404756|type=landmark|region=DE-NW|name=Querung Bahnstrecke Ibbenbüren–Hövelhof}}}}
{{BS2|STR|ABZg+l|||Verbindungskurve [[Bahnstrecke Ibbenbüren–Hövelhof|nach Gütersloh Nord]] {{Coordinate|text=ICON0|NS=51.911688|EW=8.393447|type=landmark|region=DE-NW|name= Verbindungskurve zur Bahnstrecke Ibbenbüren–Hövelhof}}}}
{{BS2|BHF-L|DST-R|126,9|'''[[Gütersloh Hauptbahnhof|Gütersloh Hbf]]'''| {{Coordinate|text=ICON0|NS=51.907028|EW=8.385143|type=landmark|region=DE-NW|name=Gütersloh Hbf}}}}
{{BS2e|STR|eABZg+l|||ehem. [[Rhedaer Bahn|Rhedaer Bahn von Lippstadt]] {{Coordinate|text=ICON0|NS=51.85977|EW=8.301501|type=landmark|region=DE-NW|name=Abzweig nach Rheda}}}}
{{BS2|BHF-L|BHF-R|135,8|[[Bahnhof Rheda-Wiedenbrück|Rheda-Wiedenbrück]]|{{Coordinate|text=ICON0|NS=51.857292|EW=8.286395|type=landmark|region=DE-NW|name=Rheda-Wiedenbrück Bf}}}}
{{BS2|KRZo|ABZgr|||[[Warendorfer Bahn|Warendorfer Bahn nach Münster]]{{Coordinate|text=ICON0|NS=51.855953|EW=8.281889|type=landmark|region=DE-NW|name=Abzweig nach Münster}}}}
{{BS2|BHF-L|DST-R|146,2|[[Bahnhof Oelde|Oelde]]|{{Coordinate|text=ICON0|NS=51.829068|EW=8.142049|type=landmark|region=DE-NW|name=Oelde Bf}}}}
{{BS2|ABZg+r|STR|||[[Bahnstrecke_Neubeckum–Warendorf|von Ennigerloh]]}}
{{BS2|BHF|BHF|155,1|[[Bahnhof Neubeckum|Neubeckum Pbf]]|{{Coordinate|text=ICON0|NS=51.801795|EW=8.020942|type=landmark|region=DE-NW|name=Neubeckum Bf}}}}
{{BS2|ABZgr|STR|||[[Bahnstrecke Münster–Warstein|nach Münster]]}}
{{BS2|STR|ABZgl+l|||[[Bahnstrecke Münster–Warstein|nach Beckum]]}}
{{BS2|DST-L|DST-R|155,9|Neubeckum Gbf}}
{{BS2e|eHST|STR|159,4|Vorhelm (PV bis Mai 1988)}}
{{BS2|eBHF|DST|162,5|Ahlen (Westf) Gbf}}
{{BS2|BHF|HST|165,1|[[Bahnhof Ahlen (Westfalen)|Ahlen (Westf)]]|(Notbahnsteig auf Güterstrecke) {{Coordinate|text=ICON0|NS=51.760987|EW=7.8952|type=landmark|region=DE-NW|name=Ahlen (Westf) Bf}}}}
{{BS2|STR2|STR3u|||[[Überwerfungsbauwerk]] Ahlen {{Coordinate|text=ICON0|NS=51.742100|EW=7.872490|type=landmark|dim=500|region=DE-NW|name=Überwerfungsbauwerk Ahlen}}}}
{{BS2|STR+1u|STR+4||||}}
{{BS2|HST|HST|172,3|[[Haltepunkt Hamm-Heessen|Heessen Hp]]| {{Coordinate|text=ICON0|NS=51.709547|EW=7.83175|type=landmark|region=DE-NW|name=Heessen Hp}}}}
{{BS2|DST-L|DST-R|173,9|[[Bahnhof Heessen|Heessen]]|''(ehem. PV)''}}
{{BS4|SHI4grq|KRZu|ABZg+r||||[[Bahnstrecke Münster–Hamm|von Münster]], Hamm Feldmark ([[Abzw]])}}
{{BS4|SHI4+lq|ABZg+r|STR||||}}
{{BS2|hKRZWae|hKRZWae|||[[Lippe (Fluss)|Lippe]], [[Datteln-Hamm-Kanal]]}}
{{BS2|STR|BHF|176,4|'''[[Hamm (Westfalen) Hauptbahnhof|Hamm (Westf) Pbf]]'''|{{Coordinate|text=ICON0|NS=51.678397|EW=7.807481|type=landmark|region=DE-NW|name=Hamm (Westf) Pbf}}}}
{{BS2|KRWg+l|KRWgr|176,8|Hamm (Westf) Rbf Hvn|(Abzw)}}
{{BS2|STR|ABZgl|||[[Bahnstrecke Hamm–Warburg|nach Warburg]]}}
{{BS2|STR|ABZgl|||[[Bahnstrecke Hagen–Hamm|nach Hagen]]}}
{{BS2|ABZg+l|KRZu|||zur [[Bahnstrecke Hamm–Warburg|Strecke nach Warburg]] (Hamm Gallberg, Abzw)}}
{{BS2|DST|STR||Hamm (Westf) Rbf}}
{{BS2|ABZgr|STR|||[[Bahnstrecke Oberhausen-Osterfeld–Hamm|Güterstrecke nach Oberhausen-Osterfeld]]}}
{{BS2|KRWgl|KRWg+r||Selmig Abzw}}
{{BS2|STRl|KRZu|||zur [[Bahnstrecke Hagen–Hamm|Strecke nach Hagen]] (Bönen Autobahn, Abzw)}}
{{BS2|BS2c2|BS2r|||[[Bahnstrecke Dortmund–Hamm|Hauptstrecke nach Dortmund]]}}
"""

let daten2 = """
{{BS-header|Nordhausen Nord–Wernigerode<re>{{Eisenbahnatlas|2007|D}}</re><re>{{Tunnelportale|9700}}</re>}}
{{BS-daten
| DE-STRECKENNR= 9700
| LÄNGE= 60,5
| SPURWEITE= 1000
| V-MAX=40
| NEIGUNG=33
| RADIUS=60
| BILDPFAD_FOTO= Harzquerbahn near Drei Annen Hohne - 2007-09-19.jpg
| PIXEL_FOTO= 350px
| TEXT_FOTO= Dampfzug unterwegs auf der Harzquerbahn (2007)
| BILDPFAD_KARTE= Karte Harzer Schmalspurbahnen.png
| PIXEL_KARTE= 350px
| TEXT_KARTE= Die blaue Markierung entspricht dem Streckenverlauf der Harzquerbahn
}}
{{BS-table}}
{{BS2|uSTR+r||||[[Straßenbahn Nordhausen|Straßenbahnstrecke zur Innenstadt]]|}}
{{BS2|uBHF|||Nordhausen Bahnhofsplatz|}}
{{BS2|uABZgr||||[[Straßenbahn Nordhausen|Straßenbahnstrecke von der Innenstadt]]|}}
{{BS2|uSTR|KBHFa|0,0|[[Nordhausen]] Nord|[[Bahnhof Nordhausen|Übergang zur DB AG]]|184 m}}
{{BS2|uWECHSEL|STR|||Systemwechsel [[Straßenbahn-Bau- und Betriebsordnung|BOStrab]] / [[Eisenbahn-Bau- und Betriebsordnung für Schmalspurbahnen|ESBO]]}}
{{BS2|BS2l|BS2r|0,2||vom Straßenbahnnetz ([[Straßenbahn Nordhausen|Linie {{Bahnlinie|U||10|#FFFFFF|#009933|#009933}}]], seit 2004)}}
{{BS|SBRÜCKE|||Bruno-Kunze-Straße}}
{{BS|BRÜCKE1|1,0||Freiherr-vom-Stein-Straße}}
{{BS|DST|1,1|Nordhausen Übergabebahnhof}}
{{BS|BUE|1,5||Hesseröder Straße|}}
{{BS|HST|1,5|Nordhausen Hesseröder Straße|}}
{{BS|HST|2,2|Nordhausen Altentor||189 m}}
{{BS|HST|3,0|Nordhausen Ricarda-Huch-Straße|}}
{{BS|HST|3,8|Nordhausen Schurzfell|}}
{{BS|BHF|4,5|Nordhausen-Krimderode||198 m}}
{{BS|WBRÜCKE1|||[[Zorge (Fluss)|Zorge]]}}
{{BS|BUE|||[[Bundesstraße 4|B 4]]}}
{{BS|HST|6,0|Niedersachswerfen Herkulesmarkt|}}
{{BSe|ABZg+r|||von Harzungen Lager}}
{{BS|BHF|7,0|[[Niedersachswerfen]] Ost||213 m}}
{{BS|HST|8,0|Niedersachswerfen Ilfelder Straße|}}
{{BS|HST|9,9|Ilfeld Schreiberwiese|}}
{{BS|BHF|10,7|[[Ilfeld]]|früher Ilfeld-[[Wiegersdorf]]|254 m}}
{{BS|HST|11,5|Ilfeld Neanderklinik|(Endpunkt [[Straßenbahn Nordhausen|Linie {{Bahnlinie|U||10|#FFFFFF|#009933|#009933}}]])|267 m}}
{{BS|BUE|||B 4}}
{{BSe|ABZgr|11,7||Anschluss Papierfabrik}}
{{BS|HST|12,6|Ilfeld Bad||287 m}}
{{BS|hKRZWae|||[[Bere (Zorge)|Bere]], Ilfelder Viadukt}}
{{BS|BUE|||B 4}}
{{BS|HST|14,0|[[Netzkater]]|| 309 m}}
{{BS|WBRÜCKE1|||Brandesbach}}
{{BS|WBRÜCKE1|||Kleiner Merkelsbach}}
{{BS|WBRÜCKE1|||Großer Merkelsbach}}
{{BS|BHF|17,3|[[Eisfelder Talmühle]]||352 m}}
{{BS|ABZgr|||[[Selketalbahn|nach Stiege]]}}
{{BS|BUE|||[[Bundesstraße 81|B 81]]}}
{{BS|WBRÜCKE1|||[[Bere (Zorge)|Bere]]}}
{{BS|HST|19,5|Tiefenbachmühle||411 m}}
{{BS|HST|21,5|[[Sophienhof (Harztor)|Sophienhof]]||445 m}}
{{BS|STR+GRZq|||Landesgrenze [[Thüringen]] / [[Sachsen-Anhalt]]}}
{{BS|BST|25,1|[[Zugleitstelle|Zlst]] Kälberbruch|(ehem. Holzverladung)}}
{{BS|WBRÜCKE1|||Dammbach}}
{{BS|WBRÜCKE1|||[[Rappbode]]}}
{{BS|BHF|29,8|[[Stadt Benneckenstein (Harz)|Benneckenstein]]||530 m}}
{{BS|HST|33,4|[[Sorge (Harz)|Sorge]]|(seit 1974)|490 m}}
{{BS|hKRZWae|33,9||[[Warme Bode]]}}
{{BS|BUE|||[[Bundesstraße 242|B 242]]}}
{{BSe|BHF|34,2|Sorge|(unterer Bf / NWE) bis 1974|486 m}}
{{BSe|ABZgl|||[[Südharz-Eisenbahn|nach Braunlage]]}}
{{BSe|KRZu|||[[Südharz-Eisenbahn]]}}
{{BS|BST|37,4|Zlst Allerbach|(ehem. Holzverladung)}}
{{BS|WBRÜCKE1|||Kleiner Allerbach}}
{{BS|GIPl|38,2||Scheitelpunkt|556 m}}
{{BS|WBRÜCKE1|||Ochsenbach}}
{{BS|hSTRae|40,5||[[Bundesstraße 27|B 27]]}}
{{BS|hKRZWae|41,1||[[Kalte Bode]]}}
{{BS|BHF|41,6|[[Elend (Harz)|Elend]]||509 m}}
{{BS|WBRÜCKE1|||[[Wormke]]}}
{{BS|WBRÜCKE1|||Dammastbach}}
{{BS|WBRÜCKE1|||Steinbach}}
{{BS|ABZg+l|||[[Brockenbahn|von Schierke]]||}}
{{BS|BHF|46,4|[[Bahnhof Drei Annen Hohne|Drei Annen Hohne]]|ehem. zur [[Rübelandbahn]]||543 m}}
{{BS|WBRÜCKE1|||[[Zillierbach]]}}
{{BS|WBRÜCKE1|||Drängetalwasser}}
{{BS|DST|50,4|Drängetal|}}
{{BS|TUNNEL1|51,6||Thumkuhlenkopftunnel (58 m)}}
{{BS|WBRÜCKE1|||[[Braunes Wasser (Holtemme)|Braunes Wasser]]}}
{{BSe|ABZg+l|52,7||Schotterwerk Thumkuhlental}}
{{BS|BHF|54,6|[[Steinerne Renne]]||311 m}}
{{BSe|ABZg+r|54,9||Anschluss Marmorwerke}}
{{BS|WBRÜCKE1|||[[Holtemme]]}}
{{BS|WBRÜCKE1|||Holtemme}}
{{BS|WBRÜCKE1|||Holtemme}}
{{BS|WBRÜCKE1|||Holtemme}}
{{BS|BHF|56,2|Wernigerode-Hasserode||280 m}}
{{BS|WBRÜCKE1|||[[Braunes Wasser (Holtemme)|Braunes Wasser]]}}
{{BSe|ABZg+l|55,7||Anschluss Steinrampe}}
{{BSe|HST|57,1|Hasserode II|– Frankenfeldstraße (bis 1922)}}
{{BSe|ABZg+l|57,5||Anschluss Papierfabrik}}
{{BS|HST|58,0|WR-[[Hochschule Harz]]|(WR-Kirchstraße)|256 m}}
{{BSe|ABZgr|58,5||Anschluss Papierfabrik Marschhausen}}
{{BSe|HST|59,0|Westerntor|(bis 1936)}}
{{BS|BUE|59,3||Westerntorkreuzung [[Bundesstraße 244|B 244]]}}
{{BS|WBRÜCKE1|||[[Zillierbach]]}}
{{BS|BHF|59,5|Wernigerode-Westerntor||238 m}}
{{BS2|BS2+l|BS2c4|||}}
{{BS2|ABZg+l|KDSTeq||Wernigerode Übergabebahnhof}}
{{BS2|KBHFe||60,5|[[Wernigerode#Verkehr|Wernigerode]]|[[Bahnstrecke Heudeber-Danstedt–Bad Harzburg/Vienenburg|Übergang zur DB AG]]|234 m}}
"""

[<SetUp>]
let Setup () = ()

// skip comments
let prepare (s: string) =
    let regex1 = Regex(@"<!--.*?-->")
    let s1 = regex1.Replace(s, "")
    let regex1 = Regex(@"<!--.*?-->")
    regex1.Replace(s1, "")

[<Test>]
let TestPrepare () =
    let s =
        (prepare "abc<!-- ist eigentlich - eine Awanst! -->")

    Assert.That(s, Is.EqualTo("abc"))

[<Test>]
let TestParseDaten0 () =
    match Parser.parse (prepare daten0) with
    | Success (result, _, _) ->
        fprintfn stderr "Success: %A" result
        Assert.That(result.Length, Is.EqualTo(71))
    | Failure (errorMsg, _, _) ->
        fprintfn stderr "Failure: %s" errorMsg
        Assert.That(0, Is.EqualTo(1))

[<Test>]
let TestParseDaten1 () =
    match Parser.parse (prepare daten1) with
    | Success (result, _, _) ->
        fprintfn stderr "Success: %A" result
        Assert.That(result.Length, Is.EqualTo(74))
    | Failure (errorMsg, _, _) ->
        fprintfn stderr "Failure: %s" errorMsg
        Assert.That(0, Is.EqualTo(1))

[<Test>]
let TestParseDaten2 () =
    match Parser.parse (prepare daten2) with
    | Success (result, _, _) ->
        fprintfn stderr "Success: %A" result
        Assert.That(result.Length, Is.EqualTo(91))
    | Failure (errorMsg, _, _) ->
        fprintfn stderr "Failure: %s" errorMsg
        Assert.That(0, Is.EqualTo(1))
