FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
RUN apt update && \
    apt install -y libgdiplus libc6-dev

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS final
WORKDIR /app
EXPOSE 80
COPY --from=build /usr/lib/libgdiplus.so /usr/lib/gdiplus.dll
COPY publish .
ENTRYPOINT ["dotnet", "CTFServer.dll"]
