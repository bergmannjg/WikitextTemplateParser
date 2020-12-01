
open FParsec
open Wikidata
open Ast

/// prepare template string, todo: add to parser
let prepare (s: string) (title:string) =
    s 
    |> StringUtilities.replaceFromRegexToEmpty AdhocReplacements.regexRef
    |> StringUtilities.replaceFromRegexToEmpty AdhocReplacements.regexRefSelfClosed
    |> StringUtilities.replaceFromRegexToEmpty AdhocReplacements.regexComment
    |> StringUtilities.replaceFromList AdhocReplacements.replacements (fun t -> t = title)

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
        |> Seq.iter (fun t -> printfn "%s" t)
    | [| "-showstations" |] ->
        getWikipediaStations 10000 
        |> Seq.iter (fun t -> printfn "%A" t)
    | _ -> fprintfn stdout "usage: [-reload] -parsetitle title|-parsestop stop"
    
    0
