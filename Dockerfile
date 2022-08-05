FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build

ARG TIMESTAMP
ARG GIT_SHA
ARG GIT_NAME
ENV VITE_APP_BUILD_TIMESTAMP=$TIMESTAMP
ENV VITE_APP_GIT_SHA=$GIT_SHA
ENV VITE_APP_GIT_NAME=$GIT_NAME

RUN apt update && \
    apt install -y wget && \
    apt install -y gnupg2 && \
    wget -qO- https://deb.nodesource.com/setup_18.x | bash - && \
    apt install -y build-essential nodejs

COPY ["GZCTF", "/src/GZCTF/"]
WORKDIR "/src/GZCTF"
RUN dotnet restore "CTFServer.csproj"
RUN dotnet build "CTFServer.csproj" -c Release -o /app/build --no-restore

FROM build AS publish
RUN dotnet publish "CTFServer.csproj" -c Release -o /app/publish -r linux-x64 --no-self-contained /p:PublishReadyToRun=true /p:UseNpm=true

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS final
WORKDIR /app
EXPOSE 80
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CTFServer.dll"]
