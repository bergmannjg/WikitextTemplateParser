module DataAccess

open System
open Database

let dbname = @"results.db"

let private collectionInsert (collection: string) (map: Map<string, string>) (value: string) =
    use db = new Database(dbname)
    db.InsertUnique collection (Guid.NewGuid()) map value

let private collectionDelete (collection: string) (map: Map<string, string>) =
    use db = new Database(dbname)
    db.Query collection map
    |> Seq.toList
    |> List.map (fun r -> db.Delete collection r.id)
    |> List.forall ((=) true)

let private collectionQuery (collection: string) (map: Map<string, string>) =
    use db = new Database(dbname)
    db.Query collection map
    |> Seq.map (fun r -> r.value)
    |> Seq.toList

let private toJsonArray (results: list<string>) =
    if results.Length = 0 then
        "[]"
    else
        let text =
            results
            |> Seq.reduce (fun text texts -> text + "," + texts)

        "[" + text + "]"

module Wikitext =
    let private toMap (title: string) = Map.empty.Add("title", title)

    let insert title value =
        collectionInsert "Wikitext" (toMap title) value

    let query title = collectionQuery "Wikitext" (toMap title)

module Templates =
    let private toMap (title: string) = Map.empty.Add("title", title)

    let insert title value =
        collectionInsert "Templates" (toMap title) value

    let query title =
        collectionQuery "Templates" (toMap title)

module DbStationOfRoute =
    let private toMap (title: string) (route: int) =
        Map.empty.Add("title", title).Add("route", route.ToString())

    let insert title route value =
        collectionInsert "DbStationOfRoute" (toMap title route) value

    let query title route =
        collectionQuery "DbStationOfRoute" (toMap title route)

module WkStationOfInfobox =
    let private toMap (title: string) = Map.empty.Add("title", title)

    let insert title value =
        collectionInsert "WkStationOfInfobox" (toMap title) value

    let query title =
        collectionQuery "WkStationOfInfobox" (toMap title)

module DbWkStationOfRoute =
    let private toMap (title: string) (route: int) =
        Map.empty.Add("title", title).Add("route", route.ToString())

    let insert title route value =
        collectionInsert "DbWkStationOfRoute" (toMap title route) value

    let query title route =
        collectionQuery "DbWkStationOfRoute" (toMap title route)

module WkStationOfRoute =
    let private toMap (title: string) (route: int) =
        Map.empty.Add("title", title).Add("route", route.ToString())

    let insert title route value =
        collectionInsert "WkStationOfRoute" (toMap title route) value

    let query title route =
        collectionQuery "WkStationOfRoute" (toMap title route)

module RouteInfo =
    let private toMap (title: string) (route: int) =
        Map.empty.Add("title", title).Add("route", route.ToString())

    let insert title route value =
        collectionInsert "RouteInfo" (toMap title route) value

    let delete title =
        collectionDelete "RouteInfo" (Map.empty.Add("title", title))

    let query title route =
        collectionQuery "RouteInfo" (toMap title route)

    let queryAll () =
        toJsonArray (collectionQuery "RouteInfo" Map.empty)

module ResultOfRoute =
    let private toMap (title: string) (route: int) =
        Map.empty.Add("title", title).Add("route", route.ToString())

    let insert title route value =
        collectionInsert "ResultOfRoute" (toMap title route) value

    let delete title =
        collectionDelete "ResultOfRoute" (Map.empty.Add("title", title))

    let query title route =
        collectionQuery "ResultOfRoute" (toMap title route)

    let queryAll () =
        toJsonArray (collectionQuery "ResultOfRoute" Map.empty)
