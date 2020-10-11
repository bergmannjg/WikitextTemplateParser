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

// skip text between links
let link =
    str "[["
    >>. pid1
    .>>. ((str "|" >>. pid1LinkTitle .>> str "]]")
          <|> (str "]]" >>. preturn ""))
    .>> pid0
    |>> Composite.Link

let simpletemplate, simpletemplateRef = createParserForwardedToRef ()

let pcomposite = (attempt link) <|> simpletemplate

let pnamed =
    ws
    >>. pnamedId
    .>> ws
    .>> str "="
    .>> ws
    .>>. pid0
    |>> Parameter.String

let pnamedpcomposites =
    pipe3 (ws >>. pnamedId .>> ws .>> str "=") (pid0) (many1 pcomposite) (fun x y z ->
        if System.String.IsNullOrEmpty y then Composite(x, z) else Composite(x, Composite.String(y) :: z))

let panonpcomposites =
    pid0
    .>>. many1 pcomposite
    |>> (fun (y, z) ->
        if System.String.IsNullOrEmpty y then Composite("", z) else Composite("", Composite.String(y) :: z))

let panonstring =
    pid1 |>> (fun s -> Parameter.String("", s))

let parameter =
    (attempt pnamedpcomposites)
    <|> (attempt pnamed)
    <|> (attempt panonpcomposites)
    <|> panonstring
    <|> preturn Empty

let commontemplate =
    ws
    >>. str "{{"
    >>. pid1
    .>>. ((str "|" >>. sepBy1 parameter (str "|"))
          <|> preturn [])
    .>> str "}}"

do simpletemplateRef
   := ws
   >>. str "{{"
   >>. pid1
   .>>. ((str "|" >>. sepBy1 parameter (str "|"))
         <|> preturn [])
   .>> str "}}"
   .>> pid0
   |>> Composite.Template

let template = commontemplate |>> Template

let templates = many template |>> Templates

let parse str = run templates str
