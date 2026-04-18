using System.Text;
using System.Text.Json;

namespace UruErpApp.Api.Services;

/// <summary>
/// Sends an invoice notification email by calling the Cloudflare Worker endpoint.
/// The Worker is responsible for formatting the email and sending it via MailChannels.
/// </summary>
public class InvoiceMailerService(IHttpClientFactory httpFactory, IConfiguration config, ILogger<InvoiceMailerService> logger)
{
    private readonly string? _workerUrl = config["InvoiceMailer:WorkerUrl"];

    /// <summary>
    /// Posts a notification to the invoice-mailer Cloudflare Worker.
    /// Silently swallows failures so that invoice creation is never blocked by email errors.
    /// </summary>
    public async Task NotifyAsync(EmailPayload payload, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_workerUrl))
        {
            logger.LogDebug("InvoiceMailer:WorkerUrl is not configured – skipping email notification.");
            return;
        }

        try
        {
            var http    = httpFactory.CreateClient("InvoiceMailer");
            var json    = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var msg = new HttpRequestMessage(HttpMethod.Post, _workerUrl) { Content = content };

            var apiSecret = config["InvoiceMailer:ApiSecret"];
            if (!string.IsNullOrWhiteSpace(apiSecret))
                msg.Headers.TryAddWithoutValidation("X-Api-Secret", apiSecret);

            var res = await http.SendAsync(msg, ct);
            if (!res.IsSuccessStatusCode)
                logger.LogWarning("Invoice mailer Worker returned {Status}", res.StatusCode);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send invoice email notification.");
        }
    }
}

public record EmailPayload(
    string TenantName,
    string RecipientEmail,
    string RecipientName,
    int    InvoiceId,
    long   InvoiceNumber,
    int    TipoCfe,
    string TipoCfeLabel,
    decimal Total,
    string? PdfUrl);
