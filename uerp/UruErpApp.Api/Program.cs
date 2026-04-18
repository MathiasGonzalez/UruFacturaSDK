using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using UruErpApp.Api;
using UruErpApp.Api.Auth;
using UruErpApp.Api.Models;
using UruErpApp.Api.Services;
using UruFacturaSDK;
using UruFacturaSDK.Configuration;
using UruFacturaSDK.Enums;
using UruFacturaSDK.Models;

var builder = WebApplication.CreateBuilder(args);

// ── Database ────────────────────────────────────────────────────────────────
// Railway provides DATABASE_URL; Aspire provides the named connection string.
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
if (!string.IsNullOrEmpty(databaseUrl))
{
    var uri = new Uri(databaseUrl);
    var userInfo = uri.UserInfo.Split(':', 2);
    var connStr = $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=true";
    builder.Services.AddDbContext<AppDbContext>(opts => opts.UseNpgsql(connStr));
}
else
{
    builder.AddNpgsqlDbContext<AppDbContext>("saasdb");
}

// ── Auth ────────────────────────────────────────────────────────────────────
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret must be configured.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = false,
            ValidateAudience = false,
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddScoped<JwtService>();

// ── Cloudflare R2 ───────────────────────────────────────────────────────────
// R2 is optional; if CloudflareR2:AccountId is missing the service won't be
// registered and R2 uploads will be skipped.
var r2Configured = !string.IsNullOrWhiteSpace(builder.Configuration["CloudflareR2:AccountId"])
                && !string.IsNullOrWhiteSpace(builder.Configuration["CloudflareR2:AccessKeyId"]);
if (r2Configured)
    builder.Services.AddSingleton<CloudflareR2Service>();

// ── Invoice Mailer (Cloudflare Worker) ──────────────────────────────────────
builder.Services.AddHttpClient("InvoiceMailer");
builder.Services.AddScoped<InvoiceMailerService>();

// ── CORS ────────────────────────────────────────────────────────────────────
builder.Services.AddCors(o =>
    o.AddDefaultPolicy(p => p
        .WithOrigins(
            builder.Configuration["AllowedOrigins"]?.Split(',')
            ?? ["http://localhost:5173"])
        .AllowAnyHeader()
        .AllowAnyMethod()));

var app = builder.Build();

// ── Schema migration ────────────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
    await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreatedAsync();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// ── Helpers ─────────────────────────────────────────────────────────────────
static string Slugify(string input) =>
    System.Text.RegularExpressions.Regex.Replace(
        input.ToLowerInvariant().Trim(), @"[^a-z0-9]+", "-").Trim('-');

static int GetTenantId(HttpContext ctx) =>
    int.Parse(ctx.User.FindFirst("tenantId")!.Value);

// ── Auth Endpoints ───────────────────────────────────────────────────────────

app.MapPost("/api/auth/register", async (RegisterRequest req, AppDbContext db, JwtService jwt) =>
{
    if (await db.Users.AnyAsync(u => u.Email == req.Email))
        return Results.Conflict(new { detail = "El email ya está registrado." });

    var tenant = new Tenant { Name = req.CompanyName, Slug = Slugify(req.CompanyName) };

    // Make slug unique if it already exists
    if (await db.Tenants.AnyAsync(t => t.Slug == tenant.Slug))
        tenant.Slug = $"{tenant.Slug}-{Guid.NewGuid().ToString("N")[..6]}";

    db.Tenants.Add(tenant);
    await db.SaveChangesAsync();

    var hasher = new PasswordHasher<AppUser>();
    var user = new AppUser
    {
        TenantId = tenant.Id,
        Email = req.Email.ToLowerInvariant().Trim(),
        Name = req.Name,
        Role = "admin",
    };
    user.PasswordHash = hasher.HashPassword(user, req.Password);

    db.Users.Add(user);
    await db.SaveChangesAsync();

    return Results.Ok(new { token = jwt.Generate(user), name = user.Name, tenantName = tenant.Name });
});

