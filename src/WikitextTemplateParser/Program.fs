
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
    parseTemplates ("Vorlage:" + name)

let checkBahnstreckeTemplate (templates:Templates) (parseTemplates: (string -> unit)) =
    templates|>List.iter (fun t-> match t with | (s,_,_) when s.StartsWith "Bahnstrecke" -> loadBahnstreckeTemplate t parseTemplates      | _ -> ())

let rec parseTemplatesForWikiTitle title =
    match loadTemplatesCached title with
    | Some t ->
        match Parser.parse (prepare t title) with
        | Success (result, _, _) -> 
            fprintfn stdout "Success: templates Length %d" result.Length
            System.IO.File.WriteAllText ("./wikidata/" + title + ".json", Serializer.Serialize<Templates>(result))
            System.IO.File.WriteAllText ("./wikidata/" + title + ".txt", sprintf "%A" result)
            fprintfn stdout "see templates %s" ("./wikidata/" + title + ".txt")
            checkBahnstreckeTemplate result parseTemplatesForWikiTitle
        | Failure (errorMsg, _, _) -> fprintfn stderr "\n***Parser failure: %s" errorMsg
    | None -> 
        fprintfn stderr "***no templates found, title %s" title

let parseTemplatesForRailwaynr railwaynr =
    match findWikiTitle railwaynr with
    | Some title -> parseTemplatesForWikiTitle title
    | None -> fprintfn stdout "findTitle failed, railwaynr %s" railwaynr

[<EntryPoint>]
let main argv =
    Serializer.addConverters ([| |])
    match argv with
    | [| "-railwaynr"; railwaynr |] ->
        match System.Int32.TryParse railwaynr with
        | true, _ ->
            parseTemplatesForRailwaynr railwaynr
        | _, _ -> fprintfn stdout "integer expected: %s" railwaynr
    | [| "-parsetitle"; title |] ->
        parseTemplatesForWikiTitle title
    | [| "-showtitles"; strMaxTitles |] ->
        match System.Int32.TryParse strMaxTitles with
        | true, maxTitles ->
            getWikipediaArticles maxTitles 
            |> getTitles
            |> Array.choose id
            |> Array.iter (fun t -> printfn "%s" t)
        | _, _ -> fprintfn stdout "integer expected: %s" strMaxTitles
    | _ -> fprintfn stdout "args expected failed: arg %A" argv
    
    0
