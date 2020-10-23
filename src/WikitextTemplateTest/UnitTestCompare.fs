// export VSTEST_HOST_DEBUG=1
module TestCompare

open NUnit.Framework
open Ast
open RouteInfo
open StationsOfRoute

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

let checkStationDistance (stations: StationOfRoute []) (name: string) (km: string) =
    let s =
        stations |> Array.tryFind (fun b -> b.name = name)

    Assert.That(s.IsSome, Is.EqualTo(true))
    //Assert.That(sprintf "%.1f" s.Value.km, Is.EqualTo(km))

[<Test>]
let TestCompareBerlinBlankenheim () =
    let templates =
        loadTemplatesForWikiTitle "Bahnstrecke_Berlin–Blankenheim"

    Assert.That(templates.Length, Is.EqualTo(110))

    match findBsDatenStreckenNr templates "Bahnstrecke_Berlin–Blankenheim" with
    | Some strecken -> 
        let bahnhöfe = findStations strecken.[0] templates 
        Assert.That(bahnhöfe.Length, Is.EqualTo(23))
        checkStationDistance bahnhöfe "Berlin-Charlottenburg" "0.0"
        checkStationDistance bahnhöfe "Berlin-Wannsee" "12.7"
    | None -> Assert.Fail("no stations found")

[<Test>]
let TestCompareNürnbergFeucht () =
    let templates =
        loadTemplatesForWikiTitle "Bahnstrecke_Nürnberg–Feucht"

    Assert.That(templates.Length, Is.EqualTo(27))

    match findBsDatenStreckenNr templates "Bahnstrecke_Nürnberg–Feucht" with
    | Some strecken -> 
        let bahnhöfe = findStations strecken.[0] templates 
        Assert.That(bahnhöfe.Length, Is.EqualTo(7))
        checkStationDistance bahnhöfe "Nürnberg Hbf" "0.0"
        checkStationDistance bahnhöfe "Feucht" "12.5"
    | None -> Assert.Fail("no stations found")

[<Test>]
let TestCompareAltenbekenKreiensen () =
    let templates =
        loadTemplatesForWikiTitle "Bahnstrecke_Altenbeken–Kreiensen"

    Assert.That(templates.Length, Is.EqualTo(45))

    match findBsDatenStreckenNr templates "Bahnstrecke_Altenbeken–Kreiensen" with
    | Some strecken -> 
        Assert.That(strecken.Length, Is.EqualTo(3))
        let bahnhöfe = findStations strecken.[1] templates 
        Assert.That(bahnhöfe.Length, Is.EqualTo(10))
        checkStationDistance bahnhöfe "Langeland" "3.5"
    | None -> Assert.Fail("no stations found")
