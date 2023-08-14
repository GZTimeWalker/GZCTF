FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build

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

RUN npm i -g pnpm

COPY ["GZCTF", "/src/GZCTF/"]
WORKDIR "/src/GZCTF"
RUN dotnet restore "GZCTF.csproj"
RUN dotnet build "GZCTF.csproj" -c Release -o /app/build --no-restore

FROM build AS publish
RUN dotnet publish "GZCTF.csproj" -c Release -o /app/publish -r linux-x64 --no-self-contained /p:PublishReadyToRun=true

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS final
WORKDIR /app
EXPOSE 80
COPY --from=build /usr/bin/wget /usr/bin/wget
COPY --from=publish /app/publish .

HEALTHCHECK --interval=5m --timeout=3s --start-period=10s --retries=1 \
  CMD wget --no-verbose --tries=1 --spider http://localhost:80/healthz || exit 1

ENTRYPOINT ["dotnet", "GZCTF.dll"]
