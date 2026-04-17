namespace SaasApp.Api.Models;

public class Tenant
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<AppUser> Users { get; set; } = [];
    public List<Invoice> Invoices { get; set; } = [];
}
