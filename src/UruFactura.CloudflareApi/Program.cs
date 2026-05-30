using System.Text.Json.Serialization.Metadata;
using UruFactura.CloudflareApi.Endpoints;
using UruFactura.CloudflareApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IUruFacturaClientFactory, TenantClientFactory>();
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.TypeInfoResolverChain.Insert(0, new DefaultJsonTypeInfoResolver()));
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTimeOffset.UtcNow }))
   .WithTags("Health")
   .ExcludeFromDescription();

app.MapCfeEndpoints();
app.MapReporteDiarioEndpoints();
app.MapCaeEndpoints();

app.Run();

