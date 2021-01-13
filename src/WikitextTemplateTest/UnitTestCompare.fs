// export VSTEST_HOST_DEBUG=1
module TestCompare

open NUnit.Framework

open WikitextRouteDiagrams

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

    match RouteInfo.findRouteInfoInTemplates templates "Bahnstrecke_Altenbeken–Kreiensen" with
    | Some strecken ->
        Assert.That(strecken.Length, Is.EqualTo(3))
        let bahnhöfe = OpPointOfRoute.findStations strecken.[1] templates
        Assert.That(bahnhöfe.Length, Is.EqualTo(18))
        checkStationDistance bahnhöfe "Langeland" "3.5"
    | None -> Assert.Fail("no stations found")

type Edge = { a: string; b: string }

[<Test>]
let TestSortEdges1 () =

    let edges =
        ResizeArray(
            [ { a = "b"; b = "c" }
              { a = "a"; b = "b" }
              { a = "c"; b = "d" } ]
        )

    let nodeStart (n: Edge) = n.a
    let nodeEnd (n: Edge) = n.b
    let sorted = RInfData.sortEdges nodeStart nodeEnd edges

    let expected =
        [ { a = "a"; b = "b" }
          { a = "b"; b = "c" }
          { a = "c"; b = "d" } ]


    Assert.That(sorted.Length, Is.EqualTo(1))
    Assert.That(expected, Is.EqualTo(sorted.[0]))

[<Test>]
let TestSortEdges2 () =

    let edges =
        ResizeArray(
            [ { a = "b"; b = "c" }
              { a = "a"; b = "b" }
              { a = "d"; b = "e" } ]
        )

    let nodeStart (n: Edge) = n.a
    let nodeEnd (n: Edge) = n.b
    let sorted = RInfData.sortEdges nodeStart nodeEnd edges

    let expected1 =
        [ { a = "a"; b = "b" }
          { a = "b"; b = "c" } ]

    let expected2 = [ { a = "d"; b = "e" } ]

    Assert.That(sorted.Length, Is.EqualTo(2))
    Assert.That(expected1, Is.EqualTo(sorted.[0]))
    Assert.That(expected2, Is.EqualTo(sorted.[1]))

[<Test>]
let TestMatchStationName () =

    Assert.That(OpPointMatch.matchStationName "Berlin" "Berlin" true, Is.EqualTo(MatchKind.EqualNames))

    Assert.That(OpPointMatch.matchStationName "Abzw Rehsiepen" "Hagen Rehsiepen" true, Is.EqualTo(MatchKind.EndsWith))

    Assert.That(OpPointMatch.matchStationName "Hagen Rehsiepen" "Abzw Rehsiepen" true, Is.EqualTo(MatchKind.Failed))

    Assert.That(OpPointMatch.matchStationName "Himmelsthür (Abzw)" "Abzweig Himmelsthür" true, Is.EqualTo(MatchKind.EqualWithoutIgnored))

    Assert.That(OpPointMatch.matchStationName "Köln Steinstr. Abzw" "Köln Steinstraße (Abzw)" true, Is.EqualTo(MatchKind.SameSubstring))

    Assert.That(OpPointMatch.matchStationName "Abzw Werkleitz nach Magdeburg" "Werkleitz" true, Is.EqualTo(MatchKind.StartsWith))
