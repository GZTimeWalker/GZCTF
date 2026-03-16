#!/bin/bash
cd "$(dirname "$0")"
set -xe

DOTNET=dotnet
if command -v dotnet.exe 2>&1 >/dev/null && ! command -v dotnet 2>&1 >/dev/null; then
	DOTNET=dotnet.exe
fi

$DOTNET build "GZCTF.csproj" -c Release -o build
$DOTNET publish "GZCTF.csproj" -c Release -o publish/linux/amd64 -r linux-x64 --no-self-contained /p:PublishReadyToRun=true
docker build -t gztime/gzctf:latest .
