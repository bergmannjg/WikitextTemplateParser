
open Ast
open RouteInfo
open StationsOfRoute
open StationsOfInfobox
open DbData
open Comparer
open ResultsOfMatch
       
let loadTemplatesForWikiTitle (title:string) showDetails  =
    let path = if title.StartsWith("./wikidata/") then title else "./wikidata/" + title + ".json"
    if System.IO.File.Exists path then
        let wikitext = System.IO.File.ReadAllText path
        let templates = Serializer.Deserialize<Template[]>(wikitext)
        templates
    else
        fprintfn stderr "file not found: %s" path
        Array.empty
    
let comparetitle title showDetails =
    fprintfn stderr "loading : %s" title
    let templates = loadTemplatesForWikiTitle title showDetails
    let streckenAlle = match findBsDatenStreckenNr templates title with
                       | Some strecken ->
                            if strecken.Length = 0  then 
                                printResult (createResult title 0 RouteParameterNotParsed) showDetails
                                if showDetails then 
                                    printfn "see wikitext ./cache/%s.txt" title
                                    printfn "see templates ./wikidata/%s.txt" title
                            strecken
                       | None -> 
                            printResult (createResult title 0 RouteParameterEmpty) showDetails
                            Array.empty

    let strecken = streckenAlle |> Array.filter (fun s -> checkPersonenzugStreckenutzung s.nummer)
    if streckenAlle.Length > 0 && streckenAlle.Length > strecken.Length then 
        let streckenOhne = streckenAlle|>Array.map (fun s -> s.nummer)|>Array.filter (fun nr -> not (strecken|>Array.exists (fun s -> s.nummer=nr)))
        streckenOhne |> Array.iter (fun nr -> printResult (createResult title nr RouteIsNoPassengerTrain) showDetails)
        if showDetails then fprintfn stderr "%s, keine Fernbahnnutzung %A" title streckenOhne 
   
    if strecken.Length>0 then
        let precodedStations = templates |> Array.takeWhile (containsBorderStation >> not) |> Array.map findStationOfInfobox |> Array.choose id
        strecken
        |> Array.iter (fun strecke -> 
            let strecke0 = if strecken.Length = 1 then fillStreckeNames strecke precodedStations else strecke
            let dbStations = loadDBStations strecke0.nummer
            let wikiStations = match dbStations.Length > 0 with 
                                | true -> filterStations strecke0 precodedStations 
                                | _ -> [||]
            compare title strecke0 wikiStations dbStations precodedStations showDetails)

[<EntryPoint>]
let main argv =
    Serializer.addConverters ([| |])
    match argv with
    | [| "-comparetitle"; title |] -> comparetitle title false
    | [| "-verbose"; "-comparetitle"; title |] -> comparetitle title true
    | [| "-showResults"; path |] -> showResults(path)
    | _ -> fprintfn stderr "usage: [-verbose] -comparetitle title | -showResults path"   
    0
