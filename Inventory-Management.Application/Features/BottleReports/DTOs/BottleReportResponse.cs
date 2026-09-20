using System;
using System.Collections.Generic;

namespace Inventory_Management.Application.Features.BottleReports.DTOs;

public class BottleReportResponse
{
    public string ReportType { get; set; } = string.Empty;
    public List<BottleMovementReportDto> MovementData { get; set; } = new();
    public List<BottleDepositReportDto> DepositData { get; set; } = new();
    public List<CustomerBottleBalanceReportDto> CustomerData { get; set; } = new();
    public List<BottleLossReportDto> LossData { get; set; } = new();
}

public class BottleMovementReportDto
{
    public DateTime Date { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    public string BottleTypeName { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Reference { get; set; } = string.Empty;
}

public class BottleDepositReportDto
{
    public DateTime Date { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string BottleTypeName { get; set; } = string.Empty;
    public decimal DepositAmount { get; set; }
    public string TransactionType { get; set; } = string.Empty;
}

public class CustomerBottleBalanceReportDto
{
    public string CustomerName { get; set; } = string.Empty;
    public string BottleTypeName { get; set; } = string.Empty;
    public int UnreturnedBottles { get; set; }
    public decimal TotalDeposit { get; set; }
    public DateTime LastUpdatedAt { get; set; }
}

public class BottleLossReportDto
{
    public DateTime Date { get; set; }
    public string BottleTypeName { get; set; } = string.Empty;
    public string TransactionType { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Notes { get; set; } = string.Empty;
}
