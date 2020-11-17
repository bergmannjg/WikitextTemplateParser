
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
    sendText (query()) contentTypeJson

let loadWithTitle (query: string -> list<string>) (title:string) : HttpHandler =
    match query title |> Seq.tryHead with
    | Some json -> sendText json contentTypeJson
    | _ -> sendText "" contentTypeJson

let prepareWikitext (wikitext :string) = 
    wikitext.Replace("{{BS", "\r\n{{BS") 

let loadWikitext (title:string) : HttpHandler =
    match DataAccess.Wikitext.query title |> Seq.tryHead with
    | Some text -> sendText (prepareWikitext text)  contentTypeText
    | _ -> sendText "" contentTypeText

let loadWithTitleAndRoute (query: string -> int -> list<string>) (title:string, route:int) : HttpHandler =
    match query title route |> Seq.tryHead with
    | Some json -> sendText json contentTypeJson
    | _ -> sendText "" contentTypeJson

let webApp =
    choose [
        routef "/dist/css/%s" ((+) "./dist/css/" >> cssFile)
        routef "/dist/js/%s" ((+) "./dist/js/" >> jsonFile)
        route "/data/results" >=> (loadAll DataAccess.ResultOfRoute.queryAll)
        route "/data/routeinfos" >=> (loadAll DataAccess.RouteInfo.queryAll)
        routef "/data/Wikitext/%s" loadWikitext
        routef "/data/Templates/%s" (loadWithTitle DataAccess.Templates.query)
        routef "/data/StationOfInfobox/%s" (loadWithTitle DataAccess.WkStationOfInfobox.query)
        routef "/data/DbStationOfRoute/%s/%i" (loadWithTitleAndRoute DataAccess.DbStationOfRoute.query)
        routef "/data/WkStationOfRoute/%s/%i" (loadWithTitleAndRoute DataAccess.WkStationOfRoute.query)
        routef "/data/StationOfDbWk/%s/%i" (loadWithTitleAndRoute DataAccess.DbWkStationOfRoute.query)
        routef "/js/%s" ((+) "./js/" >> jsonFile)
        routef "/stationOfDbWk/%s/%i" (Views.stationOfDbWk >> RenderView.AsString.htmlDocument >> htmlString)
        routef "/wkStationOfRoute/%s/%i" (Views.wkStationOfRoute >>  RenderView.AsString.htmlDocument >> htmlString)
        routef "/dbStationOfRoute/%s/%i" (Views.dbStationOfRoute >>  RenderView.AsString.htmlDocument >> htmlString)
        routef "/stationOfInfobox/%s" (Views.stationOfInfobox >> RenderView.AsString.htmlDocument >> htmlString)
        route "/routeinfos" >=> (RenderView.AsString.htmlDocument >> htmlString) Views.routeinfos
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