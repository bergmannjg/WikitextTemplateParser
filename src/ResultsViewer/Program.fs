
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Giraffe
open Giraffe.ViewEngine

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

let concat (p1,p2) = p1 + "/" + p2

let webApp =
    choose [
        routef "/dist/css/%s" ((+) "./dist/css/" >> cssFile)
        routef "/dist/js/%s" ((+) "./dist/js/" >> jsonFile)
        route "/data/results" >=> (loadAll DataAccess.ResultOfRoute.queryAll)
        route "/data/routeinfos" >=> (loadAll DataAccess.RouteInfo.queryAll)
        route "/data/stops" >=> (loadAll DataAccess.WikitextOfStop.queryKeysAsJson)
        routef "/data/Wikitext/%s" loadWikitext
        routef "/data/WikitextOfStop/%s" loadWikitextOfStop
        routef "/data/WikitextOfStop/%s/%s" (concat >> loadWikitextOfStop)
        routef "/data/Templates/%s" (loadWithTitle DataAccess.TemplatesOfRoute.queryAsStrings)
        routef "/data/StationOfInfobox/%s" (loadWithTitle DataAccess.WkStationOfInfobox.queryAsStrings)
        routef "/data/DbStationOfRoute/%s/%i" (loadWithTitleAndRoute DataAccess.DbStationOfRoute.queryAsStrings)
        routef "/data/WkStationOfRoute/%s/%i" (loadWithTitleAndRoute DataAccess.WkStationOfRoute.queryAsStrings)
        routef "/data/StationOfDbWk/%s/%i" (loadWithTitleAndRoute DataAccess.DbWkStationOfRoute.querysAsStrings)
        routef "/js/%s" ((+) "./js/" >> jsonFile)
        routef "/stationOfDbWk/%s/%i" (Views.stationOfDbWk >> RenderView.AsString.htmlDocument >> htmlString)
        routef "/wkStationOfRoute/%s/%i" (Views.wkStationOfRoute >>  RenderView.AsString.htmlDocument >> htmlString)
        routef "/dbStationOfRoute/%s/%i" (Views.dbStationOfRoute >>  RenderView.AsString.htmlDocument >> htmlString)
        routef "/stationOfInfobox/%s" (Views.stationOfInfobox >> RenderView.AsString.htmlDocument >> htmlString)
        route "/routeinfos" >=> (RenderView.AsString.htmlDocument >> htmlString) Views.routeinfos
        route "/stops" >=> (RenderView.AsString.htmlDocument >> htmlString) Views.stops
        route "/" >=> (RenderView.AsString.htmlDocument >> htmlString) Views.index ]

let configureApp (app : IApplicationBuilder) =
    app.UseGiraffe webApp

let configureServices (services : IServiceCollection) =
    services.AddGiraffe() |> ignore

let configureLogging (builder : ILoggingBuilder) =
    builder.ClearProviders().AddConsole() |> ignore

[<EntryPoint>]
let main _ =
    Host.CreateDefaultBuilder()
        .ConfigureWebHostDefaults(
            fun webHostBuilder ->
                webHostBuilder
                    .Configure(configureApp)
                    .ConfigureServices(configureServices)
                    .ConfigureLogging(configureLogging)
                    |> ignore)
        .Build()
        .Run()
    0