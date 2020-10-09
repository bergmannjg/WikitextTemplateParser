
open Ast
open AstUtils
open DbData

type Result =
    | Success of Bahnhof*BetriebsstelleRailwayRoutePosition
    | Failure of BetriebsstelleRailwayRoutePosition

let loadTemplatesForWikiTitle (title:string) =
    let path = if title.StartsWith("./wikidata/") then title else "./wikidata/" + title + ".json"
    printfn "loading %s" path
    if System.IO.File.Exists path then
        let wikitext = System.IO.File.ReadAllText path
        let templates = Serializer.Deserialize<Template[]>(wikitext)
        templates
    else
        fprintfn stderr "file not found: %s" path
        Array.empty

let generateIndex () =
    let files = System.IO.Directory.EnumerateFiles "./wikidata/" |> Seq.filter (fun f -> f.EndsWith ".json" && not (f.EndsWith "index.json"))
    let mutable map = Map.empty<string,string>
    for file in files do 
        let templates = loadTemplatesForWikiTitle file
        let strecken = findBsDatenStreckenNr templates
        let s = strecken |> Array.map (fun strecke -> strecke.ToString()) |> String.concat "+"
        let info = System.IO.FileInfo file
        map <- map.Add(info.Name.Replace(".json", ""), s)
    System.IO.File.WriteAllText("./wikidata/index.json" ,Serializer.Serialize<Map<string,string>>(map))

let loadIndex () =
    let text = System.IO.File.ReadAllText ("./wikidata/index.json")
    Serializer.Deserialize<Map<string,string>>(text)
    
let routeHasSingleTitle (map:Map<string,string>) (route:int) =
    let mutable found = false
    for entry in map do
        if entry.Value = route.ToString() then found <- true
    found
    
let loadBahnhöfeFromTemplates templates (fromTo:string[]) =
    findBahnhöfe templates fromTo

let bfStelleArt = [|"Bf";"Bft";"Hp"|]

let loadRailwayRoutePositions routenr =
    let dbtext = System.IO.File.ReadAllText ("./dbdata/original/betriebsstellen_open_data.json")
    let dbdata = Serializer.Deserialize<BetriebsstelleRailwayRoutePosition[]>(dbtext)
    let positions = dbdata|>Array.filter (fun p -> p.STRECKE_NR=routenr && bfStelleArt|>Array.contains p.STELLE_ART) 
    positions

let matchBahnhofName (wikiName: string) (dbName:string) =
    let wikiName0 = wikiName.Replace("Hbf", "").Replace("Pbf", "")
    let dbName0 = dbName.Replace("Hbf", "").Replace("Pbf", "")
    let m = wikiName0 = dbName0 || dbName0.StartsWith wikiName0 || dbName0.EndsWith wikiName0 
    // printfn "match %s %s %A" wikiName0 dbName0 m
    m

let matchBahnhof (wikiBahnhof: Bahnhof) (position: BetriebsstelleRailwayRoutePosition) =
    let dbkm = getKMI2Float position.KM_I
    (abs(dbkm-wikiBahnhof.km)<1.0) && matchBahnhofName wikiBahnhof.name  position.BEZEICHNUNG

let findBahnhof (wikiBahnhöfe: Bahnhof[]) (position: BetriebsstelleRailwayRoutePosition) =
    let res = wikiBahnhöfe |> Array.filter (fun b -> matchBahnhof b position)  
    if res.Length = 0 then Failure(position)
    else Success(res.[0], position)

let compare (strecke:int) (wikiBahnhöfe: Bahnhof[]) (dbRoutePositions:BetriebsstelleRailwayRoutePosition[]) =
    printfn "compare route %d, wikidata stops %d, dbdata stop %d " strecke wikiBahnhöfe.Length  dbRoutePositions.Length
    // printfn "%A" wikiBahnhöfe
    let results = dbRoutePositions |> Array.map (fun p -> findBahnhof wikiBahnhöfe p)
    results |> Array.iter (fun result -> match result with | Failure p -> printfn "*** failed to find Bahnhof for position %s %s" p.BEZEICHNUNG p.KM_L | _ -> ())

[<EntryPoint>]
let main argv =
    Serializer.addConverters ([| |])
    match argv with
    | [| "-comparetitle"; title |] ->
        let map = loadIndex()
        let templates = loadTemplatesForWikiTitle title
        let strecken = findBsDatenStreckenNr templates
        if strecken.Length = 0 then fprintfn stderr "no routenumbers found"
        else strecken
            |> Array.iter (fun kvstrecke -> 
                let positions = loadRailwayRoutePositions kvstrecke.Key
                if strecken.Length > 1 && routeHasSingleTitle map kvstrecke.Key then printfn "route %d is subroute, comparison ignored" kvstrecke.Key
                else compare kvstrecke.Key (loadBahnhöfeFromTemplates templates kvstrecke.Value) positions)
    | [| "-generateIndex" |] -> generateIndex()
    | _ -> fprintfn stderr "args expected failed: arg %A" argv    
    0
