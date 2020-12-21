// export VSTEST_HOST_DEBUG=1
module TestCompare

open NUnit.Framework
open Templates
open RouteInfo
open OpPointsOfRoute
open Types

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

let checkStationDistance (stations: WkOpPointOfRoute []) (name: string) (km: string) =
    let s =
        stations |> Array.tryFind (fun b -> b.name = name)

    Assert.That(s.IsSome, Is.EqualTo(true))
    //Assert.That(sprintf "%.1f" s.Value.km, Is.EqualTo(km))

[<Test>]
let TestCompareHammMinden () =
    let templates =
        loadTemplatesForWikiTitle "Bahnstrecke_Hamm–Minden"

    Assert.That(templates.Length, Is.EqualTo(74))

[<Test>]
let TestCompareAltenbekenKreiensen () =
    let templates =
        loadTemplatesForWikiTitle "Bahnstrecke_Altenbeken–Kreiensen"

    Assert.That(templates.Length, Is.EqualTo(45))

    match findRouteInfoInTemplates templates "Bahnstrecke_Altenbeken–Kreiensen" with
    | Some strecken -> 
        Assert.That(strecken.Length, Is.EqualTo(3))
        let bahnhöfe = findStations strecken.[1] templates 
        Assert.That(bahnhöfe.Length, Is.EqualTo(19))
        checkStationDistance bahnhöfe "Langeland" "3.5"
    | None -> Assert.Fail("no stations found")
