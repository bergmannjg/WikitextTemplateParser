
open FParsec
open Wikidata
open System.Text.RegularExpressions
open Ast

// prepare template string, todo: add to parser
let prepare (s0: string) =
    let regex0 = Regex(@"<ref[^>]*>.+?</ref>")
    let s1 = regex0.Replace(s0, "")
    let regex1 = Regex(@"<ref[^/]*/>")
    let s2 = regex1.Replace(s1, "")
    let regex2 = Regex(@"<!--.*?-->")
    let s3 = regex2.Replace(s2, "")
    s3.Replace("{{BS2||", "{{BS2|") // error in 'Berliner_Nordbahn'

let parseTemplatesForWikiTitle title =
    match loadTemplatesCached title with
    | Some t ->
        match Parser.parse (prepare t) with
        | Success (result, _, _) -> 
            fprintfn stdout "Success"
            System.IO.File.WriteAllText ("./wikidata/" + title + ".json", Serializer.Serialize<Templates>(result))
            System.IO.File.WriteAllText ("./wikidata/" + title + ".txt", sprintf "%A" result)
        | Failure (errorMsg, _, _) -> fprintfn stdout "\n***Parser failure: %s" errorMsg
    | None -> 
        fprintfn stdout "\n***no templates found, title %s" title

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
