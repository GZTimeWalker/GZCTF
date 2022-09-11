FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
ARG TARGETPLATFORM
COPY publish /build
RUN cp -r /build/${TARGETPLATFORM} /publish
RUN apt update && \
    apt install -y --no-install-recommends libgdiplus libc6-dev

FROM mcr.microsoft.com/dotnet/aspnet:6.0-alpine AS final
WORKDIR /app
EXPOSE 80
COPY --from=build /usr/lib/libgdiplus.so /usr/lib/gdiplus.dll
COPY --from=build /publish .
ENTRYPOINT ["dotnet", "CTFServer.dll"]
