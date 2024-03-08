@echo off
REM  requires https://github.com/dotnet-script/dotnet-script
REM  install by running: 
REM      dotnet tool install -g dotnet-script

exec dotnet script --no-cache extract-sqlserver.csx
