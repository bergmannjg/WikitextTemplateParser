#!/usr/bin/env bash

if [ ! -d "./scripts" ]; then
    echo "please run from project directory"
    exit 1
fi
 
dotnet build -c Release src/WikitextTemplateParser/WikitextTemplateParser.fsproj > /dev/null
if [ $? -ne 0 ]
then
  echo "error in building project"
  exit 0
fi

if [ ! -f "./titles.txt" ]; then
  dotnet src/WikitextTemplateParser/bin/Release/net5.0/WikitextTemplateParser.dll -showtitles > ./titles.txt
fi

LINESOFFILE=$(wc -l < ./stations.txt)

STARTLINE=0
LINES=$LINESOFFILE

if [ $# -eq 2 ]; then
  STARTLINE="$1"
  LINES="$2"
fi

if [ $# -eq 0 -o $LINES -ge $LINESOFFILE ]; then
  dotnet run --project src/WikitextDbComparer/WikitextDbComparer.fsproj -dropCollection Wikitext
fi

dotnet src/WikitextTemplateParser/bin/Release/net5.0/WikitextTemplateParser.dll -loadroutes ./titles.txt $STARTLINE $LINES

if [ $# -eq 0 -o $LINES -ge $LINESOFFILE ]; then
  dotnet run --project src/WikitextDbComparer/WikitextDbComparer.fsproj -dropCollection Templates
  dotnet src/WikitextTemplateParser/bin/Release/net5.0/WikitextTemplateParser.dll -parseroutes
fi
