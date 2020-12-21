/// load and parse templates of routes and stops
module ParserProcessing

open Templates
open Wikidata

/// prepare template string, todo: add to parser
let private prepare (s: string) (title: string) =
    s
    |> StringUtilities.replaceFromRegexToEmpty AdhocReplacements.regexRef
    |> StringUtilities.replaceFromRegexToEmpty AdhocReplacements.regexRefSelfClosed
    |> StringUtilities.replaceFromRegexToEmpty AdhocReplacements.regexComment
    |> StringUtilities.replaceFromList AdhocReplacements.Wikitext.replacements (fun t -> t = title)

let loadAndParseTemplate title (parseTemplates: (string -> unit)) =
    loadTemplatesOfRoutes false [| title |]
    parseTemplates title

/// load Vorlage:Bahnstrecke, ex. template 'Bahnstrecke Köln–Troisdorf' in title 'Siegstrecke'
let checkBahnstreckeTemplate (templates: Templates) (parseTemplates: (string -> unit)) =
    templates
    |> List.iter (fun t ->
        match t with
        | (s, _, _) when s.StartsWith "Bahnstrecke" -> loadAndParseTemplate ("Vorlage:" + s) parseTemplates
        | _ -> ())

let rec parseTemplatesOfType (maybeCheck: (Templates -> (string -> unit) -> unit) option)
                             (query: string -> list<string>)
                             (insert: string -> Templates -> System.Guid)
                             title
                             =
    fprintfn stdout "parseTemplates: %s" title
    match (query title |> List.tryHead) with
    | Some text ->
        match Parser.parse (prepare text title) with
        | FParsec.CharParsers.ParserResult.Success (result, _, _) ->
            fprintfn stdout "Success: %s, templates Length %d" title result.Length
            insert title result |> ignore
            match maybeCheck with
            | Some check -> check result (parseTemplatesOfType None query insert)
            | None -> ()
        | FParsec.CharParsers.ParserResult.Failure (errorMsg, _, _) ->
            fprintfn stderr "\n***Parser failure: %s" errorMsg
    | None -> ()

let parseTemplatesOfRoute title =
    parseTemplatesOfType
        (Some checkBahnstreckeTemplate)
        DataAccess.WikitextOfRoute.query
        DataAccess.TemplatesOfRoute.insert
        title

let parseTemplatesOfRoutes () =
    DataAccess.WikitextOfRoute.queryKeys ()
    |> List.iter parseTemplatesOfRoute

let parseTemplatesOfStop title =
    parseTemplatesOfType None DataAccess.WikitextOfStop.query DataAccess.TemplatesOfStop.insert title

let parseTemplatesOfStops () =
    DataAccess.WikitextOfStop.queryKeys ()
    |> List.iter parseTemplatesOfStop
