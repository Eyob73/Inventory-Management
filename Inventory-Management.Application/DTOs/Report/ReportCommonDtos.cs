namespace Inventory_Management.Application.DTOs.Report;

public class PeriodMetricDto
{
    public decimal Value { get; set; }
    public decimal PreviousValue { get; set; }
    public decimal? ChangePercent { get; set; }
}

public class ChartPointDto
{
    public string Label { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public int Count { get; set; }
}

public class NamedAmountDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int Quantity { get; set; }
}

public class ReportExportFile
{
    public required byte[] Content { get; init; }
    public required string ContentType { get; init; }
    public required string FileName { get; init; }
}
