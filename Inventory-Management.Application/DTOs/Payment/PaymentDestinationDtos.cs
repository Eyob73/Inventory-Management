using System.ComponentModel.DataAnnotations;
using Inventory_Management.Domain.Enums;

namespace Inventory_Management.Application.DTOs.Payment;

public class PaymentDestinationDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public PaymentDestinationType Type { get; set; }
    public string TypeName => Type.ToString();
    public string? BankCode { get; set; }
    public string? BankName { get; set; }
    public string? MaskedAccountNumber { get; set; }
    public string? MaskedPhoneNumber { get; set; }
    public string? AccountHolderName { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public class CreatePaymentDestinationDto
{
    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public PaymentDestinationType Type { get; set; } = PaymentDestinationType.BankAccount;

    [MaxLength(50)]
    public string? BankCode { get; set; }

    [MaxLength(150)]
    public string? BankName { get; set; }

    [MaxLength(100)]
    public string? AccountNumber { get; set; }

    [MaxLength(30)]
    public string? PhoneNumber { get; set; }

    [MaxLength(200)]
    public string? AccountHolderName { get; set; }

    public bool IsDefault { get; set; }
}

public class UpdatePaymentDestinationDto
{
    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? BankCode { get; set; }

    [MaxLength(150)]
    public string? BankName { get; set; }

    [MaxLength(100)]
    public string? AccountNumber { get; set; }

    [MaxLength(30)]
    public string? PhoneNumber { get; set; }

    [MaxLength(200)]
    public string? AccountHolderName { get; set; }

    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
}
