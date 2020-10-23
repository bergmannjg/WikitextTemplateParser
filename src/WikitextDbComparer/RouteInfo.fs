/// determine RouteInfo from STRECKENNR template
module RouteInfo

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

type RouteInfo =
    { nummer: int
      von: string
      bis: string }

let createStrecke (nummer: int) (von: string) (bis: string) =
    { nummer = nummer
      von = von
      bis = bis }

let matchRegexwithValue pattern value von bis (strecken: ResizeArray<RouteInfo>) =
    let regex = Regex(pattern)
    let mc = regex.Matches value
    for m in mc do
        if m.Groups.Count = 2
        then strecken.Add(createStrecke (m.Groups.[1].Value |> int) von bis)
    mc.Count > 0

let matchRegexwithSubroutes pattern value von bis (strecken: ResizeArray<RouteInfo>) =
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
        let strecken = ResizeArray<RouteInfo>()

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

