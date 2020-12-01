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

let sameSubstring (s0: string) (s1: string) checkchars =
    s0.Length
    >= checkchars
    && s1.Length >= checkchars
    && s0.Substring(0, checkchars) = s1.Substring(0, checkchars)


let regexMatchedValues (regex: Regex) (input: string) =
    let m = regex.Match(input)
    if m.Success then
        m.Groups
        |> Seq.skip 1
        |> Seq.map (fun g -> g.Value)
        |> Seq.toList
    else
        List.empty

/// see http://www.fssnip.net/bj/title/Levenshtein-distance
let levenshtein (word1: string) (word2: string) =
    let preprocess =
        fun (str: string) -> str.ToLower().ToCharArray()

    let chars1, chars2 = preprocess word1, preprocess word2
    let m, n = chars1.Length, chars2.Length
    let table: int [,] = Array2D.zeroCreate (m + 1) (n + 1)
    for i in 0 .. m do
        for j in 0 .. n do
            match i, j with
            | i, 0 -> table.[i, j] <- i
            | 0, j -> table.[i, j] <- j
            | _, _ ->
                let delete = table.[i - 1, j] + 1
                let insert = table.[i, j - 1] + 1
                //cost of substitution is 2
                let substitute =
                    if chars1.[i - 1] = chars2.[j - 1] then
                        table.[i - 1, j - 1] //same character
                    else
                        table.[i - 1, j - 1] + 2

                table.[i, j] <- List.min [ delete; insert; substitute ]
    table.[m, n] //return distance