app.MapPost("/api/auth/login", async (LoginRequest req, AppDbContext db, JwtService jwt) =>
{
    var user = await db.Users
        .Include(u => u.Tenant)
        .FirstOrDefaultAsync(u => u.Email == req.Email.ToLowerInvariant().Trim());

    if (user is null) return Results.Unauthorized();

    var hasher = new PasswordHasher<AppUser>();
    var result = hasher.VerifyHashedPassword(user, user.PasswordHash, req.Password);
    if (result == PasswordVerificationResult.Failed) return Results.Unauthorized();

    return Results.Ok(new { token = jwt.Generate(user), name = user.Name, tenantName = user.Tenant.Name });
});

// ── Dashboard ────────────────────────────────────────────────────────────────

app.MapGet("/api/dashboard", async (AppDbContext db, HttpContext ctx) =>
{
    var tenantId = GetTenantId(ctx);
    var invoices = db.Invoices.Where(i => i.TenantId == tenantId);
    var weekAgo  = DateTime.UtcNow.AddDays(-7);
    var monthAgo = DateTime.UtcNow.AddDays(-30);

    return new
    {
        totalInvoices   = await invoices.CountAsync(),
        totalRevenue    = await invoices.SumAsync(i => (decimal?)i.MontoTotal) ?? 0m,
        weekInvoices    = await invoices.Where(i => i.FechaEmision >= weekAgo).CountAsync(),
        monthRevenue    = await invoices.Where(i => i.FechaEmision >= monthAgo).SumAsync(i => (decimal?)i.MontoTotal) ?? 0m,
        acceptedByDgi   = await invoices.CountAsync(i => i.AceptadoPorDgi),
        byType          = await invoices
            .GroupBy(i => i.TipoCfe)
            .Select(g => new { tipoCfe = g.Key, count = g.Count(), total = g.Sum(i => i.MontoTotal) })
            .ToListAsync(),
    };
}).RequireAuthorization();

// ── CFE Types ────────────────────────────────────────────────────────────────

app.MapGet("/api/cfe-types", () =>
    Enum.GetValues<TipoCfe>()
        .Select(t => new { value = (int)t, label = t.ToString() })
        .OrderBy(t => t.value));

// ── Config Status ────────────────────────────────────────────────────────────

app.MapGet("/api/config/status", (IConfiguration config, HttpContext ctx) =>
{
    // Require auth but don't fail hard; just return status
    var section  = config.GetSection("UruFactura");
    var ufConfig = section.Get<UruFacturaConfig>() ?? new UruFacturaConfig();

    var issues = new List<string>();
    if (string.IsNullOrWhiteSpace(ufConfig.RutEmisor))         issues.Add("RutEmisor no configurado.");
    if (string.IsNullOrWhiteSpace(ufConfig.RazonSocialEmisor)) issues.Add("RazonSocialEmisor no configurado.");
    if (string.IsNullOrWhiteSpace(ufConfig.DomicilioFiscal))   issues.Add("DomicilioFiscal no configurado.");
    if (string.IsNullOrWhiteSpace(ufConfig.RutaCertificado))
        issues.Add("RutaCertificado no configurado.");
    else if (!File.Exists(ufConfig.RutaCertificado))
        issues.Add($"Certificado no encontrado: {ufConfig.RutaCertificado}");
    if (string.IsNullOrWhiteSpace(ufConfig.PasswordCertificado)) issues.Add("PasswordCertificado no configurado.");

    return new
    {
        ok = issues.Count == 0,
        ambiente = ufConfig.Ambiente.ToString(),
        rutEmisor = ufConfig.RutEmisor,
        razonSocial = ufConfig.RazonSocialEmisor,
        certificado = ufConfig.RutaCertificado,
        certificadoExiste = !string.IsNullOrWhiteSpace(ufConfig.RutaCertificado) && File.Exists(ufConfig.RutaCertificado),
        issues,
    };
}).RequireAuthorization();

