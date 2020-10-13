module Ast

// Ast for wikitext templates, see https://www.mediawiki.org/wiki/Help:Templates

type Link = string * string

and Composite =
    | String of string
    | Link of Link
    | Template of Template

and Parameter =
    | Empty
    | String of string * string
    | Composite of string * Composite list

and Template = string * Parameter list

type Templates = Template list
