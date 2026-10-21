using System.Text.Json.Serialization;

namespace Inventory_Management.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PaymentDestinationType
{
    BankAccount = 1,
    Telebirr = 2
}

