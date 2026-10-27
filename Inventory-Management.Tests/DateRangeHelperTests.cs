using System;
using Inventory_Management.Application.Common;
using Xunit;

namespace Inventory_Management.Tests;

public class DateRangeHelperTests
{
    private readonly DateTime _fixedNow = new DateTime(2026, 10, 26, 14, 30, 0, DateTimeKind.Utc); // Monday, Oct 26, 2026

    [Theory]
    [InlineData("today")]
    [InlineData("Today")]
    [InlineData(" TODAY ")]
    public void ResolveDateRange_Today_ReturnsStartAndEndOfToday(string preset)
    {
        var (start, end) = DateRangeHelper.ResolveDateRange(preset, utcNow: _fixedNow);

        Assert.NotNull(start);
        Assert.NotNull(end);
        Assert.Equal(new DateTime(2026, 10, 26, 0, 0, 0, DateTimeKind.Utc), start.Value);
        Assert.Equal(new DateTime(2026, 10, 26, 23, 59, 59, DateTimeKind.Utc).AddTicks(9999999), end.Value);
    }

    [Theory]
    [InlineData("yesterday")]
    [InlineData("Yesterday")]
    public void ResolveDateRange_Yesterday_ReturnsStartAndEndOfYesterday(string preset)
    {
        var (start, end) = DateRangeHelper.ResolveDateRange(preset, utcNow: _fixedNow);

        Assert.NotNull(start);
        Assert.NotNull(end);
        Assert.Equal(new DateTime(2026, 10, 25, 0, 0, 0, DateTimeKind.Utc), start.Value);
        Assert.Equal(new DateTime(2026, 10, 25, 23, 59, 59, DateTimeKind.Utc).AddTicks(9999999), end.Value);
    }

    [Theory]
    [InlineData("this_week")]
    [InlineData("thisweek")]
    [InlineData("This Week")]
    [InlineData("week")]
    public void ResolveDateRange_ThisWeek_ReturnsMondayToSunday(string preset)
    {
        // 2026-10-28 is a Wednesday
        var wednesday = new DateTime(2026, 10, 28, 12, 0, 0, DateTimeKind.Utc);
        var (start, end) = DateRangeHelper.ResolveDateRange(preset, utcNow: wednesday);

        Assert.NotNull(start);
        Assert.NotNull(end);
        Assert.Equal(new DateTime(2026, 10, 26, 0, 0, 0, DateTimeKind.Utc), start.Value); // Monday
        Assert.Equal(new DateTime(2026, 11, 1, 23, 59, 59, DateTimeKind.Utc).AddTicks(9999999), end.Value); // Sunday
    }

    [Theory]
    [InlineData("this_month")]
    [InlineData("thismonth")]
    [InlineData("This Month")]
    [InlineData("month")]
    public void ResolveDateRange_ThisMonth_ReturnsFullCurrentMonth(string preset)
    {
        var (start, end) = DateRangeHelper.ResolveDateRange(preset, utcNow: _fixedNow);

        Assert.NotNull(start);
        Assert.NotNull(end);
        Assert.Equal(new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc), start.Value);
        Assert.Equal(new DateTime(2026, 10, 31, 23, 59, 59, DateTimeKind.Utc).AddTicks(9999999), end.Value);
    }

    [Theory]
    [InlineData("this_year")]
    [InlineData("thisyear")]
    [InlineData("This Year")]
    [InlineData("year")]
    public void ResolveDateRange_ThisYear_ReturnsFullCurrentYear(string preset)
    {
        var (start, end) = DateRangeHelper.ResolveDateRange(preset, utcNow: _fixedNow);

        Assert.NotNull(start);
        Assert.NotNull(end);
        Assert.Equal(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), start.Value);
        Assert.Equal(new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc).AddTicks(9999999), end.Value);
    }

    [Fact]
    public void ResolveDateRange_Custom_UsesExplicitDates()
    {
        var customStart = new DateTime(2026, 5, 10);
        var customEnd = new DateTime(2026, 5, 20);

        var (start, end) = DateRangeHelper.ResolveDateRange("custom", customStart, customEnd, utcNow: _fixedNow);

        Assert.NotNull(start);
        Assert.NotNull(end);
        Assert.Equal(new DateTime(2026, 5, 10, 0, 0, 0, DateTimeKind.Utc), start.Value);
        Assert.Equal(new DateTime(2026, 5, 20, 23, 59, 59, DateTimeKind.Utc).AddTicks(9999999), end.Value);
    }

    [Fact]
    public void ResolveDateRange_NullOrEmptyPreset_UsesExplicitDatesIfProvided()
    {
        var customStart = new DateTime(2026, 6, 1);
        var (start, end) = DateRangeHelper.ResolveDateRange(null, customStart, null, utcNow: _fixedNow);

        Assert.NotNull(start);
        Assert.Null(end);
        Assert.Equal(new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc), start.Value);
    }

    [Fact]
    public void ResolveDateRange_AllTime_ReturnsNullDates()
    {
        var (start, end) = DateRangeHelper.ResolveDateRange("all", null, null, utcNow: _fixedNow);

        Assert.Null(start);
        Assert.Null(end);
    }
}
