# ── Build stage ─────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /repo

# Copy project files first for layer-cache efficiency
COPY src/UruFacturaSDK/UruFacturaSDK.csproj           src/UruFacturaSDK/
COPY uerp/UruErpApp.Api/UruErpApp.Api.csproj          uerp/UruErpApp.Api/

RUN dotnet restore uerp/UruErpApp.Api/UruErpApp.Api.csproj

# Copy full source
COPY src/ src/
COPY uerp/UruErpApp.Api/ uerp/UruErpApp.Api/

RUN dotnet publish uerp/UruErpApp.Api/UruErpApp.Api.csproj \
    --no-restore \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

# ── Runtime stage ────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Railway injects PORT; ASP.NET Core reads ASPNETCORE_URLS or --urls
ENV ASPNETCORE_URLS=http://+:${PORT:-8080}

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "UruErpApp.Api.dll"]
