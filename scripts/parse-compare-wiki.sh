#!/usr/bin/env bash

if [ ! -d "./scripts" ]; then
    echo "please run from project directory"
    exit 1
fi

rm -f ./cache/"$1".txt

dotnet run --project src/WikitextTemplateParser/WikitextTemplateParser.fsproj -parsetitle  "$1"

dotnet run --project src/WikitextDbComparer/WikitextDbComparer.fsproj -comparetitle  "$1"
