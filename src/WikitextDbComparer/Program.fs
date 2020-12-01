
open RouteInfo
open Comparer
open ResultsOfMatch
open Wikidata
open Ast

let markersOfStop = [| "BHF"; "DST"; "HST" |]

let excludes (link:string) =
    link.StartsWith "Bahnstrecke" || link.StartsWith "Datei" || link.Contains "#" || link.Contains ":" || link.Contains "&"

let getStationLinksOfTemplates (title:string) = 
    loadTemplates title
    |> findLinks markersOfStop
    |> List.filter (fun (link,_) -> not (excludes link))
    |> List.map (fun (link,_) -> link)
    
let getStationLinks () = 
    DataAccess.Templates.queryKeys()
    |> List.collect getStationLinksOfTemplates
    |> List.distinct
    |> List.sort
    |> List.iter (fun s -> printfn "%s" s)

let classifyBsDatenStreckenNr showDetails title  =
    let templates = loadTemplatesForWikiTitle title  
    match findRouteInfoInTemplates templates title with
    | Some strecken ->
        strecken|>List.iter (printRouteInfo showDetails) 
    | None -> 
        ()

let classifyBsDatenStreckenNrTitles () =
    DataAccess.Templates.queryKeys()
    |> List.iter (classifyBsDatenStreckenNr false)

let comparetitle showDetails title =
    loadTemplatesForWikiTitle title  
    |> compare showDetails title

let comparetitles () =
    DataAccess.Templates.queryKeys()
    |> List.iter (comparetitle false)

[<EntryPoint>]
let main argv =
    Serializer.addConverters ([| |])
    match argv with
    | [| "-dropCollection"; collection |] -> DataAccess.dropCollection collection |> ignore
    | [| "-getStationLinks" |] -> getStationLinks ()
    | [| "-comparetitle"; title |] -> comparetitle  false title
    | [| "-verbose"; "-comparetitle"; title |] -> comparetitle  true title
    | [| "-comparetitles" |] -> comparetitles ()
    | [| "-showCompareResults" |] -> showResults()
    | [| "-classify" |] -> classifyBsDatenStreckenNrTitles ()
    | [| "-showClassifyResults" |] -> showRouteInfoResults()
    | [| "-showMatchKindStatistics" |] -> showMatchKindStatistics()
    | _ -> fprintfn stderr "usage: [-verbose] -comparetitle title | -showCompareResults | -classify title | -showClassifyResults | -showMatchKindStatistics"   
    0
