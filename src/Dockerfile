FROM mcr.microsoft.com/dotnet/sdk:8.0-jammy AS build

ARG TIMESTAMP
ARG GIT_SHA
ARG GIT_NAME
ENV VITE_APP_BUILD_TIMESTAMP=$TIMESTAMP \
    VITE_APP_GIT_SHA=$GIT_SHA \
    VITE_APP_GIT_NAME=$GIT_NAME

RUN apt update && apt install -y wget gnupg2 libpcap0.8 && \
    wget -qO- https://deb.nodesource.com/setup_20.x | bash - && \
    apt install -y build-essential nodejs

RUN npm i -g pnpm

COPY [".", "/src"]

WORKDIR "/src/GZCTF"
RUN dotnet build "GZCTF.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "GZCTF.csproj" -c Release -o /app/publish -r linux-x64 --no-self-contained /p:PublishReadyToRun=true

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final

ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    LC_ALL=en_US.UTF-8

WORKDIR /app
EXPOSE 8080
RUN apk add --update --no-cache wget libpcap icu-data-full icu-libs ca-certificates libgdiplus tzdata && \
    update-ca-certificates

COPY --from=publish /app/publish .

HEALTHCHECK --interval=5m --timeout=3s --start-period=10s --retries=1 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/healthz || exit 1

ENTRYPOINT ["dotnet", "GZCTF.dll"]
