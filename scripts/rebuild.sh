#!/usr/bin/env bash
# rebuild all and load, parse and compare wikitexts

if [ ! -d "./scripts" ]; then
    echo "please run from project directory"
    exit 1
fi
 
dotnet build -c Release src/WikitextTemplateParser/WikitextTemplateParser.fsproj
if [ $? -ne 0 ]
then
  echo "error in building project"
  exit 0
fi

dotnet build -c Release src/WikitextDbComparer/WikitextDbComparer.fsproj
if [ $? -ne 0 ]
then
  echo "error in building project"
  exit 0
fi

PARSER=src/WikitextTemplateParser/bin/Release/net5.0/WikitextTemplateParser.dll
COMPARER=src/WikitextDbComparer/bin/Release/net5.0/WikitextDbComparer.dll

if [ ! -f "./titles.txt" ]; then
  dotnet ${PARSER} -showtitles > ./titles.txt
fi

if [ ! -f "./stations.txt" ]; then
  dotnet ${COMPARER} -getStationLinks > ./stations.txt
fi

if [ $# -eq 1 ]; then
  LINES="$1"
  cp -f ./titles.txt ./titles.bak.txt
  cat  ./titles.bak.txt | head -n $LINES > ./titles.txt
  cp -f ./stations.txt ./stations.bak.txt
  cat  ./stations.bak.txt | head -n $LINES > ./stations.txt
fi

dotnet ${COMPARER} -dropCollection Wikitext
dotnet ${PARSER} -loadroutes ./titles.txt

dotnet ${COMPARER} -dropCollection Templates
dotnet ${PARSER} -parseroutes

dotnet ${COMPARER} -dropCollection WikitextOfStop
dotnet ${PARSER} -loadstops ./stations.txt

dotnet ${COMPARER} -dropCollection TemplatesOfStop
dotnet ${PARSER} -parsestops

dotnet ${COMPARER} -dropCollection DbStationOfRoute
dotnet ${COMPARER} -dropCollection WkStationOfInfobox
dotnet ${COMPARER} -dropCollection DbWkStationOfRoute
dotnet ${COMPARER} -dropCollection WkStationOfRoute
dotnet ${COMPARER} -dropCollection ResultOfRoute
dotnet ${COMPARER} -comparetitles

dotnet ${COMPARER} -dropCollection RouteInfo
dotnet ${COMPARER} -classify

if [ $# -eq 1 ]; then
  mv -f ./titles.bak.txt ./titles.txt
  mv -f ./stations.bak.txt ./stations.txt 
fi