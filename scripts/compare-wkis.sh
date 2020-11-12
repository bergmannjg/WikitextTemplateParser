#!/usr/bin/env bash

if [ ! -d "./scripts" ]; then
    echo "please run from project directory"
    exit 1
fi

if [ ! -d "./dump" ]; then
    echo "directory dump not found"
    exit 1
fi

LINES=2000

if [ $# -eq 1 ]; then
  LINES="$1"
fi

dotnet build -c Release src/WikitextDbComparer/WikitextDbComparer.fsproj > /dev/null
if [ $? -ne 0 ]
then
  echo "error in building project"
  exit 0
fi

rm -f /tmp/compare-wiki.*

tmpfile=$(mktemp /tmp/compare-wiki.XXXXXX)

while read p; do
    dotnet src/WikitextDbComparer/bin/Release/net5.0/WikitextDbComparer.dll -comparetitle  "$p" >> "$tmpfile"
done < <(head -n $LINES ./titles.txt)

sed -i -e '$ ! s/$/,/' "$tmpfile"

OUTFILE=./dump/results.json

echo "[" > $OUTFILE
cat "$tmpfile" >>  $OUTFILE
echo "]" >>  $OUTFILE

rm "$tmpfile"
