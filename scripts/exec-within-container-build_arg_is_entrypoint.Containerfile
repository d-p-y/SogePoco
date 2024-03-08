FROM ubuntu:22.04
LABEL VERSION="20230928"

RUN     apt-get update && \
	apt-get -y upgrade && \
	apt-get install -y git libssl3 libicu70 ca-certificates dotnet-sdk-7.0
RUN apt-get install -y dotnet-sdk-6.0
RUN apt-get install -y sqsh postgresql-client-14
RUN dotnet tool install -g dotnet-script
RUN apt-get install -y unzip
ARG run=/bin/bash

ENV PATH "${PATH}:/root/.dotnet/tools"

ENV RUN $run
RUN echo "$RUN" > /run.sh
RUN chmod a+x /run.sh
RUN cat /run.sh

ENTRYPOINT "/run.sh"

