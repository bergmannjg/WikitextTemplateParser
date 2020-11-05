#!/usr/bin/env bash

if [ ! -d "./scripts" ]; then
    echo "please run from project directory"
    exit 1
fi

rm -f ./wikidata/*.*
 
dotnet build src/WikitextTemplateParser/WikitextTemplateParser.fsproj > /dev/null
if [ $? -ne 0 ]
then
  echo "error in building project"
  exit 0
fi

while read p; do
    dotnet src/WikitextTemplateParser/bin/Debug/netcoreapp3.1/WikitextTemplateParser.dll -parsetitle  "$p"
done <./titles.txt