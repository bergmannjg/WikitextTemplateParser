
open Ast
open AstUtils
open Stations
open DbData
open Comparer

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

[<EntryPoint>]
let main argv =
    Serializer.addConverters ([| |])
    match argv with
    | [| "-comparetitle"; title |] ->
        let templates = loadTemplatesForWikiTitle title
        let precodedStations = templates |> Array.map findPrecodedStation |> Array.choose id
        let strecken = findBsDatenStreckenNr templates
        if strecken.Length = 0 then () //todo: fprintfn stdout "no routenumbers found: %s" title
        else strecken
            |> Array.iter (fun kvstrecke -> 
                let positions = loadRailwayRoutePositions kvstrecke.Key
                if positions.Length > 0 // todo: length = 0 
                then compare title kvstrecke (filterPrecodedStations (if strecken.Length>1 then kvstrecke.Value else Array.empty) precodedStations) positions)
    | [| "-generateIndex" |] -> generateIndex()
    | _ -> fprintfn stderr "usage: -comparetitle title"   
    0
