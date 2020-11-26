module Wikidata

open Ast

let loadTemplates (title: string) =
    fprintfn stderr "loading: '%s'" title

    match DataAccess.Templates.query title |> List.tryHead with
    | Some row -> Serializer.Deserialize<list<Template>>(row)
    | None ->
        fprintfn stderr "loadTemplates: title not found: %s" title
        list.Empty

let substituteTemplateParameter (fpl: list<FunctionParameter>) (tpl: list<Parameter>) =
    match fpl, tpl with
    | (FunctionParameter.String ("1") :: _, Parameter.String (_, hd) :: _) -> hd /// todo
    | _ ->
        fprintfn stderr "unexpected template parameter: %A" fpl
        ""

let evalIfeqInTemplate (n: string) (fpl: list<FunctionParameter>) (pl: list<Parameter>) (tpl: list<Parameter>) =
    match pl with
    | Parameter.String (_, s2) :: Parameter.String (_, s3) :: Parameter.String (_, s4) :: _ ->
        let v = substituteTemplateParameter fpl tpl
        if v = s2 then Composite.String(s2) else Composite.String(s4) /// todo
    | Parameter.String (_, s2) :: Parameter.String (_, s3) :: _ ->
        let v = substituteTemplateParameter fpl tpl
        if v = s2 then Composite.String(s2) else Composite.String("") /// todo
    | _ ->
        fprintfn stderr "unexpected ifeq parameters: %A" pl
        Composite.Template(n, List.empty, pl)

let evalSwitchInTemplate (n: string) (fpl: list<FunctionParameter>) (pl: list<Parameter>) (tpl: list<Parameter>) =
    match pl with
    | Parameter.String (sw1, v1) :: Parameter.String (sw2, v2) :: _ -> /// todo
        let v = substituteTemplateParameter fpl tpl
        if v = sw1 then Composite.String(v1)
        else if v = sw2 then Composite.String(v2) /// todo
        else Composite.String("")
    | _ ->
        fprintfn stderr "unexpected switch parameters: %A" pl
        Composite.Template(n, List.empty, pl)

let evalFunctionInComposites (composites: list<Composite>) (tpl: list<Parameter>) =
    List.foldBack (fun (c: Composite) st ->
        let newc =
            match c with
            | Composite.Template (n, fpl, pl) when n.StartsWith "#ifeq" -> evalIfeqInTemplate n fpl pl tpl
            | Composite.Template (n, fpl, pl) when n.StartsWith "#switch" -> evalSwitchInTemplate n fpl pl tpl
            | _ -> c

        newc :: st) composites List.empty

let evalFunctionInParameters (parameters: list<Parameter>) (tpl: list<Parameter>) =
    List.foldBack (fun (p: Parameter) st ->
        let newp =
            match p with
            | Parameter.Composite (s, cl) -> Parameter.Composite(s, evalFunctionInComposites cl tpl)
            | _ -> p

        p :: st) parameters List.empty

let evalFunctionInTemplates (templates: list<Template>) (tpl: list<Parameter>): list<Template> =
    List.foldBack (fun ((n, _, pl): Template) st ->
        let newt =
            (n, List.empty, evalFunctionInParameters pl tpl)

        newt :: st) templates List.empty

let evalTemplate (title: string) (tpl: list<Parameter>) =
    let templates =
        loadTemplates ("Vorlage:" + title)

    evalFunctionInTemplates templates tpl

let evalTemplates (templates: list<Template>) =
    List.foldBack (fun (t: Template) st ->
        match t with
        | (n, _, pl) when n.StartsWith "Bahnstrecke" ->
            let tl = evalTemplate n pl
            tl @ st
        | _ -> t :: st) templates List.empty

let loadTemplatesForWikiTitle (title: string) showDetails =
    let templates = loadTemplates title
    evalTemplates templates |> List.toArray
