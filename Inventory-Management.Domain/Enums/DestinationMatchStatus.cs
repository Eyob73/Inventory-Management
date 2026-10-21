using System.Text.Json.Serialization;

namespace Inventory_Management.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DestinationMatchStatus
{
    Matched = 1,
    Mismatched = 2,
    UnableToConfirm = 3,
    NotApplicable = 4
}

