using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Inventory_Management.Application.Common;
using Inventory_Management.Application.DTOs.Payment;
using Inventory_Management.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Inventory_Management.Infrastructure.Services;

public class ChekiVerificationService : IChekiVerificationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ChekiVerificationService> _logger;
    private readonly string _baseUrl;
    private static List<ChekiBankDto>? _cachedBanks;
    private static DateTime _cacheExpiresAt = DateTime.MinValue;
    private static readonly SemaphoreSlim _cacheLock = new(1, 1);

    public ChekiVerificationService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<ChekiVerificationService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        _baseUrl = configuration["Cheki:BaseUrl"]?.TrimEnd('/') ?? "https://cheki.et";
        if (_httpClient.BaseAddress == null)
        {
            _httpClient.BaseAddress = new Uri(_baseUrl);
        }

        var timeoutSec = configuration.GetValue<int?>("Cheki:TimeoutSeconds") ?? 45;
        _httpClient.Timeout = TimeSpan.FromSeconds(timeoutSec);
    }

    public async Task<List<ChekiBankDto>> GetSupportedBanksAsync(CancellationToken cancellationToken = default)
    {
        if (_cachedBanks != null && DateTime.UtcNow < _cacheExpiresAt)
        {
            return _cachedBanks;
        }

        await _cacheLock.WaitAsync(cancellationToken);
        try
        {
            if (_cachedBanks != null && DateTime.UtcNow < _cacheExpiresAt)
            {
                return _cachedBanks;
            }

            try
            {
                var response = await _httpClient.GetAsync("/api/banks", cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var result = await response.Content.ReadFromJsonAsync<ChekiBanksResponse>(options, cancellationToken);
                    if (result?.Banks != null && result.Banks.Count > 0)
                    {
                        _cachedBanks = result.Banks;
                        _cacheExpiresAt = DateTime.UtcNow.AddMinutes(10);
                        return _cachedBanks;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch live banks from Cheki API. Falling back to cached or default bank list.");
            }

            // Fallback to default banks if network/Cheki fails
            _cachedBanks = GetDefaultBanks();
            _cacheExpiresAt = DateTime.UtcNow.AddMinutes(5);
            return _cachedBanks;
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    public async Task<ChekiVerifyResponse> VerifyPaymentAsync(ChekiVerifyRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Bank))
            throw new ArgumentException("Bank identifier is required.", nameof(request.Bank));

        if (string.IsNullOrWhiteSpace(request.Reference))
            throw new ArgumentException("Transaction reference or receipt link is required.", nameof(request.Reference));

        // SSRF validation if reference is a URL
        if (request.Reference.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            request.Reference.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            if (!PaymentDestinationSecurity.ValidateReceiptUrl(request.Reference, out var ssrfError))
            {
                _logger.LogWarning("SSRF validation failed for receipt URL: {Url}. Reason: {Reason}", request.Reference, ssrfError);
                return new ChekiVerifyResponse
                {
                    Success = false,
                    Verified = false,
                    Error = ssrfError ?? "Invalid receipt URL.",
                    StatusCode = (int)HttpStatusCode.BadRequest
                };
            }
        }

        var payload = new Dictionary<string, object?>
        {
            ["bank"] = request.Bank.Trim(),
            ["reference"] = request.Reference.Trim()
        };

        if (!string.IsNullOrWhiteSpace(request.AccountNumber))
            payload["accountNumber"] = request.AccountNumber.Trim();

        if (!string.IsNullOrWhiteSpace(request.PayerPhone))
            payload["payerPhone"] = PaymentDestinationSecurity.NormalizePhoneNumber(request.PayerPhone);

        if (!string.IsNullOrWhiteSpace(request.QrData))
            payload["qrData"] = request.QrData.Trim();

        var maskedRef = PaymentDestinationSecurity.NormalizeReference(request.Reference);
        _logger.LogInformation("Sending payment verification request to Cheki for bank {Bank}, ref token {Ref}", request.Bank, maskedRef);

        int maxAttempts = 2;
        HttpResponseMessage? response = null;

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                response = await _httpClient.PostAsJsonAsync("/api/verify", payload, cancellationToken);
                break;
            }
            catch (HttpRequestException ex) when (attempt < maxAttempts)
            {
                _logger.LogWarning(ex, "Transient error connecting to Cheki on attempt {Attempt}. Retrying once...", attempt);
                await Task.Delay(1000, cancellationToken);
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Timeout connecting to Cheki verification service.");
                return new ChekiVerifyResponse
                {
                    Success = false,
                    Verified = false,
                    Error = "Cheki verification request timed out. Please try again.",
                    StatusCode = (int)HttpStatusCode.GatewayTimeout
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error calling Cheki verification service.");
                return new ChekiVerifyResponse
                {
                    Success = false,
                    Verified = false,
                    Error = "An unexpected error occurred while contacting the verification provider.",
                    StatusCode = (int)HttpStatusCode.InternalServerError
                };
            }
        }

        if (response == null)
        {
            return new ChekiVerifyResponse
            {
                Success = false,
                Verified = false,
                Error = "Unable to reach the payment verification service.",
                StatusCode = (int)HttpStatusCode.ServiceUnavailable
            };
        }

        var statusCode = (int)response.StatusCode;

        // Rate limit check
        if (response.StatusCode == (HttpStatusCode)429)
        {
            int? retryAfter = null;
            if (response.Headers.TryGetValues("Retry-After", out var values))
            {
                if (int.TryParse(values.FirstOrDefault(), out var seconds))
                    retryAfter = seconds;
            }

            _logger.LogWarning("Cheki rate limit encountered. Retry-After: {RetryAfter}s", retryAfter);
            return new ChekiVerifyResponse
            {
                Success = false,
                Verified = false,
                IsRateLimited = true,
                RetryAfterSeconds = retryAfter ?? 60,
                StatusCode = 429,
                Error = $"Verification rate limit exceeded. Please retry after {retryAfter ?? 60} seconds."
            };
        }

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        };

        try
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var rawJson = JsonSerializer.Deserialize<JsonElement>(content);

            var verifyResult = new ChekiVerifyResponse
            {
                StatusCode = statusCode
            };

            if (rawJson.TryGetProperty("success", out var successProp))
                verifyResult.Success = successProp.GetBoolean();

            if (rawJson.TryGetProperty("verified", out var verifiedProp))
                verifyResult.Verified = verifiedProp.GetBoolean();

            if (rawJson.TryGetProperty("bank", out var bankProp))
                verifyResult.Bank = bankProp.GetString();

            if (rawJson.TryGetProperty("bankCode", out var bankCodeProp))
                verifyResult.BankCode = bankCodeProp.GetString();

            if (rawJson.TryGetProperty("reference", out var refProp))
                verifyResult.Reference = refProp.GetString();

            if (rawJson.TryGetProperty("amount", out var amountProp))
            {
                if (amountProp.ValueKind == JsonValueKind.Number && amountProp.TryGetDecimal(out var amt))
                    verifyResult.Amount = amt;
                else if (amountProp.ValueKind == JsonValueKind.String && decimal.TryParse(amountProp.GetString(), out var parsedAmt))
                    verifyResult.Amount = parsedAmt;
            }

            if (rawJson.TryGetProperty("currency", out var currProp))
                verifyResult.Currency = currProp.GetString() ?? "ETB";
            else
                verifyResult.Currency = "ETB";

            if (rawJson.TryGetProperty("senderName", out var senderProp))
                verifyResult.SenderName = senderProp.GetString();

            if (rawJson.TryGetProperty("senderAccount", out var senderAccProp))
                verifyResult.SenderAccount = senderAccProp.GetString();

            if (rawJson.TryGetProperty("receiverName", out var receiverProp))
                verifyResult.ReceiverName = receiverProp.GetString();

            if (rawJson.TryGetProperty("receiverAccount", out var receiverAccProp))
                verifyResult.ReceiverAccount = receiverAccProp.GetString();

            if (rawJson.TryGetProperty("date", out var dateProp))
                verifyResult.Date = dateProp.GetString();

            if (rawJson.TryGetProperty("reason", out var reasonProp))
                verifyResult.Reason = reasonProp.GetString();

            if (rawJson.TryGetProperty("durationMs", out var durProp) && durProp.TryGetInt32(out var dur))
                verifyResult.DurationMs = dur;

            if (rawJson.TryGetProperty("sourceUrl", out var srcProp))
                verifyResult.SourceUrl = srcProp.GetString();

            if (rawJson.TryGetProperty("error", out var errProp))
                verifyResult.Error = errProp.GetString();

            return verifyResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse response from Cheki API.");
            return new ChekiVerifyResponse
            {
                Success = false,
                Verified = false,
                StatusCode = statusCode,
                Error = "Invalid response format received from payment provider."
            };
        }
    }

    private static List<ChekiBankDto> GetDefaultBanks()
    {
        return
        [
            new ChekiBankDto { Id = "cbe", Name = "Commercial Bank of Ethiopia", Swift = "CBETETAA", Type = "bank", Status = "live", RequiresAccount = false, AccountDigits = 8, Color = "#502878", Initials = "CBE" },
            new ChekiBankDto { Id = "telebirr", Name = "Telebirr", Type = "mobile", Status = "live", RequiresAccount = false, RequiresPhone = false, Color = "#00A0DC", Initials = "TB" },
            new ChekiBankDto { Id = "cbebirr", Name = "CBE Birr", Type = "wallet", Status = "live", RequiresAccount = false, RequiresPhone = true, Color = "#502878", Initials = "CB" },
            new ChekiBankDto { Id = "boa", Name = "Bank of Abyssinia", Swift = "ABYSETAA", Type = "bank", Status = "live", RequiresAccount = true, AccountDigits = 8, Color = "#F7A800", Initials = "BOA" },
            new ChekiBankDto { Id = "dashen", Name = "Dashen Bank", Swift = "DASHETAA", Type = "bank", Status = "live", RequiresAccount = false, Color = "#002B49", Initials = "DB" },
            new ChekiBankDto { Id = "awash", Name = "Awash Bank", Swift = "AWINETAA", Type = "bank", Status = "live", RequiresAccount = false, Color = "#E2231A", Initials = "AB" },
            new ChekiBankDto { Id = "ebirr", Name = "eBirr", Type = "mobile", Status = "live", RequiresAccount = false, Color = "#059669", Initials = "eB" },
            new ChekiBankDto { Id = "zamzam", Name = "ZamZam Bank", Swift = "ZAMZETAA", Type = "bank", Status = "live", RequiresAccount = false, Color = "#0d9488", Initials = "ZZ" }
        ];
    }
}
