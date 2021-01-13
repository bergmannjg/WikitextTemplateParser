namespace WikitextRouteDiagrams

open System

open Database

[<RequireQualifiedAccess>]
module DataAccess =

    let mutable private dbname = @"results.db"

    let setDbname name = dbname <- name

    let dropCollection collectionName =
        use db = new Database(dbname)
        db.DropCollection collectionName

    let collectionInsert (collection: string) (map: Map<string, string>) (value: string) =
        use db = new Database(dbname)
        db.InsertUnique collection (Guid.NewGuid()) map value

    let typedCollectionInsert<'a> (collection: string) (map: Map<string, string>) (value: 'a) =
        use db = new Database(dbname)
        db.InsertUnique collection (Guid.NewGuid()) map (Serializer.Serialize<'a>(value))

    let collectionDelete (collection: string) (map: Map<string, string>) =
        use db = new Database(dbname)

        db.Query collection map
        |> Seq.toList
        |> List.map (fun r -> db.Delete collection r.id)
        |> List.forall ((=) true)

    let collectionQuery (collection: string) (map: Map<string, string>) =
        use db = new Database(dbname)

        db.Query collection map
        |> Seq.map (fun r -> r.value)
        |> Seq.toList

    let typedCollectionQuery<'a> (collection: string) (map: Map<string, string>) =
        use db = new Database(dbname)

        db.Query collection map
        |> Seq.map (fun r -> Serializer.Deserialize<'a>(r.value))
        |> Seq.toList

    let collectionQueryKeys (collection: string) (map: Map<string, string>) =
        use db = new Database(dbname)

        db.Query collection map
        |> Seq.map (fun r -> r.keyValues)
        |> Seq.toList

    let toJsonArray (results: list<string>) =
        if results.Length = 0 then
            "[]"
        else
            let text =
                results
                |> Seq.reduce (fun text texts -> text + "," + texts)

            "[" + text + "]"

    let toJson (key: string) (value: string) = sprintf "{\"%s\":\"%s\"}" key value

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

        let query title =
            collectionQuery collection (toMap title)

        let queryKeysAsJson () =
            toJsonArray (
                collectionQueryKeys collection Map.empty
                |> List.map (fun k -> toJson "title" k.["title"])
            )

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

        let queryAsStrings title =
            collectionQuery collection (toMap title)

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

