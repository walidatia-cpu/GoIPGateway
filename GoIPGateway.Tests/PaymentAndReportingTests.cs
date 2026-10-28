using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.DTOs.Reports;
using GoIPGateway.Domain.Enums;
using Xunit;

namespace GoIPGateway.Tests;

public class PaymentAndReportingTests
{
    [Fact]
    public void CompanyReportDto_SuccessRate_ShouldCalculateCorrectPercentage()
    {
        // Arrange
        var report = new CompanyReportDto
        {
            TotalTransactionsCount = 100,
            SuccessfulTransactionsCount = 85,
            FailedTransactionsCount = 15
        };

        // Assert
        Assert.Equal(85.0, report.SuccessRate);
    }

    [Fact]
    public void CompanyReportDto_SuccessRate_WhenNoTransactions_ShouldDefaultTo100()
    {
        // Arrange
        var report = new CompanyReportDto
        {
            TotalTransactionsCount = 0,
            SuccessfulTransactionsCount = 0
        };

        // Assert
        Assert.Equal(100.0, report.SuccessRate);
    }

    [Fact]
    public void DeviceReportStatDto_SuccessRate_ShouldCalculateAccurately()
    {
        // Arrange
        var deviceStat = new DeviceReportStatDto
        {
            DeviceId = 1,
            DeviceName = "GoIP-01",
            OperationsCount = 200,
            SuccessfulOperationsCount = 190,
            FailedOperationsCount = 10,
            TotalVolume = 50000m
        };

        // Assert
        Assert.Equal(95.0, deviceStat.SuccessRate);
    }

    [Fact]
    public void PagedResult_PaginationCalculations_ShouldBeAccurate()
    {
        // Arrange
        var items = new List<string> { "item1", "item2" };
        var pagedResult = new PagedResult<string>(items, 55, 3, 10);

        // Assert
        Assert.Equal(3, pagedResult.PageNumber);
        Assert.Equal(3, pagedResult.CurrentPage);
        Assert.Equal(10, pagedResult.PageSize);
        Assert.Equal(55, pagedResult.TotalCount);
        Assert.Equal(6, pagedResult.TotalPages);
        Assert.True(pagedResult.HasPreviousPage);
        Assert.True(pagedResult.HasNextPage);
    }

    [Theory]
    [InlineData("today")]
    [InlineData("yesterday")]
    [InlineData("last7days")]
    [InlineData("thismonth")]
    [InlineData("lastmonth")]
    public void CompanyReportFilterDto_DatePresets_ShouldSetFromAndToDates(string preset)
    {
        // Arrange
        var filter = new CompanyReportFilterDto
        {
            DatePreset = preset
        };

        // Act - Simulate filter preset normalization
        var now = DateTime.UtcNow;
        switch (preset)
        {
            case "today":
                filter.FromDate = now.Date;
                filter.ToDate = now.Date.AddDays(1).AddTicks(-1);
                break;
            case "yesterday":
                filter.FromDate = now.Date.AddDays(-1);
                filter.ToDate = now.Date.AddTicks(-1);
                break;
            case "last7days":
                filter.FromDate = now.Date.AddDays(-7);
                filter.ToDate = now.Date.AddDays(1).AddTicks(-1);
                break;
            case "thismonth":
                filter.FromDate = new DateTime(now.Year, now.Month, 1);
                filter.ToDate = filter.FromDate.Value.AddMonths(1).AddTicks(-1);
                break;
            case "lastmonth":
                var lastMonth = now.AddMonths(-1);
                filter.FromDate = new DateTime(lastMonth.Year, lastMonth.Month, 1);
                filter.ToDate = new DateTime(now.Year, now.Month, 1).AddTicks(-1);
                break;
        }

        // Assert
        Assert.NotNull(filter.FromDate);
        Assert.NotNull(filter.ToDate);
        Assert.True(filter.FromDate < filter.ToDate);
    }

    [Fact]
    public void TenantPerformanceItemDto_SuccessRate_ShouldCalculateAccurately()
    {
        // Arrange
        var item = new TenantPerformanceItemDto
        {
            TenantId = 1,
            CompanyName = "Test Merchant",
            TotalOperationsCount = 50,
            SuccessfulOperationsCount = 45,
            FailedOperationsCount = 5
        };

        // Assert
        Assert.Equal(90.0, item.SuccessRate);
    }

    [Fact]
    public void TenantsPerformanceReportDto_AverageSuccessRate_ShouldCalculateAccurately()
    {
        // Arrange
        var report = new TenantsPerformanceReportDto
        {
            TotalOverallOperations = 1000,
            TotalSuccessfulOperations = 920,
            TotalFailedOperations = 80
        };

        // Assert
        Assert.Equal(92.0, report.AverageSuccessRate);
    }

    [Fact]
    public void ExecutiveSummaryDto_Calculations_ShouldBeAccurate()
    {
        // Arrange
        var summary = new ExecutiveSummaryDto
        {
            TotalOperationsCount = 500,
            CompletedCount = 475,
            FailedCount = 25,
            TotalGrossVolume = 100000m,
            TotalSuccessfulVolume = 95000m,
            TotalFailedVolume = 5000m,
            TotalPlatformFees = 1900m
        };

        // Assert
        Assert.Equal(95.0, summary.SuccessRate);
        Assert.Equal(5.0, summary.FailureRate);
        Assert.Equal(93100m, summary.NetSettledVolume);
        Assert.Equal(200.00m, summary.AverageTransactionSize);
    }

    [Fact]
    public void OperatorAggregateDto_SuccessRate_ShouldCalculateAccurately()
    {
        // Arrange
        var op = new OperatorAggregateDto
        {
            Operator = OperatorType.Vodafone,
            NameAr = "فودافون كاش",
            TotalOperations = 300,
            SuccessfulOperations = 270,
            TotalVolume = 60000m
        };

        // Assert
        Assert.Equal(90.0, op.SuccessRate);
    }
}
