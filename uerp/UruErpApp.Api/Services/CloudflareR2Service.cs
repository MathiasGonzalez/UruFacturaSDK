using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.Runtime;

namespace UruErpApp.Api.Services;

/// <summary>
/// Uploads and retrieves invoice artifacts (PDF and signed XML) from Cloudflare R2.
/// R2 is S3-compatible; we use the AWS SDK with a custom endpoint.
/// </summary>
public class CloudflareR2Service
{
    private readonly AmazonS3Client _client;
    private readonly string _bucket;
    private readonly string? _publicBaseUrl;

    public CloudflareR2Service(IConfiguration config)
    {
        var section = config.GetSection("CloudflareR2");
        var accountId   = section["AccountId"] ?? throw new InvalidOperationException("CloudflareR2:AccountId is required.");
        var accessKey   = section["AccessKeyId"] ?? throw new InvalidOperationException("CloudflareR2:AccessKeyId is required.");
        var secretKey   = section["SecretAccessKey"] ?? throw new InvalidOperationException("CloudflareR2:SecretAccessKey is required.");
        _bucket         = section["BucketName"] ?? "uruerp-invoices";
        _publicBaseUrl  = section["PublicBaseUrl"];

        var endpoint = new AmazonS3Config
        {
            ServiceURL            = $"https://{accountId}.r2.cloudflarestorage.com",
            ForcePathStyle        = true,
            SignatureVersion      = "4",
            AuthenticationRegion  = "auto",
        };

        _client = new AmazonS3Client(
            new BasicAWSCredentials(accessKey, secretKey),
            endpoint);
    }

    /// <summary>Uploads raw bytes to R2 and returns the object key.</summary>
    public async Task<string> UploadAsync(string key, byte[] data, string contentType,
        CancellationToken ct = default)
    {
        var request = new PutObjectRequest
        {
            BucketName  = _bucket,
            Key         = key,
            InputStream = new MemoryStream(data),
            ContentType = contentType,
        };

        await _client.PutObjectAsync(request, ct);
        return key;
    }

    /// <summary>
    /// Returns the public URL for the given key when a public bucket base URL is
    /// configured, or generates a 1-hour presigned URL otherwise.
    /// </summary>
    public async Task<string> GetDownloadUrlAsync(string key, CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(_publicBaseUrl))
            return $"{_publicBaseUrl.TrimEnd('/')}/{key}";

        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucket,
            Key        = key,
            Expires    = DateTime.UtcNow.AddHours(1),
            Protocol   = Protocol.HTTPS,
        };

        return await Task.FromResult(_client.GetPreSignedURL(request));
    }

    /// <summary>Downloads raw bytes from R2.</summary>
    public async Task<byte[]> DownloadAsync(string key, CancellationToken ct = default)
    {
        var response = await _client.GetObjectAsync(_bucket, key, ct);
        using var ms = new MemoryStream();
        await response.ResponseStream.CopyToAsync(ms, ct);
        return ms.ToArray();
    }
}
