@echo off
REM  requires https://github.com/dotnet-script/dotnet-script
REM  install by running: 
REM      dotnet tool install -g dotnet-script

REM https://github.com/dotnet-script/dotnet-script/issues/641
dotnet script --no-cache --isolated-load-context extract-sqlite.csx
