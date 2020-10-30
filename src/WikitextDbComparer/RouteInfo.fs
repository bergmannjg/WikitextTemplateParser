/// determine RouteInfo from STRECKENNR template
module RouteInfo

open Ast
open System.Text.RegularExpressions
open FSharp.Collections

let tagsInRoutename =
    [| "<small>("
       "<small> ("
       "<small>"
       ")</small>"
       "</small>" |]

let ignoreStrings (v: string) =
    Array.concat [| tagsInRoutename
                    AdhocReplacements.ignoreStringsInRoutename |]
    |> Array.fold (fun (s: string) v -> s.Replace(v, "")) v

type RoutenameKind =
    | Empty
    | SmallFormat
    | Parenthesis
    | Text
    | Unmatched

type RouteInfo =
    { nummer: int
      title: string
      von: string
      bis: string
      routenameKind: RoutenameKind
      searchstring: string }

let createStrecke (nummer: int)
                  (title: string)
                  (von: string)
                  (bis: string)
                  (routenameKind: RoutenameKind)
                  (searchstring: string)
                  =
    { nummer = nummer
      title = title
      von = von
      bis = bis
      routenameKind = routenameKind
      searchstring = searchstring }

let printRouteInfo showDetails (routeInfo: RouteInfo) =
    if (showDetails)
    then printfn "%A" routeInfo
    else printfn "%s" (Serializer.Serialize<RouteInfo>(routeInfo))

let showRouteInfoResults (path: string) =
    if System.IO.File.Exists path then
        let text = System.IO.File.ReadAllText path

        let results =
            Serializer.Deserialize<RouteInfo []>(text)

        printfn "distinct routes count: %d" (results |> Array.countBy (fun r -> r.nummer)).Length
        printfn
            "route name empty: %d"
            (results
             |> Array.filter (fun r -> r.routenameKind = RoutenameKind.Empty)).Length
        printfn
            "route names in small format: %d"
            (results
             |> Array.filter (fun r -> r.routenameKind = RoutenameKind.SmallFormat)).Length
        printfn
            "route names in small parenthesis: %d"
            (results
             |> Array.filter (fun r -> r.routenameKind = RoutenameKind.Parenthesis)).Length
        printfn
            "route names in text: %d"
            (results
             |> Array.filter (fun r -> r.routenameKind = RoutenameKind.Text)).Length
        printfn
            "route names not matched: %d"
            (results
             |> Array.filter (fun r -> r.routenameKind = RoutenameKind.Unmatched)).Length
    else
        fprintfn stderr "file not found: %s" path

let matchRegexwithValue pattern
                        value
                        von
                        bis
                        (routenameKind: RoutenameKind)
                        (title: string)
                        (strecken: ResizeArray<RouteInfo>)
                        =
    let regex = Regex(pattern)
    let mc = regex.Matches value
    for m in mc do
        if m.Groups.Count = 2
        then strecken.Add(createStrecke (m.Groups.[1].Value |> int) title von bis routenameKind value)
    mc.Count > 0

let figureDash = "–" // FIGURE DASH U+2012
let hyphenMinus = "-" // HYPHEN-MINUS U+002D
let minusSign = "−" // MINUS SIGN U+2212

let separators = [| figureDash; hyphenMinus; minusSign |]

/// the standard is to separate names with FIGURE DASH char
let spitstring (s: string) =
    let picked =
        separators
        |> Array.tryPick (fun sep ->
            let splits = s.Split sep
            if splits.Length > 1 then Some splits else None)

    match picked with
    | Some ar -> ar
    | _ -> Array.empty

let addRoute (nrValue: string)
             (namenValue: string)
             (searchstring: string)
             von
             bis
             (routenameKind: RoutenameKind)
             (title: string)
             (strecken: ResizeArray<RouteInfo>)
             =
    let nr = nrValue |> int

    let namen = ignoreStrings namenValue

    let x = spitstring namen

    match ((spitstring namen) |> Array.map trim) with
    | [| von0; bis0 |] when not (System.String.IsNullOrEmpty bis0) ->
        strecken.Add(createStrecke nr title von0 bis0 routenameKind searchstring)
    | [| von0; _; bis0 |] when not (System.String.IsNullOrEmpty bis0) ->
        strecken.Add(createStrecke nr title von0 bis0 routenameKind searchstring)
    | [| von0; _; bis0; _ |] when not (System.String.IsNullOrEmpty bis0) ->
        strecken.Add(createStrecke nr title von0 bis0 routenameKind searchstring)
    | _ ->
        if System.String.IsNullOrEmpty(namen.Trim())
        then strecken.Add(createStrecke nr title von bis Empty searchstring)
        else strecken.Add(createStrecke nr title "" "" Unmatched searchstring)

