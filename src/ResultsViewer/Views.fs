module Views

open Giraffe.ViewEngine

let tabulatorCached = System.IO.Directory.Exists "./dist"

/// using http://tabulator.info
let titleAndScripts (titleString: string) =
    let tabulaturScript =
        if tabulatorCached
        then "/dist/js/tabulator.min.js"
        else "https://unpkg.com/tabulator-tables@4.8.4/dist/js/tabulator.min.js"

    let tabulaturCss =
        if tabulatorCached
        then "/dist/css/tabulator.min.css"
        else "https://unpkg.com/tabulator-tables@4.8.4/dist/css/tabulator.min.css"

    [ title [] [ str titleString ]
      link [ _rel "stylesheet"
             _href tabulaturCss ]
      script [ _type "application/javascript"
               _src tabulaturScript ] []
      script [ _type "application/javascript"
               _src "/js/loadTable.js" ] []
      style [] [
          rawText
              """
          .tabulator .tabulator-tableHolder {
                background:white;
            }
            .status {
                margin-left: 5%;
                margin-bottom: 5px;
            }
            .remark {
                margin-left: 50px;
                margin-bottom: 10px;
            }
            .container {
                width:1400px;
                height:600px;
                margin-left: 50px;
            }
            #leftbox {
                float:left;
                width:650px;
                height:600px;
            }
            #rightbox{
                float:right;
                width:650px;
                height:600px;
            }
            """
      ] ]

let index =
    html [] [
        head [] (titleAndScripts "Results")
        body [] [
            h1 [ _style "text-align:center" ] [
                str "Results"
            ]
            div [ _class "status" ] [
                span [ _id "statusMsg" ] []
                str " "
                a [ _href ("/routeinfos") ] [
                    str "Summary of route infos"
                ]
                str " "
                a [ _href ("/stops") ] [
                    str "Summary of stops"
                ]
                str " "
                a [ _href ("/substringMatches") ] [
                    str "SubstringMatches"
                ]
            ]
            div [ _id "results-table"
                  _style "width:90%; left:5%" ] []
            script [ _type "application/javascript" ] [
                rawText
                    """
                loadResultsTable("#results-table", document.getElementById("statusMsg"), "/data/results");
            """
            ]
        ]
    ]

let routeinfos =
    html [] [
        head [] (titleAndScripts "Route Infos")
        body [] [
            h1 [ _style "text-align:center" ] [
                str "Route Infos"
            ]
            div [ _id "statusMsg"; _class "status" ] []
            div [ _id "results-table"
                  _style "width:90%; left:5%" ] []
            script [ _type "application/javascript" ] [
                rawText
                    """
                loadRouteInfosTable("#results-table", document.getElementById("statusMsg"), "/data/routeinfos");
            """
            ]
        ]
    ]

let stops =
    html [] [
        head [] (titleAndScripts "Stops")
        body [] [
            h1 [ _style "text-align:center" ] [
                str "Stops"
            ]
            div [ _id "statusMsg"; _class "status" ] []
            div [ _id "results-table"
                  _style "width:90%; left:5%" ] []
            script [ _type "application/javascript" ] [
                rawText
                    """
                loadStopsTable("#results-table", document.getElementById("statusMsg"), "/data/stops");
            """
            ]
        ]
    ]

let viewWithLeftRightIFrameBox (title: string) (wikititle: string) (scripttext: string) (extraNode: XmlNode option) =
    html [] [
        head [] (titleAndScripts title)
        body [] [
            h1 [ _style "text-align:center" ] [
                str title
            ]
            if extraNode.IsSome then extraNode.Value

            div [ _class "container" ] [
                div [ _id "leftbox" ] []
                iframe [ _id "rightbox"
                         _src ("https://de.wikipedia.org/wiki/" + wikititle) ] []
                script [ _type "application/javascript" ] [
                    rawText scripttext
                ]
            ]
        ]
    ]

let viewWithLeftRightBox (title: string) (scripttext: string) (extraNode: XmlNode option) =
    html [] [
        head [] (titleAndScripts title)
        body [] [
            h1 [ _style "text-align:center" ] [
                str title
            ]
            if extraNode.IsSome then extraNode.Value

            div [ _class "container" ] [
                div [ _id "leftbox" ] []
                div [ _id "rightbox" ] []
                script [ _type "application/javascript" ] [
                    rawText scripttext
                ]
            ]
        ]
    ]

let stationOfInfobox (title: string) =
    let extraNode =
        div [ _class "remark" ] [
            a [ _href ("/data/Wikitext/" + title) ] [
                str "Wikitext"
            ]
            str " "
            a [ _href ("/data/Templates/" + title) ] [
                str "Templates of wikitext"
            ]
        ]

    viewWithLeftRightIFrameBox
        ("StationsOfInfobox " + title)
        title
        ("loadStationOfInfoTable(\"#leftbox\", \"/data/StationOfInfobox/"
         + title
         + "\");")
        (Some extraNode)

