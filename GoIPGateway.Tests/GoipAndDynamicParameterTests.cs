using System.Net;
using System.Text.RegularExpressions;
using GoIPGateway.Application.DTOs;
using GoIPGateway.Domain.Entities;
using GoIPGateway.Domain.Enums;
using GoIPGateway.Infrastructure.GoIP;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GoIPGateway.Tests;

public class GoipAndDynamicParameterTests
{
    [Fact]
    public void ExecuteServiceRequestDto_GetNormalizedParameters_MergesTopLevelProperties()
    {
        // Arrange
        var dto = new ExecuteServiceRequestDto
        {
            ServiceCode = "QR_PAY",
            Amount = 350.75m,
            PhoneNumber = "01012345678",
            MerchantCode = "987654",
            Pin = "1234"
        };

        // Act
        var dict = dto.GetNormalizedParameters();

        // Assert
        Assert.True(dict.ContainsKey("amount"));
        Assert.Equal("350.75", dict["amount"]);

        Assert.True(dict.ContainsKey("phoneNumber"));
        Assert.Equal("01012345678", dict["phoneNumber"]);

        Assert.True(dict.ContainsKey("merchantCode"));
        Assert.Equal("987654", dict["merchantCode"]);

        Assert.True(dict.ContainsKey("pin"));
        Assert.Equal("1234", dict["pin"]);
    }

    [Fact]
    public void ExecuteServiceRequestDto_GetNormalizedParameters_IsCaseInsensitive()
    {
        // Arrange
        var dto = new ExecuteServiceRequestDto
        {
            ServiceCode = "VF_PAY",
            Parameters = new Dictionary<string, string>
            {
                { "AMOUNT", "500" },
                { "MerchantCode", "MERCH-01" }
            }
        };

        // Act
        var dict = dto.GetNormalizedParameters();

        // Assert
        Assert.True(dict.ContainsKey("amount"));
        Assert.True(dict.ContainsKey("Amount"));
        Assert.True(dict.ContainsKey("AMOUNT"));
        Assert.Equal("500", dict["amount"]);

        Assert.True(dict.ContainsKey("merchantCode"));
        Assert.True(dict.ContainsKey("MerchantCode"));
        Assert.Equal("MERCH-01", dict["merchantCode"]);
    }

    [Fact]
    public void ExecuteServiceRequestDto_ParametersDictionaryTakesPrecedenceIfSet()
    {
        // Arrange
        var dto = new ExecuteServiceRequestDto
        {
            ServiceCode = "TRANSFER",
            Amount = 100, // Top-level
            Parameters = new Dictionary<string, string>
            {
                { "amount", "250" } // Explicit dictionary override
            }
        };

        // Act
        var dict = dto.GetNormalizedParameters();

        // Assert
        Assert.Equal("250", dict["amount"]);
    }

    [Theory]
    [InlineData("*868*1#", "15420.50")]
    [InlineData("#100#", "8250.00")]
    [InlineData("*555#", "12400.75")]
    [InlineData("*550#", "4500.00")]
    public async Task GoIPHttpClient_SimulateBalanceInquiry_ReturnsValidBalance(string ussdCommand, string expectedAmount)
    {
        // Arrange
        var httpClient = new HttpClient();
        var client = new GoIPHttpClient(httpClient, NullLogger<GoIPHttpClient>.Instance);
        var dummyDevice = new Device
        {
            DeviceName = "Simulated Device",
            IpAddress = "127.0.0.1",
            Port = 80,
            IsActive = true
        };

        // Act: Use offline/dummy IP so it invokes simulation engine
        var response = await client.SendUssdCommandAsync(dummyDevice, 1, ussdCommand, timeoutSeconds: 1);

        // Assert
        Assert.NotNull(response);
        Assert.Contains(expectedAmount, response);
    }

    [Theory]
    [InlineData("*878#", "01000018899")]
    [InlineData("#119#", "01200017766")]
    [InlineData("*947#", "01100015544")]
    [InlineData("*688#", "01500013322")]
    public async Task GoIPHttpClient_SimulateSimNumberInquiry_ReturnsCorrectPhoneNumberForPort(string ussdCommand, string expectedPhone)
    {
        // Arrange
        var httpClient = new HttpClient();
        var client = new GoIPHttpClient(httpClient, NullLogger<GoIPHttpClient>.Instance);
        var dummyDevice = new Device
        {
            DeviceName = "Simulated Device",
            IpAddress = "127.0.0.1",
            Port = 80,
            IsActive = true
        };

        // Act
        var response = await client.SendUssdCommandAsync(dummyDevice, 1, ussdCommand, timeoutSeconds: 1);

        // Assert
        Assert.NotNull(response);
        Assert.Contains(expectedPhone, response);
    }

    [Fact]
    public async Task GoIPHttpClient_SimulatePayment_ReflectsAmountAndGeneratesTransactionSeq()
    {
        // Arrange
        var httpClient = new HttpClient();
        var client = new GoIPHttpClient(httpClient, NullLogger<GoIPHttpClient>.Instance);
        var dummyDevice = new Device
        {
            DeviceName = "Simulated Device",
            IpAddress = "127.0.0.1",
            Port = 80,
            IsActive = true
        };

        // Act: Execute a QR Payment USSD command with amount 500
        var cmd = "*9*2*123456*500*9999#";
        var response = await client.SendUssdCommandAsync(dummyDevice, 1, cmd, timeoutSeconds: 1);

        // Assert
        Assert.NotNull(response);
        Assert.Contains("تمت العملية بنجاح", response);
        Assert.Contains("500.00 ج.م", response);
        Assert.Contains("رقم العملية", response);
    }
}
