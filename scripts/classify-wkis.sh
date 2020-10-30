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

rm -f /tmp/compare-wiki.*

tmpfile=$(mktemp /tmp/compare-wiki.XXXXXX)

while read p; do
    dotnet src/WikitextDbComparer/bin/Debug/netcoreapp3.1/WikitextDbComparer.dll -classify  "$p" >> "$tmpfile"
done < <(head -2000 ./titles.txt)

sed -i -e '$ ! s/$/,/' "$tmpfile"

echo "[" 
cat "$tmpfile"
echo "]"

rm "$tmpfile"
