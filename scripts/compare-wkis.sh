#!/usr/bin/env bash

if [ ! -d "./scripts" ]; then
    echo "please run from project directory"
    exit 1
fi

dotnet build -c Release src/WikitextDbComparer/WikitextDbComparer.fsproj > /dev/null
if [ $? -ne 0 ]
then
  echo "error in building project"
  exit 0
fi

LINES=2000

if [ $# -eq 1 ]; then
  LINES="$1"
fi

if [ $? -eq 0 ]; then
  dotnet src/WikitextDbComparer/bin/Debug/net5.0/WikitextDbComparer.dll -dropCollection DbStationOfRoute
  dotnet src/WikitextDbComparer/bin/Debug/net5.0/WikitextDbComparer.dll -dropCollection WkStationOfInfobox
  dotnet src/WikitextDbComparer/bin/Debug/net5.0/WikitextDbComparer.dll -dropCollection DbWkStationOfRoute
  dotnet src/WikitextDbComparer/bin/Debug/net5.0/WikitextDbComparer.dll -dropCollection WkStationOfRoute
  dotnet src/WikitextDbComparer/bin/Debug/net5.0/WikitextDbComparer.dll -dropCollection ResultOfRoute
fi

dotnet src/WikitextDbComparer/bin/Release/net5.0/WikitextDbComparer.dll -comparetitles $LINES