let matchRegexwithSubroutes pattern
                            value
                            von
                            bis
                            (routenameKind: RoutenameKind)
                            (title: string)
                            (strecken: ResizeArray<RouteInfo>)
                            =
    let regex = Regex(pattern) // subroutes

    let mc = regex.Matches value
    for m in mc do
        if m.Groups.Count = 3
           && not (m.Groups.[2].Value.Contains("parallel")) then
            addRoute m.Groups.[1].Value m.Groups.[2].Value value von bis routenameKind title strecken
    mc.Count > 0

let matchRegexwithValueAndSubroutes pattern
                                    value
                                    von
                                    bis
                                    (routenameKind: RoutenameKind)
                                    (title: string)
                                    (strecken: ResizeArray<RouteInfo>)
                                    =
    let regex = Regex(pattern) // subroutes

    let mc = regex.Matches value
    for m in mc do
        if m.Groups.Count = 4
           && not (m.Groups.[2].Value.Contains("parallel")) then // todo: generalize
            let nr0 = m.Groups.[1].Value |> int
            strecken.Add(createStrecke nr0 title von bis RoutenameKind.Empty value)
            addRoute m.Groups.[2].Value m.Groups.[3].Value value von bis routenameKind title strecken

    mc.Count > 0

let matchRegexwithValueAndSubcaptures pattern
                                      value
                                      von
                                      bis
                                      (routenameKind: RoutenameKind)
                                      (title: string)
                                      (strecken: ResizeArray<RouteInfo>)
                                      =
    let regex = Regex(pattern) // subroutes

    let mc = regex.Matches value
    for m in mc do
        if m.Groups.Count = 5
           && not (m.Groups.[2].Value.Contains("parallel")) then // todo: generalize
            let nr0 = m.Groups.[1].Value |> int
            strecken.Add(createStrecke nr0 title von bis RoutenameKind.Empty value)

            Seq.iter2 (fun (c1: Capture) (c2: Capture) ->
                addRoute c1.Value c2.Value value von bis routenameKind title strecken) m.Groups.[3].Captures
                m.Groups.[4].Captures

    mc.Count > 0

let strContainsNumber (s: string) = s |> Seq.exists System.Char.IsDigit

let findBsDatenStreckenNr (templates: Template []) title =
    let (von, bis) =
        match findTemplateParameterString templates "BS-header" "" with
        | Some value ->
            match (value.Split "–" |> Array.map trim) with
            | [| von; bis |] -> (von, bis)
            | _ -> ("", "")
        | Option.None -> ("", "")

    match findTemplateParameterString templates "BS-daten" "STRECKENNR" with
    | Some value when strContainsNumber value ->
        let strecken = ResizeArray<RouteInfo>()

        // adhoc
        let valueX =
            value.Replace("<br />", " ").Replace("<br>", " ").Replace("</span>", " ")

        let regexX = Regex(@"<span[^>]*>")
        let value0 = regexX.Replace(valueX, "")

        if not
            ((matchRegexwithValue @"^[']*([0-9]+)[']*$" value0 von bis RoutenameKind.Empty title strecken)
             || (matchRegexwithValue @"^DB ([0-9]+)" value0 von bis RoutenameKind.Empty title strecken)
             || (matchRegexwithValueAndSubroutes
                     @"([0-9]+)\s+([0-9]+)[^<]*(<small>.+?</small>)"
                     value0
                     von
                     bis
                     RoutenameKind.SmallFormat
                     title
                     strecken)
             || (matchRegexwithValue @"^([0-9]+)\s*[;,']" value0 von bis RoutenameKind.Empty title strecken)
             || (matchRegexwithValue @"^([0-9]+)\s+[0-9/]" value0 von bis RoutenameKind.Empty title strecken)
             || (matchRegexwithSubroutes
                     @"([0-9]+)[^<]*(<small>[^0-9].+?</small>)"
                     value0
                     von
                     bis
                     RoutenameKind.SmallFormat
                     title
                     strecken)
             || (matchRegexwithValueAndSubcaptures
                     @"([0-9][0-9][0-9][0-9])\s*<small>(\s*([0-9][0-9][0-9][0-9])((?<=[0-9]).+?))+</small>"
                     value0
                     von
                     bis
                     RoutenameKind.Text
                     title
                     strecken)
             || (matchRegexwithSubroutes
                     @"([0-9]+)[^\(]*\(([^\)]+)\)"
                     value0
                     von
                     bis
                     RoutenameKind.Parenthesis
                     title
                     strecken)
             || (matchRegexwithSubroutes @"([0-9]+)[^A-Z]*([^0-9]+)" value0 von bis RoutenameKind.Text title strecken)) then
            fprintfn stderr "%s, findBsDatenStreckenNr failed, '%s'" title value0
        Some(strecken.ToArray())
    | _ -> Option.None
