#!/usr/bin/env bash
# rebuild comparison of wikitexts

if [ ! -d "./scripts" ]; then
    echo "please run from project directory"
    exit 1
fi
 
dotnet build -c Release src/WikitextRouteDiagramsApp/WikitextRouteDiagramsApp.fsproj
if [ $? -ne 0 ]
then
  echo "error in building project"
  exit 0
fi

PATH2APP=src/WikitextRouteDiagramsApp/bin/Release/net5.0/WikitextRouteDiagramsApp.dll

dotnet ${PATH2APP} -dropCollection DbStationOfRoute
dotnet ${PATH2APP} -dropCollection WkStationOfInfobox
dotnet ${PATH2APP} -dropCollection DbWkStationOfRoute
dotnet ${PATH2APP} -dropCollection WkStationOfRoute
dotnet ${PATH2APP} -dropCollection ResultOfRoute
dotnet ${PATH2APP} -comparetitles

dotnet ${PATH2APP} -dropCollection RouteInfo
dotnet ${PATH2APP} -classify

