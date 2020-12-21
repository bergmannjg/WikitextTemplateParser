/// determine RouteInfo from STRECKENNR template
module RouteInfo

// fsharplint:disable RecordFieldNames

open Types
open Templates
open System.Text.RegularExpressions
open FSharp.Collections
open Microsoft.FSharp.Reflection

let private createRouteInfo (nummer: int)
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

    DataAccess.RouteInfo.insert routeInfo.title routeInfo.nummer routeInfo
    |> ignore

let private lengthOfKind kind results =
    (results
     |> Array.filter (fun r -> r.routenameKind = kind)).Length

let showRouteInfoResults () =
    let results =
        Serializer.Deserialize<RouteInfo []>(DataAccess.RouteInfo.queryAll ())

    printfn "distinct routes count: %d" (results |> Array.countBy (fun r -> r.nummer)).Length

    for case in FSharpType.GetUnionCases typeof<RoutenameKind> do
        let mk =
            FSharpValue.MakeUnion(case, [||]) :?> RoutenameKind

        printfn "route name empty: %s %d" case.Name (lengthOfKind mk results)

let private figureDash = "–" // FIGURE DASH U+2012
let private hyphenMinus = "-" // HYPHEN-MINUS U+002D
let private minusSign = "−" // MINUS SIGN U+2212
let private leftRightArrow = "↔" // MINUS SIGN U+2194

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

let private (|SplitRouteNames|_|) names =
    match (spitstring names)
          |> Array.map StringUtilities.trim with
    | [| von0; bis0 |] when not (System.String.IsNullOrEmpty bis0) -> Some(von0, bis0)
    | [| von0; _; bis0 |] when not (System.String.IsNullOrEmpty bis0) -> Some(von0, bis0)
    | [| von0; _; bis0; _ |] when not (System.String.IsNullOrEmpty bis0) -> Some(von0, bis0)
    | _ -> None

let private tagsInRoutename = [ "<small>"; "</small>" ]

let private replacements =
    List.concat [ tagsInRoutename
                  AdhocReplacements.RouteInfo.ignoreStringsInRoutename ]

let private applayReplacements (s: string) =
    s
    |> StringUtilities.replaceFromListToEmpty replacements
    |> StringUtilities.trimChars ([| ' '; ','; ':'; '('; ')'; ''' |])

let private isEmpytRouteName (name: string) =
    System.String.IsNullOrEmpty(name) || name = ","

let private isEmpytIgnoredRouteName (name: string) =
    AdhocReplacements.RouteInfo.prefixesOfEmptyRouteNames
    |> Array.exists (fun s -> name.StartsWith(s))

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

    let namen = (applayReplacements namenValue).Trim()

    match namen with
    | SplitRouteNames (von, bis) -> createRouteInfo nr title von bis hasRailwayGuide routenameKind searchstring
    | _ ->
        if isEmpytRouteName (namen) then
            createRouteInfo nr title von bis hasRailwayGuide RoutenameKind.Empty searchstring
        else if isEmpytIgnoredRouteName (namen) then
            createRouteInfo nr title von bis hasRailwayGuide EmptyWithIgnored searchstring
        else
            createRouteInfo nr title "" "" hasRailwayGuide Unmatched searchstring

let private regexRoutes = Regex(@"(\d{4})(\D+\d\D+|\D*)")

let private findRoutes value =
    let mc = regexRoutes.Matches value
    if mc.Count > 0 then
        let l =
            [ for m in mc do
                if m.Groups.Count = 3
                then yield (m.Groups.[1].Value |> int, m.Groups.[2].Value) ]

        if l.Length > 0 then l else List.empty
    else
        List.empty

let private strContainsNumber (s: string) = s |> Seq.exists System.Char.IsDigit

let private normalizeRailwayGuide (value: string) = value.Replace("'", "")

let private findBsHeaderInTemplates templates =
    match findTemplateParameterString templates "BS-header" "" with
    | Some value ->
        match (value.Split "–" |> Array.map StringUtilities.trim) with
        | [| von; bis |] -> (von, bis)
        | _ -> ("", "")
    | Option.None -> ("", "")

let private htmlTags =
    [ "<br />"
      "<br/>"
      "<br>"
      "</span>"
      "<!-- -->" ]

let private rmHtmlTags (value: string) =
    value
    |> StringUtilities.replaceFromListToEmpty htmlTags
    |> StringUtilities.removeSubstring "<ref" "/ref>"
    |> StringUtilities.replaceFromRegexToEmpty AdhocReplacements.regexSpanOPen
    |> StringUtilities.replaceFromRegexToEmpty AdhocReplacements.regexRefSelfClosed
    |> StringUtilities.replaceFromRegexToEmpty AdhocReplacements.regexYear
    |> StringUtilities.replaceFromRegexToEmpty AdhocReplacements.regexYearDiff

let findRouteInfoInTemplates templates title =
    let (von, bis) = findBsHeaderInTemplates templates

    let kbsTemplates =
        findTemplateParameterTemplates templates "BS-daten" "KBS"

    let railwayGuide =
        if kbsTemplates |> Seq.isEmpty then
            match findTemplateParameterString templates "BS-daten" "KBS" with
            | Some value when not (System.String.IsNullOrEmpty value) -> Some(normalizeRailwayGuide value)
            | _ -> None
        else
            match findTemplateParameterString kbsTemplates "Kursbuchlink" "Nummer" with
            | Some value when not (System.String.IsNullOrEmpty value) -> Some(normalizeRailwayGuide value)
            | _ -> None

    match findTemplateParameterString templates "BS-daten" "STRECKENNR"
          |> Option.bind (rmHtmlTags >> Option.Some) with
    | Some value when strContainsNumber value ->
        let routes = findRoutes value
        if routes.Length > 0 then
            Some
                (routes
                 |> List.map (fun (nr, namen) -> makeRouteÎnfo nr namen value von bis railwayGuide title))
        else
            fprintfn stderr "%s, findRouteInfoInTemplates failed, '%s'" title value
            Some(List.empty)
    | _ -> Option.None
