#!/usr/bin/env bash

if [ ! -d "./scripts" ]; then
    echo "please run from project directory"
    exit 1
fi

while read p; do
    dotnet src/WikitextTemplateParser/bin/Debug/netcoreapp3.1/WikitextTemplateParser.dll -parsetitle  "$p"
done <./titles.txt