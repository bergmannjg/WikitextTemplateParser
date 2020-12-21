module DataAccess

open System
open Database
open Templates
open Types

let mutable private dbname = @"results.db"

let setDbname name = dbname <- name

let dropCollection collectionName =
    use db = new Database(dbname)
    db.DropCollection collectionName

let private collectionInsert (collection: string) (map: Map<string, string>) (value: string) =
    use db = new Database(dbname)
    db.InsertUnique collection (Guid.NewGuid()) map value

let private typedCollectionInsert<'a> (collection: string) (map: Map<string, string>) (value: 'a) =
    use db = new Database(dbname)
    db.InsertUnique collection (Guid.NewGuid()) map  (Serializer.Serialize<'a>(value))

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

let private typedCollectionQuery<'a> (collection: string) (map: Map<string, string>) =
    use db = new Database(dbname)
    db.Query collection map
    |> Seq.map (fun r -> Serializer.Deserialize<'a>(r.value))
    |> Seq.toList

let private collectionQueryKeys (collection: string) (map: Map<string, string>) =
    use db = new Database(dbname)
    db.Query collection map
    |> Seq.map (fun r -> r.keyValues)
    |> Seq.toList

let private toJsonArray (results: list<string>) =
    if results.Length = 0 then
        "[]"
    else
        let text =
            results
            |> Seq.reduce (fun text texts -> text + "," + texts)

        "[" + text + "]"

let private toJson (key: string) (value: string) = sprintf "{\"%s\":\"%s\"}" key value

module WikitextOfRoute =
    let private toMap (title: string) = Map.empty.Add("title", title)
    let collection = "Wikitext"

    let insert title value =
        collectionInsert collection (toMap title) value

    let query title =
        collectionQuery collection (toMap title)

    let queryKeys () =
        collectionQueryKeys collection Map.empty
        |> List.map (fun k -> k.["title"])

module WikitextOfStop =
    let private toMap (title: string) = Map.empty.Add("title", title)
    let collection = "WikitextOfStop"

    let insert title value =
        collectionInsert collection (toMap title) value

    let query title = collectionQuery collection (toMap title)

    let queryKeysAsJson () =
        toJsonArray
            (collectionQueryKeys collection Map.empty
             |> List.map (fun k -> toJson "title" k.["title"]))

    let queryKeys () =
        collectionQueryKeys collection Map.empty
        |> List.map (fun k -> k.["title"])

module TemplatesOfRoute =
    let private toMap (title: string) = Map.empty.Add("title", title)
    let collection = "Templates"

    let insert title value =
        typedCollectionInsert<Templates> collection (toMap title) value

    let query title =
        typedCollectionQuery<Templates> collection (toMap title)

    let queryAsStrings title = collectionQuery collection (toMap title)

    let queryKeys () =
        collectionQueryKeys collection Map.empty
        |> List.map (fun k -> k.["title"])

module TemplatesOfStop =
    let private toMap (title: string) = Map.empty.Add("title", title)
    let collection = "TemplatesOfStop"

    let insert title value =
        typedCollectionInsert<Templates> collection (toMap title) value

    let query title =
        typedCollectionQuery<Templates> collection (toMap title)

module DbOpPointOfRoute =
    let private toMap (title: string) (route: int) =
        Map.empty.Add("title", title).Add("route", route.ToString())

    let collection = "DbStationOfRoute"

    let insert title route value =
        typedCollectionInsert<DbOpPointOfRoute []> collection (toMap title route) value

    let query title route =
        typedCollectionQuery<DbOpPointOfRoute []> collection (toMap title route)

    let queryAsStrings title route =
        collectionQuery collection (toMap title route)

module WkOpPointOfInfobox =
    let private toMap (title: string) = Map.empty.Add("title", title)
    let collection = "WkStationOfInfobox"

    let insert title value =
        typedCollectionInsert<OpPointOfInfobox []> collection (toMap title) value

    let query title =
        typedCollectionQuery<OpPointOfInfobox []> collection (toMap title)

    let queryKeys () =
        collectionQueryKeys collection Map.empty
        |> List.map (fun k -> k.["title"])

    let queryAsStrings title = collectionQuery collection (toMap title)

module DbWkOpPointOfRoute =
    let private toMap (title: string) (route: int) =
        Map.empty.Add("title", title).Add("route", route.ToString())

    let collection = "DbWkStationOfRoute"

    let insert title route value =
        typedCollectionInsert<list<OpPointOfDbWk>> collection (toMap title route) value

    let query title route =
        typedCollectionQuery<list<OpPointOfDbWk>> collection (toMap title route)

    let querysAsStrings title route =
        collectionQuery collection (toMap title route)

module WkOpPointOfRoute =
    let private toMap (title: string) (route: int) =
        Map.empty.Add("title", title).Add("route", route.ToString())

    let collection = "WkStationOfRoute"

    let insert title route value =
        typedCollectionInsert<WkOpPointOfRoute []> collection (toMap title route) value

    let query title route =
        typedCollectionQuery<WkOpPointOfRoute []> "WkStationOfRoute" (toMap title route)

    let queryAsStrings title route =
        collectionQuery "WkStationOfRoute" (toMap title route)

module RouteInfo =
    let private toMap (title: string) (route: int) =
        Map.empty.Add("title", title).Add("route", route.ToString())

    let collection = "RouteInfo"

    let insert title route value =
        typedCollectionInsert<RouteInfo> collection (toMap title route) value

    let delete title =
        collectionDelete collection (Map.empty.Add("title", title))

    let query title route =
        typedCollectionQuery<RouteInfo> collection (toMap title route)

    let queryAll () =
        toJsonArray (collectionQuery collection Map.empty)

module ResultOfRoute =
    let private toMap (title: string) (route: int) =
        Map.empty.Add("title", title).Add("route", route.ToString())

    let collection = "ResultOfRoute"

    let insert title route value =
        typedCollectionInsert<ResultOfRoute> collection (toMap title route) value

    let delete title =
        collectionDelete collection (Map.empty.Add("title", title))

    let query title route =
        typedCollectionQuery<ResultOfRoute> collection (toMap title route)

    let queryAll () =
        toJsonArray (collectionQuery collection Map.empty)
