# docker build --build-arg LINES=200 -t wikitext .
# docker run -p 5000:5000 -it wikitext

FROM mcr.microsoft.com/dotnet/sdk:5.0-alpine

ARG LINES=20000

RUN apk add git bash

WORKDIR /usr/src/apps

RUN git clone https://github.com/bergmannjg/wikitext-template-parser.git

WORKDIR /usr/src/apps/wikitext-template-parser

RUN mkdir dump

RUN source ./scripts/restore.sh

RUN source ./scripts/rebuild.sh $LINES

RUN ./scripts/compare-wkis.sh

RUN dotnet build ./src/ResultsViewer/ResultsViewer.fsproj

WORKDIR /usr/src/apps/wikitext-template-parser/src/ResultsViewer

RUN ln -s ../../results.db results.db

ENV ASPNETCORE_URLS=http://0.0.0.0:5000

ENTRYPOINT ["dotnet", "./bin/Debug/net5.0/ResultsViewer.dll"]