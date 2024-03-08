#!/bin/bash

# requires https://github.com/dotnet-script/dotnet-script
# install by running: 
#     dotnet tool install -g dotnet-script

exec dotnet script --no-cache extract-postgres.csx
