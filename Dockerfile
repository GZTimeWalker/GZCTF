FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
RUN apt-get update && \
    apt-get install -y wget && \
    apt-get install -y gnupg2 && \
    wget -qO- https://deb.nodesource.com/setup_16.x | bash - && \
    apt-get install -y build-essential nodejs

WORKDIR /src
COPY ["GZCTF/CTFServer.csproj", "CTFServer/"]
RUN dotnet restore "CTFServer/CTFServer.csproj"
COPY . .
WORKDIR "/src/CTFServer"
RUN dotnet build "CTFServer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CTFServer.csproj" -c Release -o /app/publish -r linux-x64 /p:PublishReadyToRun=true /p:PublishReadyToRunComposite=true /p:PublishTrimmed=true /p:TrimMode=CopyUsed

RUN apt remove -y --auto-remove wget gnupg2 &&\
    apt clean &&\
    rm  -rf /var/lib/apt/lists/* &&\
    npm update caniuse-lite

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CTFServer.dll"]
