FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS build
ARG TARGETPLATFORM
COPY publish /build
RUN cp -r /build/${TARGETPLATFORM} /publish

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS final
WORKDIR /app
COPY --from=build /publish .

EXPOSE 80

RUN apt update && apt install -y wget libpcap0.8 --no-install-recommends && rm -rf /var/lib/apt/lists/*
HEALTHCHECK --interval=5m --timeout=3s --start-period=10s --retries=1 \
  CMD wget --no-verbose --tries=1 --spider http://localhost:80/healthz || exit 1

ENTRYPOINT ["dotnet", "GZCTF.dll"]
