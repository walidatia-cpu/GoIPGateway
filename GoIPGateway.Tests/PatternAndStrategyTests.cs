using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.Factories;
using GoIPGateway.Domain.Enums;
using Xunit;

namespace GoIPGateway.Tests;

public class PatternAndStrategyTests
{
    [Fact]
    public void ResultPattern_SuccessResult_ShouldHoldValue()
    {
        // Arrange & Act
        var result = Result.Success("Test Transaction Ref");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal("Test Transaction Ref", result.Value);
        Assert.Equal(Error.None, result.Error);
    }

    [Fact]
    public void ResultPattern_FailureResult_ShouldHoldError()
    {
        // Arrange & Act
        var result = Result.Failure<string>("SIM.Locked", "Port is currently locked");

        // Assert
        Assert.True(result.IsFailure);
        Assert.False(result.IsSuccess);
        Assert.Equal("SIM.Locked", result.Error.Code);
        Assert.Equal("Port is currently locked", result.Error.Message);
        Assert.Throws<InvalidOperationException>(() => _ = result.Value);
    }

    [Fact]
    public void ResultPattern_ValidationFailure_ShouldContainMultipleErrors()
    {
        // Arrange
        var errors = new List<Error>
        {
            Error.Validation("Amount", "Amount must be greater than zero"),
            Error.Validation("MerchantCode", "Merchant code is required")
        };

        // Act
        var result = Result.ValidationFailure<bool>(errors);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(2, result.ValidationErrors.Count);
        Assert.Contains(result.ValidationErrors, e => e.Code == "Amount");
    }

    [Theory]
    [InlineData("رصيدك الحالي هو 15420.50 جنيه مصري صالح حتى 31/12/2026", 15420.50)]
    [InlineData("Your balance is 500.25 EGP", 500.25)]
    [InlineData("الرصيد المتاح هو: 1200", 1200.0)]
    public void VodafoneStrategy_ExtractBalance_ShouldParseCorrectly(string response, decimal expected)
    {
        // Arrange
        var strategy = new VodafoneStrategy();

        // Act
        var balance = strategy.ExtractBalance(response);

        // Assert
        Assert.NotNull(balance);
        Assert.Equal(expected, balance!.Value);
    }

    [Theory]
    [InlineData("رقم هاتفك هو 01012345678", "01012345678")]
    public void VodafoneStrategy_ExtractPhoneNumber_ShouldParseCorrectly(string response, string expected)
    {
        // Arrange
        var strategy = new VodafoneStrategy();

        // Act
        var phone = strategy.ExtractPhoneNumber(response);

        // Assert
        Assert.Equal(expected, phone);
    }

    [Fact]
    public void OperatorStrategyFactory_ShouldResolveCorrectStrategy()
    {
        // Arrange
        var strategies = new List<IOperatorStrategy>
        {
            new VodafoneStrategy(),
            new OrangeStrategy(),
            new EtisalatStrategy(),
            new WeStrategy()
        };
        var factory = new OperatorStrategyFactory(strategies);

        // Act
        var vf = factory.GetStrategy(OperatorType.Vodafone);
        var orange = factory.GetStrategy(OperatorType.Orange);
        var etisalat = factory.GetStrategy(OperatorType.Etisalat);
        var we = factory.GetStrategy(OperatorType.WE);

        // Assert
        Assert.Equal(OperatorType.Vodafone, vf.Operator);
        Assert.Equal(OperatorType.Orange, orange.Operator);
        Assert.Equal(OperatorType.Etisalat, etisalat.Operator);
        Assert.Equal(OperatorType.WE, we.Operator);
    }
}
