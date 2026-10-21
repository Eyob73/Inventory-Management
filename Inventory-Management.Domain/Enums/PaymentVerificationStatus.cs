using System.Text.Json.Serialization;

namespace Inventory_Management.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PaymentVerificationStatus
{
    Pending = 1,
    Verified = 2,
    Rejected = 3,
    AmountMismatch = 4,
    DestinationMismatch = 5,
    UnableToConfirmDestination = 6,
    NeedsManualReview = 7,
    Failed = 8
}

