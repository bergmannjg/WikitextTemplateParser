// export VSTEST_HOST_DEBUG=1
module TestCompare

open NUnit.Framework
open Ast
open AstUtils
open Stations

[<SetUp>]
let Setup () = Serializer.addConverters ([||])

let loadTemplatesForWikiTitle (title: string) =
    let path = "../../../testdata/" + title + ".json"

    printfn "loading %s" path
    if System.IO.File.Exists path then
        let wikitext = System.IO.File.ReadAllText path

        let templates =
            Serializer.Deserialize<Template []>(wikitext)

        templates
    else
        fprintfn stderr "file not found: %s" path
        Array.empty

let checkStationDistance (stations: Station []) (name: string) (km: string) =
    let s =
        stations |> Array.tryFind (fun b -> b.name = name)

    Assert.That(s.IsSome, Is.EqualTo(true))
    Assert.That(sprintf "%.1f" s.Value.km, Is.EqualTo(km))

[<Test>]
let TestCompareBerlinBlankenheim () =
    let templates =
        loadTemplatesForWikiTitle "Bahnstrecke_Berlin–Blankenheim"

    Assert.That(templates.Length, Is.EqualTo(110))

    let bahnhöfe = findBahnhöfe templates Array.empty
    Assert.That(bahnhöfe.Length, Is.EqualTo(25))

    checkStationDistance bahnhöfe "Berlin-Charlottenburg" "0.0"
    checkStationDistance bahnhöfe "Berlin-Wannsee" "12.7"

[<Test>]
let TestCompareNürnbergFeucht () =
    let templates =
        loadTemplatesForWikiTitle "Bahnstrecke_Nürnberg–Feucht"

    Assert.That(templates.Length, Is.EqualTo(27))

    let bahnhöfe = findBahnhöfe templates Array.empty
    Assert.That(bahnhöfe.Length, Is.EqualTo(7))

    checkStationDistance bahnhöfe "Nürnberg Hbf" "0.0"
    checkStationDistance bahnhöfe "Feucht" "12.5"

[<Test>]
let TestCompareAltenbekenKreiensen () =
    let templates =
        loadTemplatesForWikiTitle "Bahnstrecke_Altenbeken–Kreiensen"

    Assert.That(templates.Length, Is.EqualTo(45))

    let bahnhöfe = findBahnhöfe templates Array.empty
    Assert.That(bahnhöfe.Length, Is.EqualTo(20))

    checkStationDistance bahnhöfe "Altenbeken" "110.8"
    checkStationDistance bahnhöfe "Kreiensen" "105.8"

    let bahnhöfe1760 = findBahnhöfe templates [|"Altenbeken"; "Langeland"|]
    Assert.That(bahnhöfe1760.Length, Is.EqualTo(2))
