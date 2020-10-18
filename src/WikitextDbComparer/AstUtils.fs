module AstUtils

open Ast
open System.Text.RegularExpressions
open FSharp.Collections

// todo
let ignoreStringsInRoutename =
    [| "<small>("
       "<small>"
       ")</small>"
       "</small>"
       "Ferngleise"
       "Südgleis"
       "äußeres Gleispaar" |]

let ignoreStrings (v: string) (strings: string []) =
    strings
    |> Array.fold (fun (s: string) v -> s.Replace(v, "")) v

type Strecke =
    { nummer: int
      von: string
      bis: string }

let createStrecke (nummer: int) (von: string) (bis: string) =
    { nummer = nummer
      von = von
      bis = bis }

let trim (s: string) = s.Trim()

let concatCompositeStrings (cl: list<Composite>) =
    cl
    |> List.fold (fun s e ->
        let c =
            match e with
            | Composite.String (str) -> str
            | _ -> ""

        s + c) ""

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
                                && v.Length > 0 -> Some(concatCompositeStrings v)
        | _ -> None)
    |> Array.choose id
    |> Array.tryExactlyOne
    |> Option.bind (fun s -> if System.String.IsNullOrEmpty s then None else Some s)

let matchRegexwithValue pattern value von bis (strecken: ResizeArray<Strecke>) =
    let regex = Regex(pattern)
    let mc = regex.Matches value
    for m in mc do
        if m.Groups.Count = 2
        then strecken.Add(createStrecke (m.Groups.[1].Value |> int) von bis)
    mc.Count > 0

let matchRegexwithSubroutes pattern value von bis (strecken: ResizeArray<Strecke>) =
    let regex = Regex(pattern) // subroutes

    let mc = regex.Matches value
    for m in mc do
        if m.Groups.Count = 3
           && not (m.Groups.[2].Value.Contains("parallel")) then // todo: generalize
            let nr = m.Groups.[1].Value |> int

            let namen =
                ignoreStringsInRoutename
                |> ignoreStrings m.Groups.[2].Value

            match (namen.Split "–" |> Array.map trim) with
            | [| von0; bis0 |] -> strecken.Add(createStrecke nr von0 bis0)
            | [| von0; _; bis0 |] -> strecken.Add(createStrecke nr von0 bis0)
            | _ -> strecken.Add(createStrecke nr von bis)
    mc.Count > 0

let strContainsNumber (s: string) = s |> Seq.exists System.Char.IsDigit

let findBsDatenStreckenNr (templates: Template []) title =
    let (von, bis) =
        match findTemplateParameterString templates "BS-header" "" with
        | Some value ->
            match (value.Split "–" |> Array.map trim) with
            | [| von; bis |] -> (von, bis)
            | _ -> ("", "")
        | None -> ("", "")

    match findTemplateParameterString templates "BS-daten" "STRECKENNR" with
    | Some value when strContainsNumber value ->
        let strecken = ResizeArray<Strecke>()

        // adhoc
        let valueX =
            value.Replace("<br />", " ").Replace("</span>", " ")

        let regexX = Regex(@"<span[^>]*>")
        let value0 = regexX.Replace(valueX, "")

        if not
            ((matchRegexwithValue @"^[']*([0-9]+)[']*$" value0 von bis strecken)
             || (matchRegexwithValue @"^([0-9]+)[;,]" value0 von bis strecken)
             || (matchRegexwithValue @"^([0-9]+)\s+[0-9/]" value0 von bis strecken)
             || (matchRegexwithValue @"^DB ([0-9]+)" value0 von bis strecken)
             || (matchRegexwithSubroutes @"([0-9]+)[^<]*(<small>.+?</small>)" value0 von bis strecken)
             || (matchRegexwithSubroutes @"([0-9]+)[^\(]*\(([^\)]+)\)" value0 von bis strecken)
             || (matchRegexwithSubroutes @"([0-9]+)[^A-Z]*([^0-9]+)" value0 von bis strecken)) then
            fprintfn stderr "%s, findBsDatenStreckenNr failed, '%s'" title value0
        Some(strecken.ToArray())
    | _ -> None

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
