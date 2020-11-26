#!/usr/bin/env bash
# load and parse stations from file stations.txt

if [ ! -d "./scripts" ]; then
    echo "please run from project directory"
    exit 1
fi
 
dotnet build -c Release src/WikitextTemplateParser/WikitextTemplateParser.fsproj &> /dev/null
if [ $? -ne 0 ]
then
  echo "error in building project"
  exit 0
fi

if [ ! -f "./stations.txt" ]; then
  dotnet run --project src/WikitextDbComparer/WikitextDbComparer.fsproj -getStationLinks > ./stations.txt
fi

LINESOFFILE=$(wc -l < ./stations.txt)

STARTLINE=0
LINES=$LINESOFFILE

if [ $# -eq 2 ]; then
  STARTLINE="$1"
  LINES="$2"
fi

if [ $# -eq 0 -o $LINES -ge $LINESOFFILE ]; then
  dotnet run --project src/WikitextDbComparer/WikitextDbComparer.fsproj -dropCollection WikitextOfStop
fi

dotnet src/WikitextTemplateParser/bin/Release/net5.0/WikitextTemplateParser.dll -loadstops ./stations.txt $STARTLINE $LINES

if [ $# -eq 0 -o $LINES -ge $LINESOFFILE ]; then
  dotnet run --project src/WikitextDbComparer/WikitextDbComparer.fsproj -dropCollection TemplatesOfStop
  dotnet src/WikitextTemplateParser/bin/Release/net5.0/WikitextTemplateParser.dll -parsestops
fi