// ── Invoices ─────────────────────────────────────────────────────────────────

app.MapGet("/api/invoices", async (AppDbContext db, HttpContext ctx) =>
{
    var tenantId = GetTenantId(ctx);
    return await db.Invoices
        .Where(i => i.TenantId == tenantId)
        .OrderByDescending(i => i.FechaEmision)
        .ToListAsync();
}).RequireAuthorization();

app.MapPost("/api/invoices", async (CreateInvoiceRequest req, AppDbContext db, IConfiguration config, HttpContext ctx,
    IServiceProvider services) =>
{
    var tenantId = GetTenantId(ctx);
    var ufConfig = config.GetSection("UruFactura").Get<UruFacturaConfig>()!;

    using var client = new UruFacturaClient(ufConfig);

    var tipo = (TipoCfe)req.TipoCfe;

    var cfe = tipo switch
    {
        TipoCfe.ETicket               => client.CrearETicket(),
        TipoCfe.NotaCreditoETicket    => client.CrearNotaCreditoETicket(),
        TipoCfe.NotaDebitoETicket     => client.CrearNotaDebitoETicket(),
        TipoCfe.EFactura              => client.CrearEFactura(),
        TipoCfe.NotaCreditoEFactura   => client.CrearNotaCreditoEFactura(),
        TipoCfe.NotaDebitoEFactura    => client.CrearNotaDebitoEFactura(),
        TipoCfe.EFacturaExportacion   => client.CrearEFacturaExportacion(),
        TipoCfe.ERemito               => client.CrearERemito(),
        _ => throw new ArgumentException($"Tipo de CFE no soportado: {tipo}"),
    };

    cfe.Numero = req.Numero;

    if (!string.IsNullOrWhiteSpace(req.RutReceptor))
        cfe.Receptor = new Receptor { Documento = req.RutReceptor, RazonSocial = req.NombreReceptor };

    for (int i = 0; i < req.Detalle.Count; i++)
    {
        var l = req.Detalle[i];
        cfe.Detalle.Add(new LineaDetalle
        {
            NroLinea       = i + 1,
            NombreItem     = l.NombreItem,
            Cantidad       = l.Cantidad,
            PrecioUnitario = l.PrecioUnitario,
            IndFactIva     = (TipoIva)l.IndFactIva,
        });
    }

    foreach (var r in req.Referencias ?? [])
    {
        if (!Enum.IsDefined(typeof(TipoCfe), r.TipoCfe))
            return Results.BadRequest(new { detail = $"TipoCfe de referencia inválido: {r.TipoCfe}" });

        cfe.Referencias.Add(new RefCfe
        {
            TipoCfe  = (TipoCfe)r.TipoCfe,
            Serie    = r.Serie ?? string.Empty,
            NroCfe   = r.NroCfe,
            FechaCfe = r.FechaCfe,
            Razon    = r.Razon,
        });
    }

    cfe.CalcularTotales();
    client.GenerarYFirmar(cfe);

    // ── Generate PDF for storage ─────────────────────────────────────────────
    var pdfBytes = client.GenerarPdfA4(cfe);

    var invoice = new Invoice
    {
        TenantId         = tenantId,
        TipoCfe          = (int)cfe.Tipo,
        Numero           = cfe.Numero,
        FechaEmision     = cfe.FechaEmision,
        RutReceptor      = cfe.Receptor?.Documento,
        NombreReceptor   = cfe.Receptor?.RazonSocial,
        MontoTotal       = cfe.MontoTotal,
        MontoNetoExento  = cfe.MontoNetoExento,
        MontoNetoMinimo  = cfe.MontoNetoMinimo,
        MontoNetoBasico  = cfe.MontoNetoBasico,
        IvaMinimo        = cfe.IvaMinimo,
        IvaBasico        = cfe.IvaBasico,
        XmlFirmado       = cfe.XmlFirmado,
        DetalleJson      = JsonSerializer.Serialize(cfe.Detalle),
    };

    db.Invoices.Add(invoice);
    await db.SaveChangesAsync();

    // ── Upload to Cloudflare R2 (best-effort) ────────────────────────────────
    var r2 = services.GetService<CloudflareR2Service>();
    if (r2 is not null)
    {
        var prefix = $"tenant-{tenantId}/{cfe.FechaEmision:yyyy/MM}";
        try
        {
            var pdfKey = $"{prefix}/{invoice.Id}-{(int)cfe.Tipo}-{cfe.Numero}.pdf";
            await r2.UploadAsync(pdfKey, pdfBytes, "application/pdf");
            invoice.R2PdfKey = pdfKey;
        }
        catch (Exception ex)
        {
            app.Logger.LogWarning(ex, "R2 PDF upload failed for invoice {InvoiceId}.", invoice.Id);
        }

        if (!string.IsNullOrWhiteSpace(cfe.XmlFirmado))
        {
            try
            {
                var xmlBytes = System.Text.Encoding.UTF8.GetBytes(cfe.XmlFirmado);
                var xmlKey   = $"{prefix}/{invoice.Id}-{(int)cfe.Tipo}-{cfe.Numero}.xml";
                await r2.UploadAsync(xmlKey, xmlBytes, "application/xml");
                invoice.R2XmlKey = xmlKey;
            }
            catch (Exception ex)
            {
                app.Logger.LogWarning(ex, "R2 XML upload failed for invoice {InvoiceId}.", invoice.Id);
            }
        }

        if (invoice.R2PdfKey is not null || invoice.R2XmlKey is not null)
            await db.SaveChangesAsync();
    }

    // ── Send email notification via Cloudflare Worker (best-effort) ──────────
    if (!string.IsNullOrWhiteSpace(req.RecipientEmail))
    {
        var mailer = services.GetRequiredService<InvoiceMailerService>();
        var tenant = await db.Tenants.FindAsync(tenantId);
        string? pdfUrl = null;
        if (r2 is not null && invoice.R2PdfKey is not null)
            pdfUrl = await r2.GetDownloadUrlAsync(invoice.R2PdfKey);

        await mailer.NotifyAsync(new EmailPayload(
            TenantName:    tenant?.Name ?? string.Empty,
            RecipientEmail: req.RecipientEmail,
            RecipientName:  req.RecipientName ?? req.RecipientEmail,
            InvoiceId:     invoice.Id,
            InvoiceNumber: invoice.Numero,
            TipoCfe:       invoice.TipoCfe,
            TipoCfeLabel:  cfe.Tipo.ToString(),
            Total:         invoice.MontoTotal,
            PdfUrl:        pdfUrl));
    }

    return Results.Created($"/api/invoices/{invoice.Id}", invoice);
}).RequireAuthorization();

