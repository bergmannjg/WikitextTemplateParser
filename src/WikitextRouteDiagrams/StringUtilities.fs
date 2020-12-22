[<RequireQualifiedAccess>]
module StringUtilities

open System.Text.RegularExpressions

let replaceWithBlank (c: char) (s: string) = s.Replace(c, ' ')

let trimChars (chars: char []) (s: string) = s.Trim(chars)

let trim (s: string) = s.Trim()

let replaceFromListToString (strings: list<string>) (v: string) (s: string) =
    strings
    |> List.fold (fun (x: string) y -> x.Replace(y, v).Trim()) s

let replaceFromListToEmpty (strings: list<string>) (s: string) = replaceFromListToString strings "" s

let replaceFromList (replacements: (string * string * string) []) (choose: string -> bool) (s0: string) =
    replacements
    |> Array.fold (fun (s: string) (t, oldV, newV) -> if choose t then s.Replace(oldV, newV) else s) s0

let replaceFromRegexToString (regex: Regex) (v: string) (s: string) = regex.Replace(s, v).Trim()

let replaceFromRegexToEmpty (regex: Regex) = replaceFromRegexToString regex ""

let removeSubstring (fromStr: string) (toStr: string) (s: string) =
    let refFrom = s.IndexOf(fromStr)
    let refto = s.IndexOf(toStr)
    if refFrom > 0 && refto > refFrom then
        s.Substring(0, refFrom)
        + s.Substring(refto + toStr.Length)
    else
        s

let startsWithSameSubstring (s0: string) (s1: string) checkchars =
    s0.Length
    >= checkchars
    && s1.Length >= checkchars
    && s0.Substring(0, checkchars) = s1.Substring(0, checkchars)

let containsSubstring (s0: string) (s1: string) (substring: string) =
    s0.ToLower().Contains substring
    && s1.ToLower().Contains substring

let regexMatchedValues (regex: Regex) (input: string) =
    let m = regex.Match(input)
    if m.Success then
        m.Groups
        |> Seq.skip 1
        |> Seq.map (fun g -> g.Value)
        |> Seq.toList
    else
        List.empty
