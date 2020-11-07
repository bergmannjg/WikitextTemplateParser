/// Ast for route diagram wikitext templates,
/// see https://de.wikipedia.org/wiki/Wikipedia:Formatvorlage_Bahnstrecke
/// see https://www.mediawiki.org/wiki/Help:Templates
module Ast

type Link = string * string

and Composite =
    | String of string
    | Link of Link
    | Template of Template

and Parameter =
    | Empty
    | String of string * string
    | Composite of string * Composite list

and FunctionParameter =
    | Empty
    | String of string

and Template = string * FunctionParameter list * Parameter list

type Templates = Template list


let trim (s: string) = s.Trim()

let concatCompositeStrings (cl: list<Composite>) =
    cl
    |> List.fold (fun s e ->
        let c =
            match e with
            | Composite.String (str) -> str
            | Composite.Link (_, str) -> str
            | _ -> ""

        s + c) ""

let findTemplateParameterString (templates: Template []) (templateName: string) (parameterName: string) =
    templates
    |> Array.map (fun t ->
        match t with
        | (n, _, l) when templateName = n -> Some l
        | _ -> None)
    |> Array.choose id
    |> Array.collect List.toArray
    |> Array.map (fun p ->
        match p with
        | Parameter.String (n, v) when ("DE-" + parameterName = n) || parameterName = n -> Some v
        | Composite (n, v) when ("DE-" + parameterName = n)
                                || parameterName = n
                                && v.Length > 0 -> Some(concatCompositeStrings v)
        | _ -> None)
    |> Array.choose id
    |> Array.tryExactlyOne
    |> Option.bind (fun s -> if System.String.IsNullOrEmpty s then None else Some s)

let textOfLink (link: Link) =
    match link with
    | (t, n) -> if not (System.String.IsNullOrEmpty(n)) then n.Trim() else t.Trim()

let getFirstLinkInList (cl: list<Composite>) =
    cl
    |> List.tryFind (fun e ->
        match e with
        | Link (_) -> true
        | _ -> false)
    |> Option.bind (fun c ->
        match c with
        | Composite.Link link -> Some link
        | _ -> None)

let getParameterStrings (pl: list<Parameter>) =
    pl
    |> List.filter (fun e ->
        match e with
        | Parameter.String (_, str) -> not (System.String.IsNullOrEmpty str)
        | _ -> false)

let getCompositeStrings (cl: list<Composite>) =
    cl
    |> List.filter (fun e ->
        match e with
        | Composite.String (_) -> true
        | _ -> false)

let getFirstStringValue (p: Parameter) =
    match p with
    | Parameter.String (_, str) -> Some str
    | Parameter.Composite (_, cl) ->
        cl
        |> List.tryFind (fun e ->
            match e with
            | Composite.String (_) -> true
            | _ -> false)
        |> Option.bind (fun c ->
            match c with
            | Composite.String (str) -> Some str
            | _ -> None)
    | _ -> None
