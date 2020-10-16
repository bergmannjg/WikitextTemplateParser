
open Ast
open AstUtils
open PrecodedStation
open DbData
open Comparer

let showResults (path:string) =
    if System.IO.File.Exists path then
        let text = System.IO.File.ReadAllText path
        let results = Serializer.Deserialize<ResultOfRoute[]>(text)
        // results |> Array.iter (printfn "%A")
        printfn "routes count: %d" results.Length
        printfn "articles count : %d" (results |> Array.countBy (fun r->r.title)).Length
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
        
let loadTemplatesForWikiTitle (title:string) =
    let path = if title.StartsWith("./wikidata/") then title else "./wikidata/" + title + ".json"
    if System.IO.File.Exists path then
        let wikitext = System.IO.File.ReadAllText path
        let templates = Serializer.Deserialize<Template[]>(wikitext)
        templates
    else
        fprintfn stderr "file not found: %s" path
        Array.empty

let generateIndex () =
    let files = System.IO.Directory.EnumerateFiles "./wikidata/" |> Seq.filter (fun f -> f.EndsWith ".json" && not (f.EndsWith "index.json"))
    let mutable map = Map.empty<string,int[]>
    for file in files do 
        let templates = loadTemplatesForWikiTitle file
        let strecken = findBsDatenStreckenNr templates
        let keys = strecken |> Array.map (fun strecke -> strecke.Key)
        let info = System.IO.FileInfo file
        map <- map.Add(info.Name.Replace(".json", ""), keys)
    System.IO.File.WriteAllText("./wikidata/index.json" ,Serializer.Serialize<Map<string,int[]>>(map))

let loadIndex () =
    let text = System.IO.File.ReadAllText ("./wikidata/index.json")
    Serializer.Deserialize<Map<string,int[]>>(text)
    
let routeHasSingleTitle (map:Map<string,int[]>) (route:int) =
    let mutable found = false
    for entry in map do
        if entry.Value.Length = 1 && Array.contains route entry.Value  then found <- true
    found
    
let bfStelleArt = [|"Bf";"Bft";"Hp"|]

let loadRailwayRoutePositions routenr =
    let dbtext = System.IO.File.ReadAllText ("./dbdata/original/betriebsstellen_open_data.json")
    let dbdata = Serializer.Deserialize<BetriebsstelleRailwayRoutePosition[]>(dbtext)
    let positions = dbdata|>Array.filter (fun p -> p.STRECKE_NR=routenr && bfStelleArt|>Array.contains p.STELLE_ART) 
    positions

let comparetitle  title showDetails =
    let templates = loadTemplatesForWikiTitle title
    let precodedStations = templates |> Array.map findPrecodedStation |> Array.choose id
    let strecken = findBsDatenStreckenNr templates
    if strecken.Length = 0 then () //todo: fprintfn stdout "no routenumbers found: %s" title
    else strecken
        |> Array.iter (fun kvstrecke -> 
            let positions = loadRailwayRoutePositions kvstrecke.Key
            compare title kvstrecke (strecken.Length>1) precodedStations positions showDetails)

[<EntryPoint>]
let main argv =
    Serializer.addConverters ([| |])
    match argv with
    | [| "-comparetitle"; title |] -> comparetitle title false
    | [| "-verbose"; "-comparetitle"; title |] -> comparetitle title true
    | [| "-generateIndex" |] -> generateIndex()
    | [| "-showResults"; path |] -> showResults(path)
    | _ -> fprintfn stderr "usage: [-verbose] -comparetitle title | -generateIndex | -showResults path"   
    0
