module Ast

// Ast for wikitext Route diagram templates,
// see https://www.mediawiki.org/wiki/Help:Templates
// see https://de.wikipedia.org/wiki/Wikipedia:Formatvorlage_Bahnstrecke

type Link = string * string

type SimpleTemplate = string * Parameter list

and Composite =
    | Link of Link
    | Template of SimpleTemplate

and Parameter =
    | Empty
    | String of string * string
    | Composite of string * Composite list

and Template = Template of string * Parameter list

type Templates = Templates of Template list
