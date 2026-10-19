using System.Net;
using System.Text.RegularExpressions;
using Inventory_Management.Domain.Entities;
using Inventory_Management.Domain.Enums;

namespace Inventory_Management.Application.Common;

public static class PaymentDestinationSecurity
{
    private static readonly string[] AllowedBankDomains =
    [
        "cbe.com.et",
        "mbreciept.cbe.com.et",
        "mb.cbe.com.et",
        "ethiotelecom.et",
        "transactioninfo.ethiotelecom.et",
        "bankofabyssinia.com",
        "awashbank.com",
        "ebirr.com",
        "zamzambank.com.et",
        "zemenbank.com",
        "cheki.et"
    ];

    public static string MaskAccountNumber(string? accountNumber)
    {
        if (string.IsNullOrWhiteSpace(accountNumber))
            return string.Empty;

        var cleaned = accountNumber.Trim();
        if (cleaned.Length <= 4)
            return new string('*', cleaned.Length);

        if (cleaned.Length <= 8)
        {
            return $"{cleaned[..1]}****{cleaned[^2..]}";
        }

        // e.g. 1000000006171 -> 1000****6171
        var prefixLen = Math.Min(4, cleaned.Length / 3);
        var suffixLen = Math.Min(4, cleaned.Length / 3);
        return $"{cleaned[..prefixLen]}****{cleaned[^suffixLen..]}";
    }

    public static string MaskPhoneNumber(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return string.Empty;

        var cleaned = phoneNumber.Trim();
        if (cleaned.Length <= 4)
            return new string('*', cleaned.Length);

        // e.g. +251911223344 -> +2519****3344 or 0911223344 -> 09****3344
        if (cleaned.StartsWith("+251") && cleaned.Length >= 12)
        {
            return $"{cleaned[..5]}****{cleaned[^4..]}";
        }

        if (cleaned.StartsWith("09") || cleaned.StartsWith("07"))
        {
            return $"{cleaned[..2]}****{cleaned[^4..]}";
        }

        var prefixLen = Math.Min(3, cleaned.Length / 3);
        var suffixLen = Math.Min(4, cleaned.Length / 3);
        return $"{cleaned[..prefixLen]}****{cleaned[^suffixLen..]}";
    }

    public static string NormalizePhoneNumber(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return string.Empty;

        // Remove all non-digit and non-plus characters
        var cleaned = Regex.Replace(phoneNumber.Trim(), @"[^\d+]", "");

        // Convert local format 09... or 07... to international +251...
        if (cleaned.StartsWith("09") && cleaned.Length == 10)
        {
            return $"+251{cleaned[1..]}";
        }
        if (cleaned.StartsWith("07") && cleaned.Length == 10)
        {
            return $"+251{cleaned[1..]}";
        }
        if (cleaned.StartsWith("251") && cleaned.Length == 12)
        {
            return $"+{cleaned}";
        }
        if (cleaned.StartsWith("+251") && cleaned.Length == 13)
        {
            return cleaned;
        }

        return cleaned;
    }

    public static string NormalizeReference(string reference)
    {
        if (string.IsNullOrWhiteSpace(reference))
            return string.Empty;

        var trimmed = reference.Trim();

        // If it's a URL, extract the identifier
        if (trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            if (Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
            {
                // Check query parameters like ?ref= or ?TID=
                var query = uri.Query;
                if (!string.IsNullOrEmpty(query))
                {
                    var match = Regex.Match(query, @"(?:ref|TID|id)=([^&]+)", RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        return Uri.UnescapeDataString(match.Groups[1].Value).Trim().ToUpperInvariant();
                    }
                }

                // Extract last non-empty path segment
                var segments = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (segments.Length > 0)
                {
                    return Uri.UnescapeDataString(segments[^1]).Trim().ToUpperInvariant();
                }
            }
        }

        // Clean spaces and dashes for plain transaction codes
        return Regex.Replace(trimmed, @"\s+", "").ToUpperInvariant();
    }

    public static bool ValidateReceiptUrl(string url, out string? errorMessage)
    {
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(url))
        {
            errorMessage = "Receipt URL cannot be empty.";
            return false;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            errorMessage = "Invalid receipt URL format.";
            return false;
        }

        if (uri.Scheme != Uri.UriSchemeHttps)
        {
            errorMessage = "Receipt URL must use secure HTTPS.";
            return false;
        }

        var host = uri.Host.ToLowerInvariant();

        // SSRF protection: block loopback and IP addresses directly
        if (uri.IsLoopback || IPAddress.TryParse(host, out _))
        {
            errorMessage = "Direct IP addresses and loopback URLs are not permitted.";
            return false;
        }

        // Check against allowed bank domains
        bool isAllowedDomain = AllowedBankDomains.Any(domain =>
            host.Equals(domain, StringComparison.OrdinalIgnoreCase) ||
            host.EndsWith("." + domain, StringComparison.OrdinalIgnoreCase));

        if (!isAllowedDomain)
        {
            errorMessage = $"The receipt host '{host}' is not a recognized Ethiopian banking or payment provider domain.";
            return false;
        }

        return true;
    }

