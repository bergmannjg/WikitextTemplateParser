
open FParsec
open Wikidata
open System.Text.RegularExpressions
open Ast

// prepare template string, todo: add to parser
let prepare (s: string) =
    let regex0 = Regex(@"<ref[^>]*>.*</ref>")
    let s1 = regex0.Replace(s, "")
    let regex1 = Regex(@"<!--.*?-->")
    regex1.Replace(s1, "")

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
