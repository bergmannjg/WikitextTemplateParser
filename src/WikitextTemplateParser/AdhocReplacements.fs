/// replacements with no general rule
module AdhocReplacements

/// maybe errors in wikitext
let replacements = [|
    ("Berliner_Nordbahn", "{{BS2||", "{{BS2|")
    ("Bahnstrecke_Lübbenau–Kamenz", "{{BS|BHF|T=STR|" ,"{{BS2|BHF|T=STR|")
|]
