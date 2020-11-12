
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Giraffe
open Giraffe.ViewEngine

let contentTypeJson = "application/json; charset=utf-8"
let contentTypeCss = "text/css; charset=utf-8"

let sendFile (filePath : string) (contentType:string) : HttpHandler =
    fun (_ : HttpFunc) (ctx : HttpContext) ->
        let text = System.IO.File.ReadAllText filePath
        ctx.SetContentType contentType
        ctx.WriteStringAsync text

let jsonFile (filePath : string) : HttpHandler =
    sendFile filePath contentTypeJson

let cssFile (filePath : string) : HttpHandler =
    sendFile filePath contentTypeCss

let webApp =
    choose [
        routef "/dist/css/%s" ((+) "./dist/css/" >> cssFile)
        routef "/dist/js/%s" ((+) "./dist/js/" >> jsonFile)
        routef "/dump/%s" ((+) "./dump/" >> jsonFile)
        routef "/js/%s" ((+) "./js/" >> jsonFile)
        routef "/stationOfDbWk/%s/%i" (Views.stationOfDbWk >> RenderView.AsString.htmlDocument >> htmlString)
        routef "/wkStationOfRoute/%s/%i" (Views.stationOfRoute>>  RenderView.AsString.htmlDocument >> htmlString)
        routef "/dbStationOfRoute/%s/%i" (Views.dbStationOfRoute>>  RenderView.AsString.htmlDocument >> htmlString)
        routef "/stationOfInfobox/%s" (Views.stationOfInfobox >> RenderView.AsString.htmlDocument >> htmlString)
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