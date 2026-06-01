# ── Stage 1: build ────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy the SDK library project and the Cloudflare API project
COPY src/UruFactura/ src/UruFactura/
COPY src/UruFactura.CloudflareApi/ src/UruFactura.CloudflareApi/

# Restore dependencies
RUN dotnet restore src/UruFactura.CloudflareApi/UruFactura.CloudflareApi.csproj

# Publish
RUN dotnet publish src/UruFactura.CloudflareApi/UruFactura.CloudflareApi.csproj \
    --no-restore \
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

ENTRYPOINT ["dotnet", "UruFactura.CloudflareApi.dll"]
