#!/usr/bin/env bash
# restore files from Open-Data-Portal of Deutsche Bahn
# csv2json needs rust and cargo, curl --proto '=https' --tlsv1.2 -sSf https://sh.rustup.rs | sh

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

tempfile=$(mktemp)

wget -q http://download-data.deutschebahn.com/static/datasets/geo-betriebsstelle/geo-betriebsstelle_2020.zip
mkdir geo-betriebsstelle
unzip -q geo-betriebsstelle_2020.zip -d geo-betriebsstelle 
iconv -f 852 geo-betriebsstelle/Betriebsstellen/CSV/${INFILE} > ${tempfile} 
csv2json --in ${tempfile} > ${OUTFILE}

# rm inconsistency in data
sed -i -e 's/BREITE":""/BREITE":0/' ${OUTFILE}

ls -al ${OUTFILE}

rm -f geo-betriebsstelle_2020.zip
rm -rf geo-betriebsstelle
rm -f ${tempfile}

#
# Geo-Streckennetz
#

rm -f strecken_nutzung.json

wget -q http://download-data.deutschebahn.com/static/datasets/geo-strecke/geo-strecke_2020.zip
mkdir geo-strecke
unzip -q geo-strecke_2020.zip -d geo-strecke

echo mifcode,strecke_nr,richtung,laenge,von_km_i,bis_km_i,von_km_l,bis_km_l,elektrifizierung,bahnnutzung,geschwindigkeit,strecke_kurzn,gleisanzahl,bahnart,kmspru_typ_anf,kmspru_typ_end > ${tempfile}
iconv -f 852 geo-strecke/Strecken/MapInfoRelationen/strecken.MID >> ${tempfile}
csv2json --in ${tempfile} > strecken_nutzung.json

ls -al strecken_nutzung.json

rm -f geo-strecke_2020.zip
rm -f ${tempfile}
rm -rf geo-strecke

cd ..
cd ..

