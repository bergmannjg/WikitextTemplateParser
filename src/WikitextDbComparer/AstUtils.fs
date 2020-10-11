module AstUtils

open Ast
open System.Text.RegularExpressions

type Bahnhof =
    { km: float
      km2: float option
      name: string }

let findTemplateParameterString (templates: Template []) (templateName: string) (parameterName: string) =
    templates
    |> Array.map (fun t ->
        match t with
        | Template (n, l) when templateName = n -> Some l
        | _ -> None)
    |> Array.choose id
    |> Array.collect List.toArray
    |> Array.map (fun p ->
        match p with
        | String (n, v) when parameterName = n || ("DE-" + parameterName) = n -> Some v
        | Composite (n, v) when (parameterName = n || ("DE-" + parameterName) = n)
                                && v.Length > 0 ->
            match v with
            | Composite.String (s) :: _ -> Some s
            | _ -> None
        | _ -> None)
    |> Array.choose id
    |> Array.tryExactlyOne

let findBsDatenStreckenNr (templates: Template []) =
    let fromTo =
        match findTemplateParameterString templates "BS-header" "" with
        | Some value -> value.Split "–"
        | None -> Array.empty

    match findTemplateParameterString templates "BS-daten" "STRECKENNR" with
    | Some value ->
        let strecken =
            ResizeArray<System.Collections.Generic.KeyValuePair<int, string []>>()

        let regex0 = Regex(@"^([0-9]+)$")
        let mc0 = regex0.Matches value
        for m in mc0 do
            if m.Groups.Count = 2
            then strecken.Add(System.Collections.Generic.KeyValuePair.Create(m.Groups.[1].Value |> int, fromTo))
        if mc0.Count = 0 then
            let regex1 = Regex(@"^([0-9]+)[\<,]")
            let mc1 = regex1.Matches value
            for m in mc1 do
                if m.Groups.Count = 2
                then strecken.Add(System.Collections.Generic.KeyValuePair.Create(m.Groups.[1].Value |> int, fromTo))
            if mc1.Count = 0 then
                let regex2 = Regex(@"([0-9]+)[^\(]*\(([^\)]+)") // subroutes
                let mc2 = regex2.Matches value
                for m in mc2 do
                    if m.Groups.Count = 3 then
                        let names = m.Groups.[2].Value.Split "–"
                        strecken.Add
                            (System.Collections.Generic.KeyValuePair.Create
                                (m.Groups.[1].Value |> int, (if names.Length = 2 then names else fromTo)))
        if strecken.Count = 0
        then printfn "couldnt match STRECKENNR: %s" value
        strecken.ToArray()
    | _ -> Array.empty

let parse2float (km: string) =
    let f =
        double (System.Single.Parse(km.Replace(",", ".")))

    System.Math.Round(f, 1)

let textOfLink (link: Link) =
    match link with
    | (t, n) -> if not (System.String.IsNullOrEmpty(n)) then n else t

let findBahnhofName (p: Parameter) =
    match p with
    | Composite (_, cl) ->
        match cl with
        | fst :: rest ->
            match fst with
            | Link (link) -> Some(textOfLink link)
            | _ -> None
        | _ -> None
    | String (_, n) -> Some(n)
    | _ -> None

let findKm (p: Parameter) =
    match p with
    | String (_, km) -> Some((parse2float (km.Replace("(", "").Replace(")", "")), None))
    | Composite (_, cl) ->
        match cl with
        | fst :: rest ->
            match fst with
            | Composite.Template (n, lp) when n = "BSkm" && lp.Length = 2 ->
                match lp.[0], lp.[1] with
                | String (_, km), String (_, k2) -> Some(parse2float km, Some(parse2float k2))
                | _ -> None
            | Composite.String (km) -> Some(parse2float km, None)
            | _ -> None
        | _ -> None

    | _ -> None

let findKmBahnhof (p1: Parameter) (p2: Parameter) =
    match findKm p1, findBahnhofName p2 with
    | Some (km, km2Option), Some (name) ->
        Some
            ({ km = km
               km2 = km2Option
               name = name })
    | _ -> None

let matchesType (parameters: List<Parameter>) (types: string []) =
    parameters
    |> List.exists (fun t ->
        match t with
        | String (n, v) when types |> Array.exists v.Contains -> true
        | _ -> false)

let bhftypes = [| "BHF"; "HST"; "DST" |]

let findBahnhof (t: Template) =
    match t with
    | Template (n, l) when "BS" = n
                           || "BSe" = n
                              && l.Length >= 3
                              && (matchesType (List.take 1 l) bhftypes) -> findKmBahnhof l.[1] l.[2]
    | Template (n, l) when "BS2" = n
                           && l.Length >= 4
                           && (matchesType (List.take 2 l) bhftypes) -> findKmBahnhof l.[2] l.[3]
    | Template (n, l) when "BS3" = n
                           && l.Length >= 5
                           && (matchesType (List.take 3 l) bhftypes) -> findKmBahnhof l.[3] l.[4]
    | Template (n, l) when "BS4" = n
                           && l.Length >= 6
                           && (matchesType (List.take 4 l) bhftypes) -> findKmBahnhof l.[4] l.[5]
    | _ -> None

let filterRoute (fromTo: string []) (arrbhf: Bahnhof []) =
    let mutable routeActive =
        fromTo.Length = 0
        || fromTo |> Array.exists arrbhf.[0].name.StartsWith

    let arbhfOfRoute = ResizeArray<Bahnhof>()
    for bhf in arrbhf do
        if routeActive then arbhfOfRoute.Add bhf
        match bhf.km2 with
        | Some km2 ->
            let routeWillChange =
                fromTo |> Array.exists bhf.name.StartsWith

            if routeActive && routeWillChange then
                routeActive <- false
            else if not routeActive && routeWillChange then
                routeActive <- true
                arbhfOfRoute.Add { bhf with km = km2; km2 = None }




        | _ -> ()
    if routeActive && arrbhf.Length > 0 then // close route for error handling
        routeActive <-
            not
                (fromTo
                 |> Array.exists arrbhf.[arrbhf.Length - 1].name.StartsWith)
    if routeActive && fromTo.Length > 0
    then printfn "filterRoute: some of %A not found, maybe BsKm template missing" fromTo
    arbhfOfRoute.ToArray()

let findBahnhöfe (templates: Template []) (fromTo: string []) =
    templates
    |> Array.map findBahnhof
    |> Array.choose id
    |> filterRoute fromTo
