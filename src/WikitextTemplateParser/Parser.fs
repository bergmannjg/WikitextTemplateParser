module Parser

open FParsec
open Ast

let trim (s: string) = s.Trim()

let ws = spaces // skips any whitespace

let str s = skipString s >>. ws

let parseEndOfString =
    skipChar '|'
    <|> (skipChar '{' >>. skipChar '{')
    <|> (skipChar '}' >>. skipChar '}')
    <|> (skipChar '[' >>. skipChar '[')
    <|> (skipChar ']' >>. skipChar ']')
    <|> skipChar '\n'

let parseEndOfNamedIdString = parseEndOfString <|> skipChar '='

let pid0 =
    many ((notFollowedBy parseEndOfString) >>. anyChar)
    .>> ws
    |>> (Array.ofList >> System.String.Concat >> trim)

let pid1 =
    many1 ((notFollowedBy parseEndOfString) >>. anyChar)
    .>> ws
    |>> (Array.ofList >> System.String.Concat >> trim)

let pnamedId =
    many1
        ((notFollowedBy parseEndOfNamedIdString)
         >>. anyChar)
    .>> ws
    |>> (Array.ofList >> System.String.Concat >> trim)

// ignore templates in link title
let pid1LinkTitle =
    many1
        ((notFollowedBy (pchar ']' >>. pchar ']'))
         >>. anyChar)
    .>> ws
    |>> (Array.ofList >> System.String.Concat >> trim)

let link =
    str "[["
    >>. pid1
    .>>. ((str "|" >>. pid1LinkTitle .>> str "]]")
          <|> (str "]]" >>. preturn ""))
    |>> Composite.Link

let compositestring = pid1 .>> ws |>> Composite.String

let compositetemplate, compositetemplateRef = createParserForwardedToRef ()

let pcomposite =
    (attempt link)
    <|> (attempt compositestring)
    <|> compositetemplate

let pnamed =
    ws
    >>. pnamedId
    .>> ws
    .>> str "="
    .>> ws
    .>>. pid0
    |>> Parameter.String

let makecomposite (x: string) (y: string) (z: list<Composite>) =
    if System.String.IsNullOrEmpty y then Composite(x, z) else Composite(x, Composite.String(y) :: z)

let pnamedpcomposites =
    pipe3 (ws >>. pnamedId .>> ws .>> str "=") (pid0) (many1 pcomposite) makecomposite

let panonpcomposites =
    pid0
    .>>. many1 pcomposite
    |>> (fun (y, z) -> makecomposite "" y z)

let panonstring =
    pid1 |>> (fun s -> Parameter.String("", s))

let parameter =
    (attempt pnamedpcomposites)
    <|> (attempt pnamed)
    <|> (attempt panonpcomposites)
    <|> panonstring
    <|> preturn Parameter.Empty

let functionparameter =
    pid1
    |>> (fun s -> FunctionParameter.String(s))
    <|> preturn FunctionParameter.Empty

let functionname =
    many1 ((notFollowedBy (str ":")) >>. anyChar)
    .>> ws
    |>> (Array.ofList >> System.String.Concat >> trim)

let parserfunction =
    ws
    >>. str "{{"
    >>. str "#"
    >>. functionname
    .>> str ":"
    .>> str "{{{"
    .>>. sepBy1 functionparameter (str "|")
    .>> str "}}}"
    .>>. ((str "|" >>. sepBy1 parameter (str "|"))
          <|> preturn [])
    .>> str "}}"
    |>> (fun ((fn, pl1), pl2) -> ("#" + fn, pl1, pl2))

let template =
    ws
    >>. str "{{"
    >>. pid1
    .>>. ((str "|" >>. sepBy1 parameter (str "|"))
          <|> preturn [])
    .>> str "}}"
    |>> (fun (n, pl) -> (n, List.empty, pl))

do compositetemplateRef
   := (attempt parserfunction)
   <|> template
   |>> Composite.Template

let templates = many template

let parse str = run templates str
