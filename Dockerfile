# docker build --build-arg LINES=2000 -t wikitext .
# docker run -p 5000:5000 -it wikitext

FROM mcr.microsoft.com/dotnet/sdk:5.0-alpine

ARG LINES=100

RUN apk add git bash

WORKDIR /usr/src/apps

RUN git clone https://github.com/bergmannjg/wikitext-template-parser.git

WORKDIR /usr/src/apps/wikitext-template-parser

RUN mkdir cache dump wikidata

RUN source ./scripts/restore.sh

RUN cp titles.txt titles.orig.txt

RUN head -n ${LINES} titles.orig.txt > titles.txt

RUN ./scripts/parse-wikis.sh

RUN ./scripts/compare-wkis.sh

RUN dotnet build ./src/ResultsViewer/ResultsViewer.fsproj

WORKDIR /usr/src/apps/wikitext-template-parser/src/ResultsViewer

RUN ln -s ../../dump dump

ENV ASPNETCORE_URLS=http://0.0.0.0:5000

ENTRYPOINT ["dotnet", "./bin/Debug/netcoreapp3.1/ResultsViewer.dll"]