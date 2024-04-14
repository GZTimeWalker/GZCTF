FROM mcr.microsoft.com/dotnet/aspnet:8.0-jammy AS build
ARG TARGETPLATFORM
COPY publish /build
RUN cp -r /build/${TARGETPLATFORM} /publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final
WORKDIR /app
COPY --from=build /publish .

EXPOSE 8080

RUN apk add --no-cache wget libpcap

HEALTHCHECK --interval=5m --timeout=3s --start-period=10s --retries=1 \
  CMD wget --no-verbose --tries=1 --spider http://localhost:8080/healthz || exit 1

ENTRYPOINT ["dotnet", "GZCTF.dll"]
