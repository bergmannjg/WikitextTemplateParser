#!/usr/bin/env bash
# rebuild all and load, parse and compare wikitexts

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

if [ ! -f "./titles.txt" ]; then
  dotnet ${PATH2APP} -showtitles > ./titles.txt
fi

if [ ! -f "./stations.txt" ]; then
  dotnet ${PATH2APP} -getStationLinks > ./stations.txt
fi

if [ $# -eq 1 ]; then
  LINES="$1"
  cp -f ./titles.txt ./titles.bak.txt
  cat  ./titles.bak.txt | head -n $LINES > ./titles.txt
  cp -f ./stations.txt ./stations.bak.txt
  cat  ./stations.bak.txt | head -n $LINES > ./stations.txt
fi

dotnet ${PATH2APP} -dropCollection Wikitext
dotnet ${PATH2APP} -loadroutes ./titles.txt

dotnet ${PATH2APP} -dropCollection Templates
dotnet ${PATH2APP} -parseroutes

dotnet ${PATH2APP} -dropCollection WikitextOfStop
dotnet ${PATH2APP} -loadstops ./stations.txt

dotnet ${PATH2APP} -dropCollection TemplatesOfStop
dotnet ${PATH2APP} -parsestops

dotnet ${PATH2APP} -dropCollection DbStationOfRoute
dotnet ${PATH2APP} -dropCollection WkStationOfInfobox
dotnet ${PATH2APP} -dropCollection DbWkStationOfRoute
dotnet ${PATH2APP} -dropCollection WkStationOfRoute
dotnet ${PATH2APP} -dropCollection ResultOfRoute
dotnet ${PATH2APP} -comparetitles

dotnet ${PATH2APP} -dropCollection RouteInfo
dotnet ${PATH2APP} -classify

if [ $# -eq 1 ]; then
  mv -f ./titles.bak.txt ./titles.txt
  mv -f ./stations.bak.txt ./stations.txt 
fi