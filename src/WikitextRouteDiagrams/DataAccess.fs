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

    let query title = collectionQuery collection (toMap title)

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
        collectionInsert collection (toMap title) (Serializer.Serialize<Templates>(value))

    let query title =
        collectionQuery collection (toMap title)
        |> List.map Serializer.Deserialize<Templates>

    let queryAsStrings title =
        collectionQuery collection (toMap title)

    let queryKeys () =
        collectionQueryKeys collection Map.empty
        |> List.map (fun k -> k.["title"])

module TemplatesOfStop =
    let private toMap (title: string) = Map.empty.Add("title", title)
    let collection = "TemplatesOfStop"

    let insert title value =
        collectionInsert collection (toMap title) (Serializer.Serialize<Templates>(value))

    let query title =
        collectionQuery collection (toMap title)
        |> List.map Serializer.Deserialize<Templates>

module DbStationOfRoute =
    let private toMap (title: string) (route: int) =
        Map.empty.Add("title", title).Add("route", route.ToString())

    let collection = "DbStationOfRoute"

    let insert title route value =
        collectionInsert collection (toMap title route) (Serializer.Serialize<DbStationOfRoute []>(value))

    let query title route =
        collectionQuery collection (toMap title route)
        |> List.map Serializer.Deserialize<DbStationOfRoute []>

    let queryAsStrings title route =
        collectionQuery collection (toMap title route)

module WkStationOfInfobox =
    let private toMap (title: string) = Map.empty.Add("title", title)
    let collection = "WkStationOfInfobox"

    let insert title value =
        collectionInsert collection (toMap title) (Serializer.Serialize<StationOfInfobox []>(value))

    let query title =
        collectionQuery collection (toMap title)
        |> List.map Serializer.Deserialize<StationOfInfobox []>

    let queryAsStrings title =
        collectionQuery collection (toMap title)

module DbWkStationOfRoute =
    let private toMap (title: string) (route: int) =
        Map.empty.Add("title", title).Add("route", route.ToString())

    let collection = "DbWkStationOfRoute"

    let insert title route value =
        collectionInsert collection (toMap title route) (Serializer.Serialize<list<StationOfDbWk>>(value))

    let query title route =
        collectionQuery collection (toMap title route)
        |> List.map Serializer.Deserialize<list<StationOfDbWk>>

    let querysAsStrings title route =
        collectionQuery collection (toMap title route)

module WkStationOfRoute =
    let private toMap (title: string) (route: int) =
        Map.empty.Add("title", title).Add("route", route.ToString())

    let collection = "WkStationOfRoute"

    let insert title route value =
        collectionInsert collection (toMap title route) (Serializer.Serialize<StationOfRoute []>(value))

    let query title route =
        collectionQuery "WkStationOfRoute" (toMap title route)
        |> List.map Serializer.Deserialize<list<StationOfRoute>>

    let queryAsStrings title route =
        collectionQuery "WkStationOfRoute" (toMap title route)

module RouteInfo =
    let private toMap (title: string) (route: int) =
        Map.empty.Add("title", title).Add("route", route.ToString())

    let collection = "RouteInfo"

    let insert title route value =
        collectionInsert collection (toMap title route) (Serializer.Serialize<RouteInfo>(value))

    let delete title =
        collectionDelete collection (Map.empty.Add("title", title))

    let query title route =
        collectionQuery collection (toMap title route)
        |> List.map Serializer.Deserialize<RouteInfo>

    let queryAll () =
        toJsonArray (collectionQuery collection Map.empty)

module ResultOfRoute =
    let private toMap (title: string) (route: int) =
        Map.empty.Add("title", title).Add("route", route.ToString())

    let collection = "ResultOfRoute"

    let insert title route value =
        collectionInsert collection (toMap title route) (Serializer.Serialize<ResultOfRoute>(value))

    let delete title =
        collectionDelete collection (Map.empty.Add("title", title))

    let query title route =
        collectionQuery collection (toMap title route)
        |> List.map Serializer.Deserialize<ResultOfRoute>

    let queryAll () =
        toJsonArray (collectionQuery collection Map.empty)
