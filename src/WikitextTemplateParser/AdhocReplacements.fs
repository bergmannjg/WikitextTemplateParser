/// replacements with no general rule
module AdhocReplacements

open System.Text.RegularExpressions

let regexRef = Regex(@"<ref[^>]*>.+?</ref>")
let regexRefSelfClosed = Regex(@"<ref[^/]*/>")
let regexComment = Regex(@"<!--.*?-->")

/// maybe errors in wikitext
let replacements = [|
    ("Berliner Nordbahn", "{{BS2||", "{{BS2|")
    ("Bahnstrecke Lübbenau–Kamenz", "{{BS|BHF|T=STR|" ,"{{BS2|BHF|T=STR|")
|]
