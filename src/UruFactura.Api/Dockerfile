# ── Stage 1: build ────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy global SDK pin so the build uses the same toolchain version
COPY global.json .

# Copy the SDK library project and the file-based API app
COPY src/UruFacturaSDK/ src/UruFacturaSDK/
COPY src/UruFactura.Api/ src/UruFactura.Api/

# Restore the referenced library project first (improves Docker layer caching)
RUN dotnet restore src/UruFacturaSDK/UruFacturaSDK.csproj

# Publish the .NET 10 file-based API
RUN dotnet publish src/UruFactura.Api/Api.cs \
    --configuration Release \
    --output /app/publish

# ── Stage 2: runtime ──────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Cloudflare Containers (and most container runtimes) expect port 8080.
# ASPNETCORE_HTTP_PORTS is the .NET 8+ replacement for ASPNETCORE_URLS.
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Api.dll"]
