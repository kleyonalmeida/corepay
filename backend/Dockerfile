FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY backend/src/BuildingBlocks/BuildingBlocks.csproj backend/src/BuildingBlocks/
COPY backend/src/Core/Core.csproj backend/src/Core/
COPY backend/src/Infrastructure/Infrastructure.csproj backend/src/Infrastructure/
COPY backend/src/WebAPI/WebAPI.csproj backend/src/WebAPI/

RUN dotnet restore backend/src/WebAPI/WebAPI.csproj

COPY backend/src/ backend/src/

RUN dotnet publish backend/src/WebAPI/WebAPI.csproj \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "WebAPI.dll"]
