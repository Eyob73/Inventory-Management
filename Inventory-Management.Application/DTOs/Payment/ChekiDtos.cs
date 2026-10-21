namespace Inventory_Management.Application.DTOs.Payment;

public class ChekiBankDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Swift { get; set; }
    public string Type { get; set; } = "bank";
    public string Status { get; set; } = "live";
    public bool RequiresAccount { get; set; }
    public int? AccountDigits { get; set; }
    public bool RequiresPhone { get; set; }
    public string? Endpoint { get; set; }
    public string? Color { get; set; }
    public string? Initials { get; set; }
}

public class ChekiBanksResponse
{
    public List<ChekiBankDto> Banks { get; set; } = new();
}

public class ChekiVerifyRequest
{
    public string Bank { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string? AccountNumber { get; set; }
    public string? PayerPhone { get; set; }
    public string? QrData { get; set; }
}

public class ChekiVerifyResponse
{
    public bool Success { get; set; }
    public bool Verified { get; set; }
    public string? Bank { get; set; }
    public string? BankCode { get; set; }
    public string? Reference { get; set; }
    public decimal? Amount { get; set; }
    public string? Currency { get; set; }
    public string? SenderName { get; set; }
    public string? SenderAccount { get; set; }
    public string? ReceiverName { get; set; }
    public string? ReceiverAccount { get; set; }
    public string? Date { get; set; }
    public string? Reason { get; set; }
    public int? DurationMs { get; set; }
    public string? SourceUrl { get; set; }
    public string? Error { get; set; }
    public int? StatusCode { get; set; }
    public bool IsRateLimited { get; set; }
    public int? RetryAfterSeconds { get; set; }
}
