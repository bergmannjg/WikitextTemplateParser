namespace WikitextRouteDiagrams

open FSharp.Data

module OsmData =

    let private url =
        "http://overpass-api.de/api/interpreter?data="

    let private routeQuery = """
    [timeout:900][out:json];
    area
      [boundary=administrative]
      [name='Deutschland'];
    relation
      [route=tracks]
      [ref=0000]
      [type=route];
    out;"""

    let private loadOsmData (query: string) =
        async {
            let! response = JsonValue.AsyncLoad(url + query)
            return response
        }
        |> Async.RunSynchronously

    let private matchProperty (json: JsonValue) (name: string) =
        match json with
        | JsonValue.Record properties ->
            match properties
                  |> Array.tryFind (fun (s, v) -> s = name) with
            | Some (name, v) -> Some v
            | _ -> None
        | _ -> None

    let private matchRelationId (json: JsonValue) =
        let elements =
            match matchProperty json "elements" with
            | Some (JsonValue.Array elements) -> elements
            | _ -> Array.empty

        if elements.Length > 0 then
            match matchProperty elements.[0] "id" with
            | Some (JsonValue.Number id) -> Some(id |> int)
            | _ -> None
        else
            None

    let loadRelationId (route: int) =
        let query =
            routeQuery.Replace("0000", route.ToString())

        let json = loadOsmData query

        matchRelationId json

