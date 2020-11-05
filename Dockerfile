# docker build --build-arg LINES=2000 -t wikitext .
# docker run -p 5000:5000 -it wikitext

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-alpine

ARG LINES=100

RUN apk add git bash

WORKDIR /usr/src/apps

RUN git clone https://github.com/bergmannjg/wikitext-template-parser.git

WORKDIR /usr/src/apps/wikitext-template-parser

RUN mkdir cache dump wikidata

COPY ./dbdata dbdata/

RUN cp titles.txt titles.orig.txt

RUN head -n ${LINES} titles.orig.txt > titles.txt

RUN ./scripts/parse-wikis.sh

RUN ./scripts/compare-wkis.sh > ./dump/results.json

RUN dotnet build ./src/ResultsViewer/ResultsViewer.fsproj

WORKDIR /usr/src/apps/wikitext-template-parser/src/ResultsViewer

RUN ln -s ../../dump dump

ENV ASPNETCORE_URLS=http://0.0.0.0:5000

ENTRYPOINT ["dotnet", "./bin/Debug/netcoreapp3.1/ResultsViewer.dll"]