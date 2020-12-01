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

let private concatCompositeStrings (cl: seq<Composite>) =
    cl
    |> Seq.fold (fun s e ->
        let c =
            match e with
            | Composite.String (str) -> str
            | Composite.Link (_, str) -> str
            | _ -> ""

        s + c) ""

let private findCompositeTemplate (cl: seq<Composite>) =
    cl
    |> Seq.map (fun e ->
        match e with
        | Composite.Template (t) -> Some t
        | _ -> None)
    |> Seq.choose id

let findTemplateParameterString (templates: seq<Template>) (templateName: string) (parameterName: string) =
    templates
    |> Seq.map (fun t ->
        match t with
        | (n, _, l) when templateName = n -> Some l
        | _ -> None)
    |> Seq.choose id
    |> Seq.collect id
    |> Seq.map (fun p ->
        match p with
        | Parameter.String (n, v) when ("DE-" + parameterName = n) || parameterName = n -> Some v
        | Composite (n, v) when ("DE-" + parameterName = n)
                                || parameterName = n
                                && v.Length > 0 -> Some(concatCompositeStrings v)
        | _ -> None)
    |> Seq.choose id
    |> Seq.tryExactlyOne
    |> Option.bind (fun s -> if System.String.IsNullOrEmpty s then None else Some s)

let findTemplateParameterTemplates (templates: seq<Template>) (templateName: string) (parameterName: string) =
    templates
    |> Seq.map (fun t ->
        match t with
        | (n, _, l) when templateName = n -> Some l
        | _ -> None)
    |> Seq.choose id
    |> Seq.collect id
    |> Seq.map (fun p ->
        match p with
        | Composite (n, v) when ("DE-" + parameterName = n)
                                || parameterName = n
                                && v.Length > 0 -> Some(findCompositeTemplate v)
        | _ -> None)
    |> Seq.choose id
    |> Seq.collect id

let textOfLink (link: Link) =
    match link with
    | (t, n) -> if not (System.String.IsNullOrEmpty(n)) then n.Trim() else t.Trim()

let linktextOfLink (link: Link) =
    match link with
    | (t, _) -> t.Trim()

let getFirstLinkInList (cl: seq<Composite>) =
    cl
    |> Seq.tryFind (fun e ->
        match e with
        | Link (_) -> true
        | _ -> false)
    |> Option.bind (fun c ->
        match c with
        | Composite.Link link -> Some link
        | _ -> None)

let getParameterStrings (pl: seq<Parameter>) =
    pl
    |> Seq.filter (fun e ->
        match e with
        | Parameter.String (_, str) -> not (System.String.IsNullOrEmpty str)
        | _ -> false)

let existsParameterStringNameInList (strings: seq<string>)
                                    (chooser: string -> string -> bool)
                                    (parameters: seq<Parameter>)
                                    =
    parameters
    |> Seq.exists (fun t ->
        match t with
        | Parameter.String (n, _) when strings |> Seq.exists (chooser n) -> true
        | _ -> false)

let existsParameterStringValueInList (strings: seq<string>)
                                     (chooser: string -> string -> bool)
                                     (parameters: seq<Parameter>)
                                     =
    parameters
    |> Seq.exists (fun t ->
        match t with
        | Parameter.String (_, v) when strings |> Seq.exists (chooser v) -> true
        | _ -> false)

let getCompositeStrings (cl: seq<Composite>) =
    cl
    |> Seq.filter (fun e ->
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

let findLinks (markers: string []) (templates: seq<Template>) =
    [ for (_, _, parameters) in templates do
        let mutable foundMarker = false
        for p in parameters do
            match p with
            | Parameter.String (_, v) when not foundMarker -> foundMarker <- (markers |> Array.exists v.Contains)
            | Composite (_, composites) ->
                for c in composites do
                    match c with
                    | Link (link) -> if foundMarker then yield link
                    | _ -> ()
            | _ -> () ]
