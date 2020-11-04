
function loadResultsTable(tableId, statusElementId, url) {

    let urlOfRoute = (cell) => {
        return "/stationOfDbWk/" + cell.getData().title + '/' + cell.getValue();
    }

    new Tabulator(tableId, {
        ajaxURL: url,
        layout: "fitColumns",
        pagination: "local",
        paginationSize: 20,
        dataFiltered: function (filters, rows) {
            //filters - array of filters currently applied
            //rows - array of row components that pass the filters
            statusElementId.textContent= rows.length + " rows selected"
        },
        columns: [
            {
                // var data = cell.getData();
                title: "Route", field: "route", width: 60, headerFilter: "input", formatter: "link", formatterParams: {
                    url: urlOfRoute,
                    target: "_blank",
                }
            },
            {
                title: "Title", field: "title", width: 250, headerFilter: "input", formatter: "link", formatterParams: {
                    labelField: "title",
                    urlPrefix: "/stationOfInfobox/",
                    target: "_blank",
                }
            },
            {
                title: "From", field: "fromToNameMatched", width: 150, formatter: function (cell, formatterParams, onRendered) {
                    return cell.getValue()[0];
                },
            },
            {
                title: "To", field: "fromToNameMatched", width: 150, formatter: function (cell, formatterParams, onRendered) {
                    return cell.getValue()[1];
                },
            },
            {
                title: "Km", width: 100, field: "fromToKm", width: 60, hozAlign: "right", formatter: function (cell, formatterParams, onRendered) {
                    return cell.getValue().length > 0 ? cell.getValue()[0].toFixed(1) : '';
                },
            },
            {
                title: "Km", width: 100, field: "fromToKm", width: 60, hozAlign: "right", formatter: function (cell, formatterParams, onRendered) {
                    return cell.getValue().length > 0 ? cell.getValue()[1].toFixed(1) : '';
                },
            },
            {
                title: "ResultKind", field: "resultKind.Case", headerFilter: "select", headerFilterParams:{values:["","WikidataFoundInDbData","WikidataNotFoundInDbData","RouteParameterEmpty", "NoDbDataFound", "RouteIsNoPassengerTrain", "StartStopStationsNotFound"]}
            },
            { title: "WikiStops", field: "countWikiStops", width: 100 },
            { title: "DbStopsNotFound", field: "countDbStopsNotFound", width: 100 },
            { title: "Complete", field: "isCompleteDbRoute", width: 100 },
        ],
    });
}

function loadStationOfInfoTable(id, url) {

    new Tabulator(id, {
        ajaxURL: url,
        layout: "fitColumns",
        columns: [
            { title: "Station", field: "name" },
            { title: "Symbols", field: "symbols", width: 150 },
            { title: "Distances", field: "distances", width: 150 }
        ],
    });

}

function loadStationOfRouteTable(id, url) {

    new Tabulator(id, {
        ajaxURL: url,
        layout: "fitColumns",
        columns: [
            { title: "Station", field: "name" },
            { title: "Distances", field: "kms", width: 150 }
        ],
    });

}

function loadStationOfDbWkTable(id, url) {

    new Tabulator(id, {
        ajaxURL: url,
        layout: "fitColumns",
        columns: [
            { title: "DB Station", field: "dbname" },
            {
                title: "Db Distance", field: "dbkm", width: 100, formatter: function (cell, formatterParams, onRendered) {
                    return cell.getValue().toFixed(1);
                },
            },
            { title: "Wk Station", field: "wkname" },
            { title: "Wk Distances", field: "wkkms", width: 100 }
        ],
    });

}