#!/usr/bin/env bash
# restore files from Open-Data-Portal of Deutsche Bahn

if [ ! -d "./scripts" ]; then
    echo "please run from project directory"
    exit 1
fi

if [ ! -d "./dbdata" ]; then
    mkdir dbdata
fi

if [ ! -d "./dbdata/original" ]; then
    mkdir dbdata/original
fi

cd dbdata/original

#
# Geo-Betriebsstelle
#

INFILE=betriebsstellen_open_data.csv 
OUTFILE=betriebsstellen_open_data.json

rm -f ${INFILE}
rm -f ${OUTFILE}
rm -f geo-betriebsstelle_2020.zip
rm -rf geo-betriebsstelle

wget -q http://download-data.deutschebahn.com/static/datasets/geo-betriebsstelle/geo-betriebsstelle_2020.zip
unzip -q geo-betriebsstelle_2020.zip -d geo-betriebsstelle
iconv -f 852 geo-betriebsstelle/Betriebsstellen/CSV/${INFILE} | npx csv2json -d -s "," > ${OUTFILE}

# rm inconsistency in data
sed -i -e 's/BREITE":""/BREITE":0/' ${OUTFILE}

rm -f geo-betriebsstelle_2020.zip
rm -rf geo-betriebsstelle

cd ..
cd ..

