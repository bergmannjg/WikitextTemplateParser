module AstUtils

open Ast
open System.Text.RegularExpressions
open FSharp.Collections

let trim (s: string) = s.Trim()

let findTemplateParameterString (templates: Template []) (templateName: string) (parameterName: string) =
    templates
    |> Array.map (fun t ->
        match t with
        | (n, l) when templateName = n -> Some l
        | _ -> None)
    |> Array.choose id
    |> Array.collect List.toArray
    |> Array.map (fun p ->
        match p with
        | String (n, v) when parameterName = n || ("DE-" + parameterName) = n -> Some v
        | Composite (n, v) when (parameterName = n || ("DE-" + parameterName) = n)
                                && v.Length > 0 ->
            match v with
            | Composite.String (s) :: _ -> Some s
            | _ -> None
        | _ -> None)
    |> Array.choose id
    |> Array.tryExactlyOne

let findBsDatenStreckenNr (templates: Template []) =
    let fromTo =
        match findTemplateParameterString templates "BS-header" "" with
        | Some value -> value.Split "–" |> Array.map trim
        | None -> Array.empty

    match findTemplateParameterString templates "BS-daten" "STRECKENNR" with
    | Some value ->
        let strecken =
            ResizeArray<System.Collections.Generic.KeyValuePair<int, string []>>()

        // adhoc
        let value0 =
            value.Replace("<small>", " ").Replace("</small>", " ").Replace("<br />", " ").Replace("(", " ").Replace(")", " ")

        let regex0 = Regex(@"^([0-9]+)$")
        let mc0 = regex0.Matches value0
        for m in mc0 do
            if m.Groups.Count = 2
            then strecken.Add(System.Collections.Generic.KeyValuePair.Create(m.Groups.[1].Value |> int, fromTo))
        if mc0.Count = 0 then
            let regex1 = Regex(@"^([0-9]+)[\<,]")
            let mc1 = regex1.Matches value0
            for m in mc1 do
                if m.Groups.Count = 2
                then strecken.Add(System.Collections.Generic.KeyValuePair.Create(m.Groups.[1].Value |> int, fromTo))
            if mc1.Count = 0 then
                let regex2 = Regex(@"([0-9]+)([^0-9)]+)") // subroutes
                let mc2 = regex2.Matches value0
                for m in mc2 do
                    if m.Groups.Count = 3
                       && not (m.Groups.[2].Value.Contains("parallel"))
                       && not (m.Groups.[2].Value.Contains("Gleis"))
                       && not (m.Groups.[2].Value.Contains("S-Bahn")) then // todo: generalize
                        let names =
                            m.Groups.[2].Value.Split "–" |> Array.map trim

                        strecken.Add
                            (System.Collections.Generic.KeyValuePair.Create
                                (m.Groups.[1].Value |> int, (if names.Length = 2 then names else fromTo)))
        strecken.ToArray()
    | _ -> Array.empty

let convertfloattxt (km: string) =
    let regex0 = Regex(@"^([0-9\.]+)")

    let m =
        regex0.Match(km.Replace(",", ".").Replace("(", "").Replace(")", ""))

    if m.Success && m.Groups.Count = 2 then m.Groups.[1].Value else "-1.0"

let parse2float (km: string) =
    let f =
        double (System.Single.Parse(convertfloattxt km))

    System.Math.Round(f, 1)

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
