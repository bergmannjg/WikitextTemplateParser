
open FParsec
open Wikidata
open System.Text.RegularExpressions
open Ast

// prepare template string, todo: add to parser
let prepare (s0: string) (title:string) =
    let regex0 = Regex(@"<ref[^>]*>.+?</ref>")
    let s1 = regex0.Replace(s0, "")
    let regex1 = Regex(@"<ref[^/]*/>")
    let s2 = regex1.Replace(s1, "")
    let regex2 = Regex(@"<!--.*?-->")
    let s3 = regex2.Replace(s2, "")
    AdhocReplacements.replacements 
    |> Array.fold (fun (s:string) (t,oldV,newV) -> if t = title then s.Replace(oldV,newV) else s) s3

/// load Vorlage:Bahnstrecke, ex. template 'Bahnstrecke Köln–Troisdorf' in title 'Siegstrecke'
let loadBahnstreckeTemplate ((name,_,_):Template) (parseTemplates: (string -> unit)) =
    let name = "Vorlage:" + name
    loadTemplatesOfRoutes false [|name|]
    parseTemplates name

let checkBahnstreckeTemplate (templates:Templates) (parseTemplates: (string -> unit)) =
    templates|>List.iter (fun t-> match t with | (s,_,_) when s.StartsWith "Bahnstrecke" -> loadBahnstreckeTemplate t parseTemplates      | _ -> ())

let rec parseTemplatesOfType (maybeCheck: (Templates -> (string -> unit) -> unit) option) (query: string -> list<string>) (insert: string -> string -> System.Guid) title =
    fprintfn stdout "parseTemplates: %s" title 
    match (query title |> List.tryHead) with
    | Some text -> 
        match Parser.parse text with
        | Success (result, _, _) -> 
            fprintfn stdout "Success: %s, templates Length %d" title result.Length
            insert title (Serializer.Serialize<Templates>(result)) |> ignore
            match maybeCheck with
            | Some check -> check result (parseTemplatesOfType None query insert)
            | None -> ()
        | Failure (errorMsg, _, _) -> fprintfn stderr "\n***Parser failure: %s" errorMsg
    | None -> ()

let parseTemplatesOfRoute title =
    parseTemplatesOfType (Some checkBahnstreckeTemplate) DataAccess.Wikitext.query DataAccess.Templates.insert title

let parseTemplatesOfRoutes () =
    DataAccess.Wikitext.queryKeys()
    |> List.iter parseTemplatesOfRoute

let parseTemplatesOfStop title =
    parseTemplatesOfType None DataAccess.WikitextOfStop.query DataAccess.TemplatesOfStop.insert title

let parseTemplatesOfStops () =
    DataAccess.WikitextOfStop.queryKeys()
    |> List.iter parseTemplatesOfStop

let loadTemplatesOfRoutesFromFile filename =
    System.IO.File.ReadAllLines filename
    |> loadTemplatesOfRoutes false

let loadTemplatesOfStopsFromFile filename  =
    System.IO.File.ReadAllLines filename
    |> loadTemplatesOfStops false

[<EntryPoint>]
let main argv =
    Serializer.addConverters ([| |])
    match argv with
    | [| "-loadroute"; route |] ->
        loadTemplatesOfRoutes true [|route|]  
    | [| "-loadroutes"; filename |] ->
        loadTemplatesOfRoutesFromFile filename
    | [| "-parseroute"; route |] -> 
        parseTemplatesOfRoute route  
    | [| "-parseroutes" |] -> 
        parseTemplatesOfRoutes ()
    | [| "-loadstop"; stop |] ->
        loadTemplatesOfStops true [|stop|]  
    | [| "-loadstops"; filename |] ->
        loadTemplatesOfStopsFromFile filename
    | [| "-parsestop"; stop |] -> 
        parseTemplatesOfStop stop  
    | [| "-parsestops" |] -> 
        parseTemplatesOfStops ()
    | [| "-showtitles" |] ->
        getWikipediaArticles 10000 
        |> Array.iter (fun t -> printfn "%s" t)
    | [| "-showstations" |] ->
        getWikipediaStations 10000 
        |> Array.iter (fun t -> printfn "%A" t)
    | _ -> fprintfn stdout "usage: [-reload] -parsetitle title|-parsestop stop"
    
    0