// ── Download PDF (from R2 if stored, otherwise regenerate) ────────────────
app.MapGet("/api/invoices/{id:int}/pdf", async (int id, AppDbContext db, IConfiguration config, HttpContext ctx,
    IServiceProvider services) =>
{
    var tenantId = GetTenantId(ctx);
    var invoice  = await db.Invoices.FirstOrDefaultAsync(i => i.Id == id && i.TenantId == tenantId);
    if (invoice is null) return Results.NotFound();

    // If the PDF is stored in R2, serve it from there
    var r2 = services.GetService<CloudflareR2Service>();
    if (r2 is not null && !string.IsNullOrWhiteSpace(invoice.R2PdfKey))
    {
        try
        {
            var pdfBytes = await r2.DownloadAsync(invoice.R2PdfKey);
            return Results.File(pdfBytes, "application/pdf", $"factura-{invoice.Numero}.pdf");
        }
        catch (Exception ex)
        {
            app.Logger.LogWarning(ex, "R2 PDF download failed for invoice {Id}, falling back to on-the-fly generation.", id);
        }
    }

    // Fallback: regenerate PDF from stored data
    var ufConfig = config.GetSection("UruFactura").Get<UruFacturaConfig>()!;
    using var client = new UruFacturaClient(ufConfig);

    var cfe = new Cfe
    {
        Tipo                  = (TipoCfe)invoice.TipoCfe,
        Numero                = invoice.Numero,
        FechaEmision          = invoice.FechaEmision,
        MontoTotal            = invoice.MontoTotal,
        MontoNetoExento       = invoice.MontoNetoExento,
        MontoNetoMinimo       = invoice.MontoNetoMinimo,
        MontoNetoBasico       = invoice.MontoNetoBasico,
        IvaMinimo             = invoice.IvaMinimo,
        IvaBasico             = invoice.IvaBasico,
        RutEmisor             = ufConfig.RutEmisor,
        RazonSocialEmisor     = ufConfig.RazonSocialEmisor,
        DomicilioFiscalEmisor = ufConfig.DomicilioFiscal,
        CiudadEmisor          = ufConfig.Ciudad,
        DepartamentoEmisor    = ufConfig.Departamento,
        XmlFirmado            = invoice.XmlFirmado,
    };

    if (!string.IsNullOrWhiteSpace(invoice.DetalleJson))
    {
        try
        {
            var detalle = JsonSerializer.Deserialize<List<LineaDetalle>>(invoice.DetalleJson);
            if (detalle is not null) cfe.Detalle.AddRange(detalle);
        }
        catch (JsonException)
        {
            return Results.Problem(
                detail: "La factura no puede generar el PDF porque su detalle almacenado es inválido o incompatible.",
                statusCode: StatusCodes.Status422UnprocessableEntity,
                title: "Detalle de factura inválido");
        }
    }

    if (!string.IsNullOrWhiteSpace(invoice.RutReceptor))
        cfe.Receptor = new Receptor { Documento = invoice.RutReceptor, RazonSocial = invoice.NombreReceptor };

    var pdf = client.GenerarPdfA4(cfe);
    return Results.File(pdf, "application/pdf", $"factura-{invoice.Numero}.pdf");
}).RequireAuthorization();

