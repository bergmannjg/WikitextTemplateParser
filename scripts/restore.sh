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

rm -f ${INFILE}
rm -f geo-betriebsstelle_2020.zip
rm -rf geo-betriebsstelle

wget -q http://download-data.deutschebahn.com/static/datasets/geo-betriebsstelle/geo-betriebsstelle_2020.zip
mkdir geo-betriebsstelle
unzip -q geo-betriebsstelle_2020.zip -d geo-betriebsstelle
cp geo-betriebsstelle/Betriebsstellen/CSV/${INFILE} .

rm -f geo-betriebsstelle_2020.zip
rm -rf geo-betriebsstelle

#
# Geo-Streckennetz
#

rm -f strecken.csv
rm -f strecken_nutzung.csv

wget -q http://download-data.deutschebahn.com/static/datasets/geo-strecke/geo-strecke_2020.zip
mkdir geo-strecke
unzip -q geo-strecke_2020.zip -d geo-strecke
cp geo-strecke/Strecken/CSV/strecken.csv .
cp geo-strecke/Strecken/MapInfoRelationen/strecken.MID strecken_nutzung.csv

rm -f geo-strecke_2020.zip
rm -rf geo-strecke

cd ..
cd ..

