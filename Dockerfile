ARG DOTNET_VERSION=10.0

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build

WORKDIR /src

COPY src/service-requests-service/service-requests-service.csproj src/service-requests-service/
RUN dotnet restore src/service-requests-service/service-requests-service.csproj

COPY src/service-requests-service/ src/service-requests-service/

RUN dotnet publish src/service-requests-service/service-requests-service.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# For production use alpine instead
ARG DOTNET_VERSION=10.0
FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} AS runtime

WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080

EXPOSE 8080

USER $APP_UID

ENTRYPOINT ["dotnet", "service-requests-service.dll"]
