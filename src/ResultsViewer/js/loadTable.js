
function loadResultsTable(tableId, statusElementId, url) {

    let urlOfRoute = (cell) => {
        return "/stationOfDbWk/" + cell.getData().title + '/' + cell.getValue();
    }

    let table = new Tabulator(tableId, {
        ajaxURL: url,
        layout: "fitColumns",
        pagination: "local",
        paginationSize: 20,
        dataFiltered: function (filters, rows) {
            //filters - array of filters currently applied
            //rows - array of row components that pass the filters
            statusElementId.textContent = rows.length + " rows selected"
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
                title: "ResultKind", field: "resultKind.Case", headerFilter: "select", headerFilterParams: { values: ["", "WikidataFoundInDbData", "WikidataNotFoundInDbData", "RouteParameterEmpty", "NoDbDataFoundWithRailwayGuide", "NoDbDataFoundWithoutRailwayGuide", "RouteIsNoPassengerTrain", "StartStopStationsNotFound", "RouteIsShutdown", "RouteParameterNotParsed"] }
            },
            { title: "WikiStops", field: "countWikiStops", width: 100 },
            { title: "DbFound", field: "countDbStopsFound", width: 100 },
            { title: "DbNotFound", field: "countDbStopsNotFound", width: 100 },
            // { title: "railwayGuide", field: "railwayGuide", width: 80 },
            { title: "RoutesInTitle", field: "routesInTitle", width: 80 },
            { title: "Complete", field: "isCompleteDbRoute", width: 80 },
        ],
    });

    function customFilter(data){
        return data.resultKind.Case == 'WikidataNotFoundInDbData' 
            && !(
                data.countDbStopsNotFound <= 4 && (data.countDbStopsFound - data.countDbStopsNotFound >= 1)
                || data.countDbStopsNotFound == 5 && (data.countDbStopsFound - data.countDbStopsNotFound >= 5)
                || data.countDbStopsNotFound == 6 && (data.countDbStopsFound - data.countDbStopsNotFound >= 6)
                );
    }
    // table.setFilter(customFilter);
    return table;
}

function loadRouteInfosTable(tableId, statusElementId, url) {

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
            statusElementId.textContent = rows.length + " rows selected"
        },
        columns: [
            {
                // var data = cell.getData();
                title: "Route", field: "nummer", width: 60, headerFilter: "input", formatter: "link", formatterParams: {
                    url: urlOfRoute,
                    target: "_blank",
                }
            },
            {
                title: "Title", field: "title", width: 250, headerFilter: "input"
            },
            {
                title: "From", field: "von", width: 150
            },
            {
                title: "To", field: "bis", width: 150
            },
            {
                title: "RoutenameKind", field: "routenameKind.Case", width: 150, headerFilter: "select", headerFilterParams: { values: ["", "Empty", "EmptyWithIgnored", "SmallFormat", "Parenthesis", "Text", "Unmatched"] }
            },
            { title: "Text", field: "searchstring", headerFilter: "input", formatter: "textarea" },
        ],
    });
}

function loadStopsTable(tableId, statusElementId, url) {

    let urlOfWikitext = (cell) => {
        return "/data/WikitextOfStop/" + cell.getValue();
    }

    new Tabulator(tableId, {
        ajaxURL: url,
        layout: "fitColumns",
        pagination: "local",
        paginationSize: 20,
        dataFiltered: function (filters, rows) {
            //filters - array of filters currently applied
            //rows - array of row components that pass the filters
            statusElementId.textContent = rows.length + " rows selected"
        },
        columns: [
            {
                title: "Title", field: "title", headerFilter: "input", formatter: "link", formatterParams: {
                    url: urlOfWikitext,
                    target: "_blank",
                }
            }
        ],
    });
}

function loadStationOfInfoTable(id, url) {

    let urlOfWikitext = (cell) => {
        return "/data/WikitextOfStop/" + cell.getValue();
    }

    new Tabulator(id, {
        ajaxURL: url,
        layout: "fitColumns",
        columns: [
            { title: "Station", headerFilter: "input", field: "name" },
            {
                title: "Link", headerFilter: "input", field: "link", formatter: "link", formatterParams: {
                    url: urlOfWikitext,
                    target: "_blank",
                }
            },
            { title: "DS100", field: "shortname", width: 80 },
            { title: "Symbols", field: "symbols", width: 150 },
            { title: "Distances", field: "distances", width: 80 }
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

function loadDbStationOfRouteTable(id, url) {

    new Tabulator(id, {
        ajaxURL: url,
        layout: "fitColumns",
        columns: [
            { title: "Station", field: "name" },
            { title: "Distance", field: "km", width: 150 },
            { title: "Art", field: "STELLE_ART", width: 150 }
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
                title: "Db Distance", field: "dbkm", width: 90, formatter: function (cell, formatterParams, onRendered) {
                    return cell.getValue().toFixed(1);
                },
            },
            { title: "Wk Station", field: "wkname" },
            { title: "Wk Distances", field: "wkkms", width: 90 },
            { title: "Match", field: "matchkind.Case", width: 120 }
        ],
    });

}

