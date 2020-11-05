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
          rawText """
          .tabulator .tabulator-tableHolder {
                background:white;
            }
            .container {
                width:1400px;
                height:600px;
                margin-left: 50px;
            }
            #leftbox {
                float:left;
                width:700px;
                height:600px;
            }
            #rightbox{
                float:right;
                width:600px;
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
            div [ _id "statusMsg"
                  _style "margin-left:5%; margin-bottom:5px;" ] []
            div [ _id "results-table"
                  _style "width:90%; left:5%" ] []
            script [ _type "application/javascript" ] [
                rawText """
                loadResultsTable("#results-table", document.getElementById("statusMsg"), "/dump/results.json");
            """
            ]
        ]
    ]

let viewWithLeftRightBox (title: string) (wikititle: string) (scripttext: string) =
    html [] [
        head [] (titleAndScripts title)
        body [] [
            div [ _class "container" ] [
                h1 [ _style "text-align:center" ] [
                    str title
                ]
                div [ _id "leftbox" ] []
                iframe [ _id "rightbox"
                         _src ("https://de.wikipedia.org/wiki/" + wikititle) ] []
                script [ _type "application/javascript" ] [
                    rawText scripttext
                ]
            ]
        ]
    ]

let stationOfInfobox (title: string) =
    viewWithLeftRightBox
        ("StationsOfInfobox " + title)
        title
        ("loadStationOfInfoTable(\"#leftbox\", \"/dump/"
         + title
         + "-StationOfInfobox.json\");")

let stationOfRoute (title: string, route: int) =
    viewWithLeftRightBox
        ("StationOfRoute " + title + " " + route.ToString())
        title
        ("loadStationOfRouteTable(\"#leftbox\", \"/dump/"
         + title
         + "-"
         + route.ToString()
         + "-StationOfRoute.json\");")

let stationOfDbWk (title: string, route: int) =
    viewWithLeftRightBox
        ("DB/Wiki Stations "
         + title
         + " "
         + route.ToString())
        title
        ("loadStationOfDbWkTable(\"#leftbox\", \"/dump/"
         + title
         + "-"
         + route.ToString()
         + "-StationOfDbWk.json\");")
