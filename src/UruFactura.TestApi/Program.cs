using System.Text.Json.Serialization.Metadata;
using Scalar.AspNetCore;
using UruFactura.CloudflareApi.Endpoints;
using UruFactura.CloudflareApi.Services;

// ---------------------------------------------------------------------------
// UruFactura.TestApi — Entorno de desarrollo/testing con Scalar UI.
// Reutiliza toda la lógica de UruFactura.CloudflareApi (endpoints, servicios,
// modelos) y agrega explorador interactivo de la API.
// ---------------------------------------------------------------------------

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IUruFacturaClientFactory, TenantClientFactory>();
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.TypeInfoResolverChain.Insert(0, new DefaultJsonTypeInfoResolver()));
builder.Services.AddOpenApi();

var app = builder.Build();

// --- Scalar UI (solo en Development) ---
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(o => o.WithTitle("UruFactura TestApi"));
    app.MapGet("/", () => Results.Redirect("/scalar/v1"))
       .ExcludeFromDescription();
}

// --- Health ---
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTimeOffset.UtcNow }))
   .WithTags("Health")
   .ExcludeFromDescription();

// --- Endpoints (compartidos con CloudflareApi) ---
app.MapCfeEndpoints();
app.MapReporteDiarioEndpoints();
app.MapCaeEndpoints();

app.Run();
