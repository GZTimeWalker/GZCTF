FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS build
ARG TARGETPLATFORM
COPY publish /build
RUN cp -r /build/${TARGETPLATFORM} /publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final
ARG TARGETPLATFORM
RUN ARCH=$(echo ${TARGETPLATFORM} | cut -d'/' -f2) && \
    wget https://github.com/hez2010/Satori/releases/latest/download/linux_musl_${ARCH}.zip && \
    unzip linux_musl_${ARCH}.zip -d satori && \
    chmod +x satori/* && \
    mv satori/* /usr/share/dotnet/shared/Microsoft.NETCore.App/$(ls /usr/share/dotnet/shared/Microsoft.NETCore.App)/ && \
    rm linux_musl_${ARCH}.zip && rm -rf satori

ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
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
