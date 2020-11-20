/// determine RouteInfo from STRECKENNR template
module RouteInfo

open Ast
open System.Text.RegularExpressions
open FSharp.Collections

type RoutenameKind =
    | Empty
    | EmptyWithIgnored
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
      routenameKind = if nummer >= 1000 then routenameKind else Unmatched
      searchstring = searchstring }

let printRouteInfo showDetails (routeInfo: RouteInfo) =
    if (showDetails) then printfn "%A" routeInfo

    DataAccess.RouteInfo.insert routeInfo.title routeInfo.nummer (Serializer.Serialize<RouteInfo>(routeInfo))
    |> ignore

let lengthOfKind kind results =
    (results
     |> Array.filter (fun r -> r.routenameKind = kind)).Length

let showRouteInfoResults () =
    let results =
        Serializer.Deserialize<RouteInfo []>(DataAccess.RouteInfo.queryAll ())

    printfn "distinct routes count: %d" (results |> Array.countBy (fun r -> r.nummer)).Length
    printfn "route name empty: %d" (lengthOfKind Empty results)
    printfn "route name emptyWithIgnored: %d" (lengthOfKind EmptyWithIgnored results)
    printfn "route names in small format: %d" (lengthOfKind SmallFormat results)
    printfn "route names in small parenthesis: %d" (lengthOfKind Parenthesis results)
    printfn "route names in text: %d" (lengthOfKind Text results)
    printfn "route names not matched: %d" (lengthOfKind Unmatched results)

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

let private tagsInRoutename = [| "<small>"; "</small>" |]

let private ignoreStrings (s0: string) =
    let s1 =
        Array.concat [| tagsInRoutename
                        AdhocReplacements.ignoreStringsInRoutename |]
        |> Array.fold (fun (s: string) v -> s.Replace(v, "")) s0

    s1.Trim([| ' '; ','; ':'; '('; ')'; ''' |])

let isEmpytRouteName (name: string) =
    System.String.IsNullOrEmpty(name) || name = ","

let isEmpytIgnoredRouteName (name: string) =
    (AdhocReplacements.prefixesOfEmptyRouteNames
     |> Array.exists (fun s -> name.StartsWith(s)))

let private makeRouteÎnfo (nr: int)
                      (namenValue: string)
                      (searchstring: string)
                      (von: string)
                      (bis: string)
                      (hasRailwayGuide: string option)
                      (title: string)
                      =
    let routenameKind =
        if namenValue.Contains "<small>" then RoutenameKind.SmallFormat else RoutenameKind.Text

    let namen = (ignoreStrings namenValue).Trim()

    match namen with
    | SplitRouteNames (von, bis) -> createRouteInfo nr title von bis hasRailwayGuide routenameKind searchstring
    | _ ->
        if isEmpytRouteName (namen) then
            createRouteInfo nr title von bis hasRailwayGuide Empty searchstring
        else if isEmpytIgnoredRouteName (namen) then
            createRouteInfo nr title von bis hasRailwayGuide EmptyWithIgnored searchstring
        else
            createRouteInfo nr title "" "" hasRailwayGuide Unmatched searchstring

let (|GroupsOfRegex|_|) pattern value =
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

let private strContainsNumber (s: string) = s |> Seq.exists System.Char.IsDigit

let private normalizeRailwayGuide (value: string) = value.Replace("'", "")

let private findBsHeaderInTemplates (templates: Template []) =
    match findTemplateParameterString templates "BS-header" "" with
    | Some value ->
        match (value.Split "–" |> Array.map trim) with
        | [| von; bis |] -> (von, bis)
        | _ -> ("", "")
    | Option.None -> ("", "")

let private rmHtmlTags (value: string) =
    let value0 =
        value.Replace("<br />", " ").Replace("<br/>", " ").Replace("<br>", " ").Replace("</span>", " ")

    Regex(@"<span[^>]*>").Replace(value0, "")

let findRouteInfoInTemplates (templates: Template []) title =
    let (von, bis) = findBsHeaderInTemplates templates

    let railwayGuide =
        match findTemplateParameterString templates "BS-daten" "KBS" with
        | Some value when not (System.String.IsNullOrEmpty value) -> Some(normalizeRailwayGuide value)
        | _ -> None

    match findTemplateParameterString templates "BS-daten" "STRECKENNR" with
    | Some value when strContainsNumber value ->
        let value0 = rmHtmlTags value
        match value0 with
        | GroupsOfRegex @"(\d{4})(\D+\d\D+|\D*)" routes ->
            Some
                (routes
                 |> List.map (fun (nr, namen) -> makeRouteÎnfo nr namen value0 von bis railwayGuide title)
                 |> List.toArray)
        | _ ->
            fprintfn stderr "%s, findRouteInfoInTemplates failed, '%s'" title value0
            Some(Array.empty)
    | _ -> Option.None
