namespace WikitextRouteDiagrams

/// result of match of operational points
type ResultOfOpPoint =
    | Success of DbOpPointOfRoute * WkOpPointOfRoute * MatchKind
    | Failure of DbOpPointOfRoute

[<RequireQualifiedAccess>]
module ResultOfOpPoint =

    let exists (pred: (DbOpPointOfRoute * WkOpPointOfRoute * MatchKind) -> bool) (results: array<ResultOfOpPoint>) =
        results
        |> Array.exists
            (function
            | Success (db, wk, mk) -> pred (db, wk, mk)
            | _ -> false)

    let tryFindIndex
        (pred: (DbOpPointOfRoute * WkOpPointOfRoute * MatchKind) -> bool)
        (results: array<ResultOfOpPoint>)
        =
        results
        |> Array.tryFindIndex
            (function
            | Success (db, wk, mk) -> pred (db, wk, mk)
            | _ -> false)

    let filter (pred: (DbOpPointOfRoute * WkOpPointOfRoute * MatchKind) -> bool) (results: array<ResultOfOpPoint>) =
        results
        |> Array.map
            (fun result ->
                match result with
                | Success (db, wk, mk) when pred (db, wk, mk) -> Some(result)
                | _ -> None)
        |> Array.choose id

    let mapDbOpPoint<'a>
        (mapsDbOpPoint: DbOpPointOfRoute -> 'a option)
        (mapping: DbOpPointOfRoute -> 'a -> ResultOfOpPoint)
        (results: array<ResultOfOpPoint>)
        =
        results
        |> Array.map
            (fun result ->
                match result with
                | Success (db, _, _)
                | Failure (db) ->
                    match mapsDbOpPoint db with
                    | Some a -> mapping db a
                    | None -> result)

    let mapToSuccess
        (shouldMap: DbOpPointOfRoute -> bool)
        (mapping: DbOpPointOfRoute -> (WkOpPointOfRoute * MatchKind))
        (results: array<ResultOfOpPoint>)
        =
        results
        |> Array.map
            (fun result ->
                match result with
                | Failure (db) when shouldMap (db) ->
                    let (wk, mk) = mapping db
                    Success(db, wk, mk)
                | _ -> result)

    let mapToFailure
        (shouldMap: (DbOpPointOfRoute * WkOpPointOfRoute * MatchKind) -> bool)
        (results: array<ResultOfOpPoint>)
        =
        results
        |> Array.map
            (fun result ->
                match result with
                | Success (db, wk, mk) when shouldMap (db, wk, mk) -> Failure(db)
                | _ -> result)

    let filteredMap
        (pred: (DbOpPointOfRoute * WkOpPointOfRoute * MatchKind) -> bool)
        (mapping: (DbOpPointOfRoute * WkOpPointOfRoute * MatchKind) -> 'a)
        (results: array<ResultOfOpPoint>)
        =
        results
        |> Array.map
            (function
            | Success (db, wk, mk) when pred (db, wk, mk) -> Some(mapping (db, wk, mk))
            | _ -> None)
        |> Array.choose id

    /// mapping (prev,curr,next) -> 'b
    let mapWithContext<'a, 'b> (mapping: 'a option -> 'a -> 'a option -> 'b) (array: 'a []) =
        array
        |> Array.mapi
            (fun i curr ->
                let prev =
                    if i = 0 then
                        None
                    else
                        Some array.[i - 1]

                let next =
                    if i = array.Length - 1 then
                        None
                    else
                        Some array.[i + 1]

                mapping prev curr next)

    let mapFailureWithMatchKindContext
        (shouldMap: (MatchKind option * MatchKind option) -> bool)
        (mapping: DbOpPointOfRoute -> ResultOfOpPoint)
        (results: array<ResultOfOpPoint>)
        =
        results
        |> mapWithContext
            (fun prev curr next ->
                match prev, curr, next with
                | Some (Success (_, _, mk1)), Failure p, Some (Success (_, _, mk2)) when shouldMap (Some mk1, Some mk2) ->
                    mapping p
                | Some (Success (_, _, mk1)), Failure p, Some (Failure _) when shouldMap (Some mk1, None) -> mapping p
                | _ -> curr)

    let countFailures results =
        results
        |> Array.filter
            (fun result ->
                match result with
                | ResultOfOpPoint.Failure _ -> true
                | ResultOfOpPoint.Success _ -> false)
        |> Array.length

    let countSuccess results =
        results
        |> Array.filter
            (fun result ->
                match result with
                | ResultOfOpPoint.Failure _ -> false
                | ResultOfOpPoint.Success _ -> true)
        |> Array.length

