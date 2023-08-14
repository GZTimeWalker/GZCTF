FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS build
ARG TARGETPLATFORM
COPY publish /build
RUN apt update && \
    apt install -y wget --no-install-recommends && \
    cp -r /build/${TARGETPLATFORM} /publish

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS final
WORKDIR /app
COPY --from=build /publish .
COPY --from=build /usr/bin/wget /usr/bin/wget

EXPOSE 80

HEALTHCHECK --interval=5m --timeout=3s --start-period=10s --retries=1 \
  CMD wget --no-verbose --tries=1 --spider http://localhost:80/healthz || exit 1

ENTRYPOINT ["dotnet", "GZCTF.dll"]
