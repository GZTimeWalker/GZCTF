FROM mcr.microsoft.com/dotnet/aspnet:6.0-alpine AS build
ARG TARGETPLATFORM
COPY publish /build
RUN cp -r /build/${TARGETPLATFORM} /publish

FROM mcr.microsoft.com/dotnet/aspnet:6.0-alpine AS final
WORKDIR /app
COPY --from=build /publish .

EXPOSE 80
ENTRYPOINT ["dotnet", "CTFServer.dll"]
