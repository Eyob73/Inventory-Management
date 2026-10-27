using System;

namespace Inventory_Management.Application.Common;

public static class DateRangeHelper
{
    public static (DateTime? StartUtc, DateTime? EndUtc) ResolveDateRange(
        string? dateRange,
        DateTime? startDate = null,
        DateTime? endDate = null,
        DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;
        var today = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc);

        DateTime? startUtc = null;
        DateTime? endUtc = null;

        if (!string.IsNullOrWhiteSpace(dateRange))
        {
            var normalized = dateRange.Trim().ToLowerInvariant().Replace("-", "_").Replace(" ", "_");
            switch (normalized)
            {
                case "today":
                    startUtc = startDate.HasValue
                        ? DateTime.SpecifyKind(startDate.Value.Date, DateTimeKind.Utc)
                        : today;
                    endUtc = endDate.HasValue
                        ? DateTime.SpecifyKind(endDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc)
                        : today.AddDays(1).AddTicks(-1);
                    break;

                case "yesterday":
                    startUtc = startDate.HasValue
                        ? DateTime.SpecifyKind(startDate.Value.Date, DateTimeKind.Utc)
                        : today.AddDays(-1);
                    endUtc = endDate.HasValue
                        ? DateTime.SpecifyKind(endDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc)
                        : today.AddTicks(-1);
                    break;

                case "this_week":
                case "thisweek":
                case "week":
                    if (startDate.HasValue && endDate.HasValue)
                    {
                        startUtc = DateTime.SpecifyKind(startDate.Value.Date, DateTimeKind.Utc);
                        endUtc = DateTime.SpecifyKind(endDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
                    }
                    else
                    {
                        int diff = (7 + (int)now.DayOfWeek - (int)DayOfWeek.Monday) % 7;
                        var monday = today.AddDays(-diff);
                        startUtc = monday;
                        endUtc = monday.AddDays(7).AddTicks(-1);
                    }
                    break;

                case "this_month":
                case "thismonth":
                case "month":
                    if (startDate.HasValue && endDate.HasValue)
                    {
                        startUtc = DateTime.SpecifyKind(startDate.Value.Date, DateTimeKind.Utc);
                        endUtc = DateTime.SpecifyKind(endDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
                    }
                    else
                    {
                        var firstDay = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                        startUtc = firstDay;
                        endUtc = firstDay.AddMonths(1).AddTicks(-1);
                    }
                    break;

                case "this_year":
                case "thisyear":
                case "year":
                    if (startDate.HasValue && endDate.HasValue)
                    {
                        startUtc = DateTime.SpecifyKind(startDate.Value.Date, DateTimeKind.Utc);
                        endUtc = DateTime.SpecifyKind(endDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
                    }
                    else
                    {
                        var firstDayOfYear = new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                        startUtc = firstDayOfYear;
                        endUtc = firstDayOfYear.AddYears(1).AddTicks(-1);
                    }
                    break;

                case "custom":
                case "all":
                case "all_time":
                case "alltime":
                default:
                    // Handled below via startDate and endDate fallback
                    break;
            }
        }

        if (!startUtc.HasValue && startDate.HasValue)
        {
            startUtc = DateTime.SpecifyKind(startDate.Value.Date, DateTimeKind.Utc);
        }

        if (!endUtc.HasValue && endDate.HasValue)
        {
            endUtc = DateTime.SpecifyKind(endDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
        }

        return (startUtc, endUtc);
    }
}
