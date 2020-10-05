module Parser

open FParsec
open Ast

let ws = spaces // skips any whitespace

let str s = pstring s >>. ws

let parseEndOfString =
    pchar '|'
    <|> (pchar '{' >>. pchar '{')
    <|> (pchar '}' >>. pchar '}')
    <|> (pchar '[' >>. pchar '[')
    <|> (pchar ']' >>. pchar ']')
    <|> pchar '\n'

let pid0 =
    many ((notFollowedBy parseEndOfString) >>. anyChar)
    .>> ws
    |>> (Array.ofList >> System.String.Concat)

let pid1 =
    many1 ((notFollowedBy parseEndOfString) >>. anyChar)
    .>> ws
    |>> (Array.ofList >> System.String.Concat)

// ignore templates in link title
let pid1LinkTitle =
    many1 ((notFollowedBy  (pchar ']' >>. pchar ']')) >>. anyChar)
    .>> ws
    |>> (Array.ofList >> System.String.Concat)

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
    >>. pid1
    .>> str "="
    .>>. pid0
    |>> Parameter.String

let pnamedpcomposites =
    pipe3 (ws >>. pid1 .>> str "=") (pid0) (many1 pcomposite) (fun x y z -> Composite(x, z))

let panonpcomposites =
    pid0
    >>. many1 pcomposite
    |>> (fun l -> Parameter.Composite("", l))

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
