
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Giraffe
open Giraffe.ViewEngine
open RInfData

let contentTypeText = "text/plain; charset=utf-8"
let contentTypeJson = "application/json; charset=utf-8"
let contentTypeCss = "text/css; charset=utf-8"

let sendText (text : string) (contentType:string) : HttpHandler =
    fun (_ : HttpFunc) (ctx : HttpContext) ->
        ctx.SetContentType contentType
        ctx.WriteStringAsync text

let sendFile (filePath : string) (contentType:string) : HttpHandler =
    fun (_ : HttpFunc) (ctx : HttpContext) ->
        let text = System.IO.File.ReadAllText filePath
        ctx.SetContentType contentType
        ctx.WriteStringAsync text

let jsonFile (filePath : string) : HttpHandler =
    sendFile filePath contentTypeJson

let cssFile (filePath : string) : HttpHandler =
    sendFile filePath contentTypeCss

let loadAll (query: (_) -> string)  : HttpHandler =
    fun (_ : HttpFunc) (ctx : HttpContext) ->
        ctx.SetContentType contentTypeJson
        ctx.WriteStringAsync (query())

let loadWithTitle (query: string -> list<string>) (title:string) : HttpHandler =
    match query title |> Seq.tryHead with
    | Some json -> sendText json contentTypeJson
    | _ -> sendText "" contentTypeJson

let prepareWikitext (wikitext :string) = 
    wikitext.Replace("{{BS", "\r\n{{BS").Replace(" |", "\r\n| ").Replace(" * ", "\r\n * ")

let loadWikitext (title:string) : HttpHandler =
    match DataAccess.WikitextOfRoute.query title |> Seq.tryHead with
    | Some text -> sendText (prepareWikitext text)  contentTypeText
    | _ -> sendText "" contentTypeText

let loadWikitextOfStop (stop:string) : HttpHandler =
    printfn "loadWikitextOfStop: %s" stop   
    match DataAccess.WikitextOfStop.query stop |> Seq.tryHead with
    | Some text -> sendText (prepareWikitext text)  contentTypeText
    | _ -> sendText "" contentTypeText

let loadWithTitleAndRoute (query: string -> int -> list<string>) (title:string, route:int) : HttpHandler =
    match query title route |> Seq.tryHead with
    | Some json -> sendText json contentTypeJson
    | _ -> sendText "" contentTypeJson

let sendTextWithRoute (query: int -> string) (route:int) : HttpHandler =
    sendText (query route) contentTypeJson

let redirectOsmRelation (route:int) : HttpHandler =
    match OsmData.loadRelationId route with
    | Some relation -> redirectTo false ("https://www.openstreetmap.org/relation/" + relation.ToString() + "#map=7/51.771/10.811")
    | _ -> (RenderView.AsString.htmlDocument >> htmlString) Views.index

let concat (p1,p2) = p1 + "/" + p2

let webApp =
    choose [
        routef "/dist/css/%s" ((+) "./dist/css/" >> cssFile)
        routef "/dist/js/%s" ((+) "./dist/js/" >> jsonFile)
        route "/data/results" >=> (loadAll DataAccess.ResultOfRoute.queryAll)
        route "/data/routeinfos" >=> (loadAll DataAccess.RouteInfo.queryAll)
        route "/data/stops" >=> (loadAll DataAccess.WikitextOfStop.queryKeysAsJson)
        route "/data/SubstringMatches" >=> (loadAll  ResultsOfMatch.loadSubstringMatches)
        routef "/data/Wikitext/%s" loadWikitext
        routef "/data/WikitextOfStop/%s" loadWikitextOfStop
        routef "/data/WikitextOfStop/%s/%s" (concat >> loadWikitextOfStop)
        routef "/data/Templates/%s" (loadWithTitle DataAccess.TemplatesOfRoute.queryAsStrings)
        routef "/data/StationOfInfobox/%s" (loadWithTitle DataAccess.WkOpPointOfInfobox.queryAsStrings)
        routef "/data/DbStationOfRoute/%i" (sendTextWithRoute (DbData.loadRouteAsJSon))
        routef "/data/RInfStationOfRoute/%i" (sendTextWithRoute (RInfData.loadRouteAsJSon))
        routef "/data/RInfSolOfRoute/%i" (sendTextWithRoute (RInfData.loadSoLAsJSon))
        routef "/data/WkStationOfRoute/%s/%i" (loadWithTitleAndRoute DataAccess.WkOpPointOfRoute.queryAsStrings)
        routef "/data/StationOfDbWk/%s/%i" (loadWithTitleAndRoute DataAccess.DbWkOpPointOfRoute.querysAsStrings)
        routef "/js/%s" ((+) "./js/" >> jsonFile)
        routef "/osmRelationOfRoute/%i" redirectOsmRelation
        routef "/stationOfDbWk/%s/%i" (Views.stationOfDbWk >> RenderView.AsString.htmlDocument >> htmlString)
        routef "/wkStationOfRoute/%s/%i" (Views.wkStationOfRoute >>  RenderView.AsString.htmlDocument >> htmlString)
        routef "/dbStationOfRoute/%i" (Views.dbStationOfRoute >>  RenderView.AsString.htmlDocument >> htmlString)
        routef "/rinfStationOfRoute/%i" (Views.rinfStationOfRoute >>  RenderView.AsString.htmlDocument >> htmlString)
        routef "/rinfSoLOfRoute/%i" (Views.rinfSoLOfRoute >>  RenderView.AsString.htmlDocument >> htmlString)
        routef "/stationOfInfobox/%s" (Views.stationOfInfobox >> RenderView.AsString.htmlDocument >> htmlString)
        route "/routeinfos" >=> (RenderView.AsString.htmlDocument >> htmlString) Views.routeinfos
        route "/stops" >=> (RenderView.AsString.htmlDocument >> htmlString) Views.stops
        route "/substringMatches" >=> (RenderView.AsString.htmlDocument >> htmlString) Views.substringMatches
        route "/" >=> (RenderView.AsString.htmlDocument >> htmlString) Views.index ]

let configureApp (app : IApplicationBuilder) =
    app.UseGiraffe webApp

let configureServices (services : IServiceCollection) =
    services.AddGiraffe() |> ignore

let configureLogging (builder : ILoggingBuilder) =
    builder.ClearProviders().AddConsole().SetMinimumLevel(LogLevel.Information) |> ignore

[<EntryPoint>]
let main _ =
    Serializer.addConverters ([||])
    Host.CreateDefaultBuilder()
        .ConfigureWebHostDefaults(
            fun webHostBuilder ->
                webHostBuilder
                    .Configure(configureApp)
                    .ConfigureServices(configureServices)
                    .ConfigureLogging(configureLogging)
                    .UseUrls "http://localhost:5000" 
                    |> ignore)
        .Build()
        .Run()
    0