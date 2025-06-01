ARG TARGETPLATFORM

FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS build
COPY publish /build
RUN cp -r /build/${TARGETPLATFORM} /publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS final
RUN wget https://github.com/hez2010/Satori/releases/latest/download/linux_musl_${TARGETPLATFORM}.zip && \
    unzip linux_musl_${TARGETPLATFORM}.zip -d satori && \
    chmod +x satori/* && \
    mv satori/* /usr/share/dotnet/shared/Microsoft.NETCore.App/$(ls /usr/share/dotnet/shared/Microsoft.NETCore.App)/ && \
    rm linux_musl_${TARGETPLATFORM}.zip && rm -rf satori

ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    DOTNET_ReadyToRun=0 \
    LC_ALL=en_US.UTF-8

WORKDIR /app
RUN apk add --update --no-cache wget libpcap icu-data-full icu-libs \
    ca-certificates libgdiplus tzdata krb5-libs && \
    update-ca-certificates

COPY --from=build --chown=$APP_UID:$APP_UID /publish .

EXPOSE 8080

HEALTHCHECK --interval=5m --timeout=3s --start-period=10s --retries=1 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:3000/healthz || exit 1

ENTRYPOINT ["dotnet", "GZCTF.dll"]