let wkStationOfRoute (title: string, route: int) =
    viewWithLeftRightIFrameBox
        ("StationOfRoute " + title + " " + route.ToString())
        title
        ("loadStationOfRouteTable(\"#leftbox\", \"/data/WkStationOfRoute/"
         + title
         + "/"
         + route.ToString()
         + "\");")
        None

let dbStationOfRoute (route: int) =
    let extraNode =
        div [ _class "remark" ] [
                a [ _href ("/rinfSoLOfRoute/" + route.ToString()) ] [
                    str "RINF SoL of route"
                ]
                str " "
                a [ _href ("https://geovdbn.deutschebahn.com/isr") ] [
                    str "DB ISR"
                ]
        ]

    viewWithLeftRightBox
        ("RINF versus DB Open data Stations " + route.ToString())
        ("loadRInfStationOfRouteTable(\"#leftbox\", \"/data/RInfStationOfRoute/"
         + route.ToString()
         + "\");"
         + "loadDbStationOfRouteTable(\"#rightbox\", \"/data/DbStationOfRoute/"
         + route.ToString()
         + "\");")
        (Some extraNode)

let rinfStationOfRoute (route: int) =
    html [] [
        head [] (titleAndScripts "RINF Stations of Route")
        body [] [
            h1 [ _style "text-align:center" ] [
                str ("RINF Stations of Route " + route.ToString())
            ]
            div [ _class "status" ] [
                a [ _href ("/rinfSoLOfRoute/" + route.ToString()) ] [
                    str "RINF SoL of route"
                ]
                str " "
                a [ _href ("https://geovdbn.deutschebahn.com/isr") ] [
                    str "DB ISR"
                ]
            ]
            div [ _id "results-table"
                  _style "width:90%; left:5%" ] []

            script [ _type "application/javascript" ] [
                rawText (
                    "loadRInfStationOfRouteTable(\"#results-table\", \"/data/RInfStationOfRoute/"
                    + route.ToString()
                    + "\");"
                )
            ]
        ]
    ]

let rinfSoLOfRoute (route: int) =
    html [] [
        head [] (titleAndScripts "RINF Sections of Lines of Route")
        body [] [
            h1 [ _style "text-align:center" ] [
                str (
                    "RINF Sections of Line of Route "
                    + route.ToString()
                )
            ]
            div [ _class "status" ] [
                a [ _href ("https://geovdbn.deutschebahn.com/isr") ] [
                    str "DB Infrastrukturregister"
                ]
                str " "
                a [ _href ("https://www.openrailwaymap.org/") ] [
                    str "OpenRailwayMap"
                ]
            ]
            div [ _id "results-table"
                  _style "width:90%; left:5%" ] []

            script [ _type "application/javascript" ] [
                rawText (
                    "loadRInfSolOfRouteTable(\"#results-table\", \"/data/RInfSolOfRoute/"
                    + route.ToString()
                    + "\");"
                )
            ]
        ]
    ]

let substringMatches  =
    html [] [
        head [] (titleAndScripts "SubstringMatches")
        body [] [
            h1 [ _style "text-align:center" ] [
                str (
                    "SubstringMatches"
                )
            ]
            div [ _class "status" ] [
                span [ _id "statusMsg" ] []
            ]
            div [ _id "results-table"
                  _style "width:90%; left:5%" ] []

            script [ _type "application/javascript" ] [
                rawText (
                    "loadSubstringMatches(\"#results-table\", document.getElementById(\"statusMsg\"), \"/data/SubstringMatches\");"
                )
            ]
        ]
    ]


let stationOfDbWk (title: string, route: int) =
    let extraNode =
        div [ _class "remark" ] [
            a [ _href (
                    "/wkStationOfRoute/"
                    + title
                    + "/"
                    + route.ToString()
                )
                _target "_blank" ] [
                str "Wiki stations"
            ]
            str " "
            a [ _href ("/osmRelationOfRoute/" + route.ToString()) ] [
                str "OSM data"
            ]
            str " "
            a [ _href ("/stationOfInfobox/" + title) ] [
                str "Wiki stations of infobox"
            ]
            str " "
            a [ _href ("/rinfStationOfRoute/" + route.ToString()) ] [
                str "RINF stations"
            ]
            str " "
            a [ _href ("/dbStationOfRoute/" + route.ToString()) ] [
                str "Db Open data stations"
            ]
        ]

    viewWithLeftRightIFrameBox
        ("DB/Wiki Stations "
         + title
         + " "
         + route.ToString())
        title
        ("loadStationOfDbWkTable(\""
         + title
         + "\","
         + route.ToString()
         + ",\"#leftbox\", \"/data/StationOfDbWk/"
         + title
         + "/"
         + route.ToString()
         + "\");")
        (Some extraNode)
