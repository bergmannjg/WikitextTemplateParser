/// wrapper class for LiteDB database
module Database

open System
open LiteDB

type Row =
    { id: Guid
      keyValues: Map<string, string>
      value: string }

type Database(file: string) =
    let db = new LiteDatabase(file)
    let keyPrefix = "key"

    let toMap (doc: BsonDocument) =
        doc.Keys
        |> Seq.filter (fun k -> k.StartsWith keyPrefix)
        |> Seq.fold (fun (st: Map<string, string>) k -> st.Add(k.Substring(3), doc.[k].AsString)) Map.empty

    let rec toQuery (keyValues: list<string * string>) =
        match keyValues with
        | [ (k, v) ] -> Query.EQ(keyPrefix + k, BsonValue v)
        | (k, v) :: rest -> Query.And(Query.EQ(keyPrefix + k, BsonValue v), (toQuery rest))
        | _ -> failwith "toQuery: map is empty"

    member __.DropCollection(collectionName: string) = db.DropCollection collectionName

    member __.Insert (collectionName: string) (id: Guid) (keyValues: Map<string, string>) (value: string) =
        let collection = db.GetCollection(collectionName)
        let doc = BsonDocument()
        doc.Add("_id", BsonValue id)
        keyValues
        |> Map.iter (fun k v -> doc.Add(keyPrefix + k, BsonValue v))
        doc.Add("value", BsonValue value)
        let _id = collection.Insert doc
        _id.AsGuid

    member __.Delete (collectionName: string) (id: Guid) =
        let collection = db.GetCollection(collectionName)
        collection.Delete(BsonValue id)

    member __.Query (collectionName: string) (keyValues: Map<string, string>) =
        let collection = db.GetCollection(collectionName)
        if keyValues.IsEmpty
        then collection.Find(Query.All(), 0, 2000)
        else collection.Find(toQuery (keyValues |> Map.toList), 0, 2000)
        |> Seq.map (fun doc ->
            { id = doc.["_id"].AsGuid
              keyValues = toMap doc
              value = doc.["value"].AsString })

    member __.InsertUnique (collectionName: string) (id: Guid) (keyValues: Map<string, string>) (value: string) =
        if keyValues.IsEmpty then failwith "InsertUnique: map is empty"

        __.Query collectionName keyValues
        |> Seq.iter (fun r -> __.Delete collectionName r.id |> ignore)

        __.Insert collectionName id keyValues value

    interface IDisposable with
        member __.Dispose() = db.Dispose()
