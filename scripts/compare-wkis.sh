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

while read p; do
    dotnet src/WikitextDbComparer/bin/Debug/netcoreapp3.1/WikitextDbComparer.dll -comparetitle  "$p"
done < "${1:-/dev/stdin}"