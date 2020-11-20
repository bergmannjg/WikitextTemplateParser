module DataAccess

open System
open Database

let dbname = @"results.db"

let dropCollection (collectionName: string) =
    use db = new Database(dbname)
    db.DropCollection collectionName

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
    let collection = "Wikitext"

    let insert title value =
        collectionInsert collection (toMap title) value

    let query title = collectionQuery collection (toMap title)

module Templates =
    let private toMap (title: string) = Map.empty.Add("title", title)
    let collection = "Templates"

    let insert title value =
        collectionInsert collection (toMap title) value

    let query title = collectionQuery collection (toMap title)

module DbStationOfRoute =
    let private toMap (title: string) (route: int) =
        Map.empty.Add("title", title).Add("route", route.ToString())

    let collection = "DbStationOfRoute"

    let insert title route value =
        collectionInsert collection (toMap title route) value

    let query title route =
        collectionQuery collection (toMap title route)

module WkStationOfInfobox =
    let private toMap (title: string) = Map.empty.Add("title", title)
    let collection = "WkStationOfInfobox"

    let insert title value =
        collectionInsert collection (toMap title) value

    let query title = collectionQuery collection (toMap title)

module DbWkStationOfRoute =
    let private toMap (title: string) (route: int) =
        Map.empty.Add("title", title).Add("route", route.ToString())

    let collection = "DbWkStationOfRoute"

    let insert title route value =
        collectionInsert collection (toMap title route) value

    let query title route =
        collectionQuery collection (toMap title route)

module WkStationOfRoute =
    let private toMap (title: string) (route: int) =
        Map.empty.Add("title", title).Add("route", route.ToString())

    let collection = "WkStationOfRoute"

    let insert title route value =
        collectionInsert collection (toMap title route) value

    let query title route =
        collectionQuery "WkStationOfRoute" (toMap title route)

module RouteInfo =
    let private toMap (title: string) (route: int) =
        Map.empty.Add("title", title).Add("route", route.ToString())

    let collection = "RouteInfo"

    let insert title route value =
        collectionInsert collection (toMap title route) value

    let delete title =
        collectionDelete collection (Map.empty.Add("title", title))

    let query title route =
        collectionQuery collection (toMap title route)

    let queryAll () =
        toJsonArray (collectionQuery collection Map.empty)

module ResultOfRoute =
    let private toMap (title: string) (route: int) =
        Map.empty.Add("title", title).Add("route", route.ToString())

    let collection = "ResultOfRoute"

    let insert title route value =
        collectionInsert collection (toMap title route) value

    let delete title =
        collectionDelete collection (Map.empty.Add("title", title))

    let query title route =
        collectionQuery collection (toMap title route)

    let queryAll () =
        toJsonArray (collectionQuery collection Map.empty)
