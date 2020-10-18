
open Ast
open AstUtils
open Stations
open PrecodedStation
open DbData
open Comparer

let showResults (path:string) =
    if System.IO.File.Exists path then
        let text = System.IO.File.ReadAllText path
        let results = Serializer.Deserialize<ResultOfRoute[]>(text)
        printfn "distinct routes count: %d" (results |> Array.countBy (fun r->r.route)).Length
        printfn "articles count : %d" (results |> Array.countBy (fun r->r.title)).Length
        printfn "route parameter empty: %d" (results |> Array.filter
            (fun r->r.resultKind = ResultKind.RouteParameterEmpty)).Length
        printfn "route parameter not parsed: %d" (results |> Array.filter
            (fun r->r.resultKind = ResultKind.RouteParameterNotParsed)).Length
        printfn "route is no passenger train: %d" (results |> Array.filter
            (fun r->r.resultKind = ResultKind.RouteIsNoPassengerTrain)).Length
        printfn "start/stop stations of route not found: %d" (results |> Array.filter
            (fun r->r.resultKind = ResultKind.StartStopStationsNotFound)).Length
        printfn "found wikidata : %d" (results |> Array.filter 
            (fun r->r.resultKind = ResultKind.WikidataFound)).Length
        printfn "not found wikidata in templates: %d" (results |> Array.filter
            (fun r->r.resultKind = ResultKind.WikidataNotFoundInTemplates)).Length
        printfn "not found wikidata in db data: %d" (results |> Array.filter 
            (fun r->r.resultKind = ResultKind.WikidataNotFoundInDbData)).Length
        printfn "no db data found: %d" (results |> Array.filter 
            (fun r->r.resultKind = ResultKind.NoDbDataFound)).Length
        let countUndef = (results |> Array.filter (fun r->r.resultKind = ResultKind.Undef)).Length
        if countUndef > 0 then fprintf stderr "undef result kind unexpected, count %d" countUndef
    else
        fprintfn stderr "file not found: %s" path
        
let loadTemplatesForWikiTitle (title:string) showDetails  =
    let path = if title.StartsWith("./wikidata/") then title else "./wikidata/" + title + ".json"
    if System.IO.File.Exists path then
        let wikitext = System.IO.File.ReadAllText path
        let templates = Serializer.Deserialize<Template[]>(wikitext)
        templates
    else
        fprintfn stderr "file not found: %s" path
        Array.empty
    
let loadDBStations routenr =
    loadDBRoutePosition routenr
    |>Array.map (fun p ->  {km = (getKMI2Float p.KM_I); name = p.BEZEICHNUNG})

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
        let precodedStations = templates |> Array.takeWhile (containsBorderStation >> not) |> Array.map findPrecodedStation |> Array.choose id
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
