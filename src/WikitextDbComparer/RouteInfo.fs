/// determine RouteInfo from STRECKENNR template
module RouteInfo

open Ast
open System.Text.RegularExpressions
open FSharp.Collections

let private tagsInRoutename =
    [| "<small>("
       "<small> ("
       "<small>"
       ")</small>"
       "</small>" |]

let private ignoreStrings (v: string) =
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
      railwayGuide: string option
      routenameKind: RoutenameKind
      searchstring: string }

let createRouteInfo (nummer: int)
                  (title: string)
                  (von: string)
                  (bis: string)
                  (hasRailwayGuide: string option)
                  (routenameKind: RoutenameKind)
                  (searchstring: string)
                  =
    { nummer = nummer
      title = title
      von = von
      bis = bis
      railwayGuide = hasRailwayGuide
      routenameKind = routenameKind
      searchstring = searchstring }

let printRouteInfo showDetails (routeInfo: RouteInfo) =
    if (showDetails) then printfn "%A" routeInfo

    DataAccess.RouteInfo.insert routeInfo.title routeInfo.nummer (Serializer.Serialize<RouteInfo>(routeInfo))
    |> ignore

let showRouteInfoResults () =
    let results =
        Serializer.Deserialize<RouteInfo []>(DataAccess.RouteInfo.queryAll ())

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

let figureDash = "–" // FIGURE DASH U+2012
let hyphenMinus = "-" // HYPHEN-MINUS U+002D
let minusSign = "−" // MINUS SIGN U+2212
let leftRightArrow = "↔" // MINUS SIGN U+2194

let private separators =
    [| leftRightArrow
       figureDash
       hyphenMinus
       minusSign |]

/// the standard is to separate names with FIGURE DASH char
let private spitstring (s: string) =
    let picked =
        separators
        |> Array.tryPick (fun sep ->
            let splits = s.Split sep
            if splits.Length > 1 then Some splits else None)

    match picked with
    | Some ar -> ar
    | _ -> Array.empty

let (|SplitRouteNames|_|) names =
    match (spitstring names) |> Array.map trim with
    | [| von0; bis0 |] when not (System.String.IsNullOrEmpty bis0) -> Some(von0, bis0)
    | [| von0; _; bis0 |] when not (System.String.IsNullOrEmpty bis0) -> Some(von0, bis0)
    | [| von0; _; bis0; _ |] when not (System.String.IsNullOrEmpty bis0) -> Some(von0, bis0)
    | _ -> None

let private addRoute (nr: int)
             (namenValue: string)
             (searchstring: string)
             von
             bis
             (hasRailwayGuide: string option)
             (routenameKind: RoutenameKind)
             (title: string)
             (routes: ResizeArray<RouteInfo>)
             =
    let namen = ignoreStrings namenValue

    match namen with
    | SplitRouteNames (von, bis) ->
        routes.Add(createRouteInfo nr title von bis hasRailwayGuide routenameKind searchstring)
    | _ ->
        if System.String.IsNullOrEmpty(namen.Trim())
        then routes.Add(createRouteInfo nr title von bis hasRailwayGuide Empty searchstring)
        else routes.Add(createRouteInfo nr title "" "" hasRailwayGuide Unmatched searchstring)

let (|RegexWithValue|_|) pattern value =
    let regex = Regex(pattern)
    let mc = regex.Matches value
    if mc.Count > 0 then
        let l =
            [ for m in mc do
                if m.Groups.Count = 2 then yield (m.Groups.[1].Value |> int) ]

        if l.Length > 0 then Some l else None
    else
        None

let (|RegexWithSubroutes|_|) pattern value =
    let regex = Regex(pattern)
    let mc = regex.Matches value
    if mc.Count > 0 then
        let l =
            [ for m in mc do
                if m.Groups.Count = 3
                then yield (m.Groups.[1].Value |> int, m.Groups.[2].Value) ]

        if l.Length > 0 then Some l else None
    else
        None

let (|RegexWithValueAndSubroutes|_|) pattern value =
    let regex = Regex(pattern)
    let mc = regex.Matches value
    if mc.Count > 0 then
        let l =
            [ for m in mc do
                if m.Groups.Count = 4
                then yield (m.Groups.[1].Value |> int, m.Groups.[2].Value |> int, m.Groups.[3].Value) ]

        if l.Length > 0 then Some l else None
    else
        None

let (|RegexWithValueAndSubcapture|_|) pattern value =
    let regex = Regex(pattern)
    let mc = regex.Matches value
    if mc.Count > 0 then
        let l =
            [ for m in mc do
                if m.Groups.Count = 3
                then yield (m.Groups.[1].Value |> int, Seq.map (fun (c1: Capture) -> c1.Value) m.Groups.[2].Captures) ]

        if l.Length > 0 then Some l else None
    else
        None

