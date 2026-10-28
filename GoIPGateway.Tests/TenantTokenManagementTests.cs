using System.Text.Json;
using GoIPGateway.Domain.Entities;
using GoIPGateway.Domain.Enums;
using Xunit;

namespace GoIPGateway.Tests;

public class TenantTokenManagementTests
{
    [Fact]
    public void TenantApiKey_Defaults_ShouldBeProperlyInitialized()
    {
        // Arrange & Act
        var key = new TenantApiKey
        {
            TenantId = 1,
            ApiKey = "gw_test_key_12345",
            SecretKey = "sec_test_secret_67890",
            KeyName = "Payment Server"
        };

        // Assert
        Assert.True(key.IsActive);
        Assert.Equal(100, key.RateLimit);
        Assert.Null(key.AllowedServices);
        Assert.Null(key.ExpiresAt);
        Assert.Null(key.LastUsedAt);
    }

    [Fact]
    public void TokenScope_WhenAllowedServicesIsNull_ShouldGrantFullAccess()
    {
        // Arrange
        var allServices = new List<ServiceDefinition>
        {
            new() { Id = 1, ServiceCode = "VF_PAY", NameAr = "فودافون كاش - دفع", Operator = OperatorType.Vodafone },
            new() { Id = 2, ServiceCode = "OR_PAY", NameAr = "أورانج كاش - دفع", Operator = OperatorType.Orange },
            new() { Id = 3, ServiceCode = "ET_PAY", NameAr = "اتصالات كاش - دفع", Operator = OperatorType.Etisalat }
        };

        var key = new TenantApiKey
        {
            ApiKey = "gw_key",
            AllowedServices = null
        };

        // Act
        bool isCustomScope = !string.IsNullOrEmpty(key.AllowedServices);
        var allowed = isCustomScope ? new List<ServiceDefinition>() : allServices;

        // Assert
        Assert.False(isCustomScope);
        Assert.Equal(3, allowed.Count);
    }

    [Fact]
    public void TokenScope_WhenAllowedServicesContainsIntIds_ShouldFilterCorrectly()
    {
        // Arrange
        var allServices = new List<ServiceDefinition>
        {
            new() { Id = 1, ServiceCode = "VF_PAY", NameAr = "فودافون كاش", Operator = OperatorType.Vodafone },
            new() { Id = 2, ServiceCode = "OR_PAY", NameAr = "أورانج كاش", Operator = OperatorType.Orange },
            new() { Id = 3, ServiceCode = "WE_PAY", NameAr = "وي باي", Operator = OperatorType.WE }
        };

        var selectedIds = new List<int> { 1, 3 };
        var key = new TenantApiKey
        {
            ApiKey = "gw_key",
            AllowedServices = JsonSerializer.Serialize(selectedIds)
        };

        // Act
        var deserializedIds = JsonSerializer.Deserialize<List<int>>(key.AllowedServices);
        var matched = allServices.Where(s => deserializedIds!.Contains(s.Id)).ToList();

        // Assert
        Assert.NotNull(deserializedIds);
        Assert.Equal(2, matched.Count);
        Assert.Contains(matched, s => s.ServiceCode == "VF_PAY");
        Assert.Contains(matched, s => s.ServiceCode == "WE_PAY");
        Assert.DoesNotContain(matched, s => s.ServiceCode == "OR_PAY");
    }

    [Theory]
    [InlineData(10, true)]
    [InlineData(-5, false)]
    public void TokenExpiration_RemainingDays_ShouldIdentifyActiveVsExpired(int daysOffset, bool expectValid)
    {
        // Arrange
        var key = new TenantApiKey
        {
            ApiKey = "gw_key",
            ExpiresAt = DateTime.UtcNow.AddDays(daysOffset)
        };

        // Act
        var remainingDays = (int)Math.Ceiling((key.ExpiresAt.Value - DateTime.UtcNow).TotalDays);
        bool isValid = remainingDays > 0;

        // Assert
        Assert.Equal(expectValid, isValid);
    }
}