    public static (DestinationMatchStatus Status, string? Message) EvaluateDestinationMatch(
        PaymentDestination destination,
        string? receiverAccount,
        string? receiverName)
    {
        if (destination == null)
            return (DestinationMatchStatus.UnableToConfirm, "Destination not selected.");

        if (string.IsNullOrWhiteSpace(receiverAccount))
        {
            return (DestinationMatchStatus.UnableToConfirm,
                "Payment provider verified the transaction, but receiving account details were not provided in the receipt.");
        }

        var recClean = receiverAccount.Trim();

        if (destination.Type == PaymentDestinationType.BankAccount)
        {
            var destAcc = (destination.AccountNumber ?? "").Trim();
            if (string.IsNullOrEmpty(destAcc))
            {
                return (DestinationMatchStatus.UnableToConfirm, "Registered destination does not have an account number recorded.");
            }

            // Check masked account format like 1****6171
            if (recClean.Contains('*'))
            {
                var parts = recClean.Split('*', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    var prefix = parts[0];
                    var suffix = parts[^1];

                    bool prefixMatches = destAcc.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
                    bool suffixMatches = destAcc.EndsWith(suffix, StringComparison.OrdinalIgnoreCase);

                    if (prefixMatches && suffixMatches)
                    {
                        return (DestinationMatchStatus.Matched, "Receiving account prefix and suffix match the registered bank account.");
                    }
                    else
                    {
                        return (DestinationMatchStatus.Mismatched,
                            $"Receiving account pattern '{recClean}' does not match registered account ending in {destAcc[^Math.Min(4, destAcc.Length)..]}.");
                    }
                }
                else if (parts.Length == 1)
                {
                    var token = parts[0];
                    if (destAcc.EndsWith(token, StringComparison.OrdinalIgnoreCase))
                    {
                        return (DestinationMatchStatus.Matched, "Receiving account suffix matches registered destination.");
                    }
                }
            }
            else
            {
                // Full account check
                var cleanDest = Regex.Replace(destAcc, @"\D", "");
                var cleanRec = Regex.Replace(recClean, @"\D", "");

                if (cleanDest.Equals(cleanRec, StringComparison.OrdinalIgnoreCase) ||
                    (cleanDest.Length >= 8 && cleanRec.Length >= 8 && cleanDest[^8..] == cleanRec[^8..]))
                {
                    return (DestinationMatchStatus.Matched, "Receiving account number fully matches registered destination.");
                }
                else
                {
                    return (DestinationMatchStatus.Mismatched,
                        $"Receiving account '{recClean}' does not match registered destination account.");
                }
            }
        }
        else if (destination.Type == PaymentDestinationType.Telebirr)
        {
            var destPhone = NormalizePhoneNumber(destination.PhoneNumber);
            var recPhone = NormalizePhoneNumber(receiverAccount);

            if (!string.IsNullOrEmpty(destPhone) && !string.IsNullOrEmpty(recPhone))
            {
                if (destPhone.Equals(recPhone, StringComparison.OrdinalIgnoreCase) ||
                    (destPhone.Length >= 9 && recPhone.Length >= 9 && destPhone[^9..] == recPhone[^9..]))
                {
                    return (DestinationMatchStatus.Matched, "Receiving phone number matches registered Telebirr destination.");
                }
                else
                {
                    return (DestinationMatchStatus.Mismatched,
                        $"Receiving phone '{receiverAccount}' does not match registered Telebirr number.");
                }
            }
        }

        // Account check was inconclusive, check receiver name against account holder name
        if (!string.IsNullOrWhiteSpace(receiverName) && !string.IsNullOrWhiteSpace(destination.AccountHolderName))
        {
            var holderParts = destination.AccountHolderName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var recParts = receiverName.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var common = holderParts.Intersect(recParts, StringComparer.OrdinalIgnoreCase).Count();
            if (common >= 2)
            {
                return (DestinationMatchStatus.Matched, "Receiver name matches registered account holder name.");
            }
        }

        return (DestinationMatchStatus.UnableToConfirm,
            "Receiving destination details could not be matched with high certainty. Manual review is recommended.");
    }
}
