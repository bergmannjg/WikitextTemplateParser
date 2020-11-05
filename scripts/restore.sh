#!/usr/bin/env bash
# restore files from Open-Data-Portal of Deutsche Bahn
# uses npx csv2json, install with 'sudo apt install npm'

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

#
# Geo-Streckennetz
#

rm -f strecken_nutzung.json

wget -q http://download-data.deutschebahn.com/static/datasets/geo-strecke/geo-strecke_2020.zip
unzip -q geo-strecke_2020.zip -d geo-strecke

tempfile=$(mktemp)

echo mifcode,strecke_nr,richtung,laenge,von_km_i,bis_km_i,von_km_l,bis_km_l,elektrifizierung,bahnnutzung,geschwindigkeit,strecke_kurzn,gleisanzahl,bahnart,kmspru_typ_anf,kmspru_typ_end > ${tempfile}
iconv -f WINDOWS-1252 geo-strecke/Strecken/MapInfoRelationen/strecken.MID >> ${tempfile}
npx csv2json -d -s "," <  ${tempfile} > strecken_nutzung.json

rm -f geo-strecke_2020.zip
rm -f ${tempfile}
rm -rf geo-strecke

cd ..
cd ..