// ── R2 download URLs ──────────────────────────────────────────────────────
app.MapGet("/api/invoices/{id:int}/r2-urls", async (int id, AppDbContext db, HttpContext ctx,
    IServiceProvider services) =>
{
    var tenantId = GetTenantId(ctx);
    var invoice  = await db.Invoices.FirstOrDefaultAsync(i => i.Id == id && i.TenantId == tenantId);
    if (invoice is null) return Results.NotFound();

    var r2 = services.GetService<CloudflareR2Service>();
    if (r2 is null)
        return Results.Problem("Cloudflare R2 is not configured.", statusCode: StatusCodes.Status503ServiceUnavailable);

    string? pdfUrl = invoice.R2PdfKey is not null
        ? await r2.GetDownloadUrlAsync(invoice.R2PdfKey)
        : null;

    string? xmlUrl = invoice.R2XmlKey is not null
        ? await r2.GetDownloadUrlAsync(invoice.R2XmlKey)
        : null;

    return Results.Ok(new { pdfUrl, xmlUrl });
}).RequireAuthorization();

app.Run();

// ── DTOs ────────────────────────────────────────────────────────────────────

record RegisterRequest(string CompanyName, string Name, string Email, string Password);
record LoginRequest(string Email, string Password);

record CreateInvoiceRequest(
    int TipoCfe,
    long Numero,
    string? RutReceptor,
    string? NombreReceptor,
    List<LineaDetalleDto> Detalle,
    List<RefCfeDto>? Referencias = null,
    string? RecipientEmail = null,
    string? RecipientName  = null);

record LineaDetalleDto(
    string NombreItem,
    decimal Cantidad,
    decimal PrecioUnitario,
    int IndFactIva);

record RefCfeDto(
    int TipoCfe,
    string? Serie,
    long NroCfe,
    DateTime FechaCfe,
    string? Razon);
