FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS build
ARG TARGETPLATFORM
COPY publish /build
RUN cp -r /build/${TARGETPLATFORM} /publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final

ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    LC_ALL=en_US.UTF-8

WORKDIR /app
EXPOSE 8080
RUN apk add --update --no-cache wget libpcap icu-data-full icu-libs ca-certificates libgdiplus tzdata && \
    update-ca-certificates

COPY --from=build /publish .

HEALTHCHECK --interval=5m --timeout=3s --start-period=10s --retries=1 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/healthz || exit 1

ENTRYPOINT ["dotnet", "GZCTF.dll"]
