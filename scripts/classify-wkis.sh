#!/usr/bin/env bash

if [ ! -d "./scripts" ]; then
    echo "please run from project directory"
    exit 1
fi

dotnet build src/WikitextDbComparer/WikitextDbComparer.fsproj > /dev/null
if [ $? -ne 0 ]
then
  echo "error in building project"
  exit 0
fi

dotnet src/WikitextDbComparer/bin/Debug/net5.0/WikitextDbComparer.dll -dropCollection RouteInfo

while read p; do
    dotnet src/WikitextDbComparer/bin/Debug/net5.0/WikitextDbComparer.dll -classify  "$p"
done < <(head -2000 ./titles.txt)
