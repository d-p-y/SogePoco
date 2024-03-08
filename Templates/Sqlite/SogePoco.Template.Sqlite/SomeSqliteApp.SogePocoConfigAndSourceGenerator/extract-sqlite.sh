#!/bin/bash

# requires https://github.com/dotnet-script/dotnet-script
# install by running: 
#     dotnet tool install -g dotnet-script

#https://github.com/dotnet-script/dotnet-script/issues/641
exec dotnet script --no-cache --isolated-load-context extract-sqlite.csx
