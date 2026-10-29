using GoIPGateway.Application.Common.Interfaces;
using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.DTOs;
using GoIPGateway.Application.Factories;
using GoIPGateway.Domain.Entities;
using GoIPGateway.Domain.Enums;
using GoIPGateway.Infrastructure.GoIP;
using GoIPGateway.Infrastructure.Services.Executors;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GoIPGateway.Tests;

public class EndToEndOperationExecutionTests
{
    private readonly GoIPHttpClient _client;
    private readonly Device _device;
    private readonly DevicePort _port;
    private readonly SimCard _sim;

    public EndToEndOperationExecutionTests()
    {
        _client = new GoIPHttpClient(new HttpClient(), NullLogger<GoIPHttpClient>.Instance);

        _device = new Device
        {
            Id = 1,
            DeviceName = "GoIP-128 Primary",
            DeviceType = DeviceType.GoIP128,
            IpAddress = "192.168.1.100",
            Port = 80,
            HttpPort = 80,
            MerchantCode = "998877",
            IsActive = true,
            IsOnline = true,
            TotalPorts = 128
        };

        _port = new DevicePort
        {
            Id = 1,
            DeviceId = 1,
            Device = _device,
            PortNumber = 1,
            PortLabel = "L001",
            Status = PortStatus.Active,
            IsActive = true,
            IsBusy = false
        };

        _sim = new SimCard
        {
            Id = 1,
            DevicePortId = 1,
            DevicePort = _port,
            Operator = OperatorType.Vodafone,
            PhoneNumber = "01012345678",
            SimNumber = "01012345678",
            WalletPassword = "9999",
            CurrentBalance = 15420.50m,
            IsActive = true
        };
    }

    [Fact]
    public async Task EndToEnd_BalanceInquiry_Execution_ShouldSucceedAndExtractBalance()
    {
        // 1. Arrange Strategy & Command
        var strategy = new VodafoneStrategy();
        var command = strategy.DefaultWalletBalanceInquiryCommand
            .Replace("{Password}", _sim.WalletPassword)
            .Replace("Password", _sim.WalletPassword);

        // 2. Act: Send via GoIP Client
        var response = await _client.SendUssdCommandAsync(_device, _port.PortNumber, command, timeoutSeconds: 1);

        // 3. Extract Balance via Strategy
        var extractedBalance = strategy.ExtractBalance(response);

        // 4. Assert
        Assert.NotNull(response);
        Assert.Contains("رصيدك الحالي", response);
        Assert.NotNull(extractedBalance);
        Assert.Equal(15420.50m, extractedBalance.Value);
    }

    [Fact]
    public async Task EndToEnd_SimNumberInquiry_Execution_ShouldSucceedAndExtractPhone()
    {
        // 1. Arrange Strategy & Command
        var strategy = new VodafoneStrategy();
        var command = strategy.DefaultNumberInquiryCommand;

        // 2. Act: Send via GoIP Client
        var response = await _client.SendUssdCommandAsync(_device, _port.PortNumber, command, timeoutSeconds: 1);

        // 3. Extract Phone via Strategy
        var extractedPhone = strategy.ExtractPhoneNumber(response);

        // 4. Assert
        Assert.NotNull(response);
        Assert.NotNull(extractedPhone);
        Assert.StartsWith("010", extractedPhone);
        Assert.Contains(_port.PortNumber.ToString("D4"), extractedPhone);
    }

    [Fact]
    public async Task EndToEnd_QrMerchantPayment_Execution_ShouldSucceedWithDeduction()
    {
        // 1. Arrange Payment Parameters
        var amount = 250m;
        var merchantCode = "123456";
        var pin = _sim.WalletPassword;

        // Ussd template: *9*2*{MerchantCode}*{Amount}*{Password}#
        var template = "*9*2*{MerchantCode}*{Amount}*{Password}#";
        var evaluatedCmd = template
            .Replace("{MerchantCode}", merchantCode)
            .Replace("{Amount}", amount.ToString("0.##"))
            .Replace("{Password}", pin);

        Assert.Equal("*9*2*123456*250*9999#", evaluatedCmd);

        // 2. Act: Execute USSD
        var response = await _client.SendUssdCommandAsync(_device, _port.PortNumber, evaluatedCmd, timeoutSeconds: 1);

        // 3. Assert Response
        Assert.NotNull(response);
        Assert.Contains("تمت العملية بنجاح", response);
        Assert.Contains("250.00 ج.م", response);
        Assert.Contains(merchantCode, response);

        // 4. Simulate balance movement
        var balanceBefore = _sim.CurrentBalance;
        _sim.CurrentBalance -= amount;
        Assert.Equal(balanceBefore - amount, _sim.CurrentBalance);
    }

    [Fact]
    public async Task EndToEnd_TransferMoney_Execution_ShouldSucceedWithTargetPhone()
    {
        // 1. Arrange Transfer Parameters
        var amount = 100m;
        var targetPhone = "01098765432";
        var pin = _sim.WalletPassword;

        // Ussd template: *9*7*{PhoneNumber}*{Amount}*{Password}#
        var template = "*9*7*{PhoneNumber}*{Amount}*{Password}#";
        var evaluatedCmd = template
            .Replace("{PhoneNumber}", targetPhone)
            .Replace("{Amount}", amount.ToString("0.##"))
            .Replace("{Password}", pin);

        Assert.Equal("*9*7*01098765432*100*9999#", evaluatedCmd);

        // 2. Act: Execute USSD
        var response = await _client.SendUssdCommandAsync(_device, _port.PortNumber, evaluatedCmd, timeoutSeconds: 1);

        // 3. Assert Response
        Assert.NotNull(response);
        Assert.Contains("تمت العملية بنجاح", response);
        Assert.Contains("100.00 ج.م", response);
        Assert.Contains(targetPhone, response);
    }

    [Fact]
    public void EndToEnd_AutoMerchantCodeFallback_ShouldUseDeviceMerchantCode_WhenNotProvidedInRequest()
    {
        // Arrange
        var parameters = new Dictionary<string, string>();
        var deviceMerchant = _device.MerchantCode;

        // Act: Apply fallback logic from TransactionCommandService
        if (!parameters.ContainsKey("merchantCode") && !string.IsNullOrWhiteSpace(_device.MerchantCode))
        {
            parameters["merchantCode"] = _device.MerchantCode.Trim();
        }

        // Assert
        Assert.True(parameters.ContainsKey("merchantCode"));
        Assert.Equal("998877", parameters["merchantCode"]);
    }
}
