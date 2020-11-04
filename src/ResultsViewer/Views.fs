module Views

open Giraffe.ViewEngine

let titleAndScripts (titleString: string) =
    [ title [] [ str titleString ]
      link [ _rel "stylesheet"
             _href "/dist/css/tabulator.min.css" ]
      script [ _type "application/javascript"
               _src "/dist/js/tabulator.min.js" ] [] // http://tabulator.info
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