let (|RegexWithValueAndSubcaptures|_|) pattern value =
    let regex = Regex(pattern)
    let mc = regex.Matches value
    if mc.Count > 0 then
        let l =
            [ for m in mc do
                if m.Groups.Count = 5 then
                    yield
                        (m.Groups.[1].Value |> int,
                         Seq.map2 (fun (c1: Capture) (c2: Capture) -> (c1.Value |> int, c2.Value)) m.Groups.[3].Captures
                             m.Groups.[4].Captures) ]

        if l.Length > 0 then Some l else None
    else
        None

let strContainsNumber (s: string) = s |> Seq.exists System.Char.IsDigit

let normalizeRailwayGuide (value: string) = value.Replace("'", "")

let findRouteInfoInTemplates (templates: Template []) title =
    let (von, bis) =
        match findTemplateParameterString templates "BS-header" "" with
        | Some value ->
            match (value.Split "–" |> Array.map trim) with
            | [| von; bis |] -> (von, bis)
            | _ -> ("", "")
        | Option.None -> ("", "")

    let railwayGuide =
        match findTemplateParameterString templates "BS-daten" "KBS" with
        | Some value when not (System.String.IsNullOrEmpty value) -> Some(normalizeRailwayGuide value)
        | _ -> None

    let rmHtmlTags (value: string) =
        let value0 =
            value.Replace("<br />", " ").Replace("<br>", " ").Replace("</span>", " ")

        Regex(@"<span[^>]*>").Replace(value0, "")

    match findTemplateParameterString templates "BS-daten" "STRECKENNR" with
    | Some value when strContainsNumber value ->
        let routeInfos = ResizeArray<RouteInfo>()
        let value0 = rmHtmlTags value
        match value0 with
        | RegexWithValue @"^[']*([0-9]+)[']*$" routes ->
            routes
            |> List.iter (fun r -> routeInfos.Add(createRouteInfo r title von bis railwayGuide RoutenameKind.Empty value0))
        | RegexWithValue @"^DB ([0-9]+)" routes ->
            routes
            |> List.iter (fun r -> routeInfos.Add(createRouteInfo r title von bis railwayGuide RoutenameKind.Empty value0))
        | RegexWithValueAndSubroutes @"([0-9]+)\s+([0-9]+)[^<]*(<small>.+?</small>)" routes ->
            routes
            |> List.iter (fun (nr0, nr1, namen) ->
                routeInfos.Add(createRouteInfo nr0 title von bis railwayGuide RoutenameKind.Empty value0)
                addRoute nr1 namen value0 von bis railwayGuide RoutenameKind.SmallFormat title routeInfos)
        | RegexWithValueAndSubcapture @"^([0-9]+)(,\s*[0-9]+)+" result ->
            result
            |> List.iter (fun (nr0, routes) ->
                routeInfos.Add(createRouteInfo nr0 title von bis railwayGuide RoutenameKind.Empty value)

                Seq.iter (fun (nr: string) ->
                    let nr0 = nr.Replace(",", "") |> int
                    addRoute nr0 "" value von bis railwayGuide RoutenameKind.SmallFormat title routeInfos) routes)
        | RegexWithValue @"^([0-9]+)\s*[;,']" routes ->
            routes
            |> List.iter (fun r -> routeInfos.Add(createRouteInfo r title von bis railwayGuide RoutenameKind.Empty value0))
        | RegexWithValue @"^([0-9]+)\s+[0-9/]" routes ->
            routes
            |> List.iter (fun r -> routeInfos.Add(createRouteInfo r title von bis railwayGuide RoutenameKind.Empty value0))
        | RegexWithSubroutes @"([0-9]+)[^<]*(<small>[^0-9].+?</small>)" routes ->
            routes
            |> List.iter (fun (nr0, namen) ->
                addRoute nr0 namen value0 von bis railwayGuide RoutenameKind.SmallFormat title routeInfos)
        | RegexWithValueAndSubcaptures @"([0-9][0-9][0-9][0-9])\s*<small>(\s*([0-9][0-9][0-9][0-9])((?<=[0-9]).+?))+</small>"
                                       result ->
            result
            |> List.iter (fun (nr0, routes) ->
                routeInfos.Add(createRouteInfo nr0 title von bis railwayGuide RoutenameKind.Empty value)
                Seq.iter (fun (nr: int, namen: string) ->
                    addRoute nr namen value0 von bis railwayGuide RoutenameKind.Text title routeInfos) routes)
        | RegexWithSubroutes @"([0-9]+)[^\(]*\(([^\)]+)\)" routes ->
            routes
            |> List.iter (fun (nr, namen) ->
                addRoute nr namen value0 von bis railwayGuide RoutenameKind.Parenthesis title routeInfos)
        | RegexWithSubroutes @"([0-9]+)[^A-Z]*([^0-9]+)" routes ->
            routes
            |> List.iter (fun (nr, namen) ->
                addRoute nr namen value0 von bis railwayGuide RoutenameKind.Text title routeInfos)
        | _ -> fprintfn stderr "%s, findBsDatenStreckenNr failed, '%s'" title value0

        Some(routeInfos.ToArray())
    | _ -> Option.None
