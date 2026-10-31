using System.Linq.Expressions;
using GoIPGateway.Application.Common.Behaviors;
using GoIPGateway.Application.Common.Interfaces;
using GoIPGateway.Application.Common.Interfaces.Repositories;
using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.DTOs;
using GoIPGateway.Application.Features.ServiceDefinitions.Commands;
using GoIPGateway.Application.Features.Transactions.Commands;
using GoIPGateway.Application.Services.Transactions;
using GoIPGateway.Domain.Common;
using GoIPGateway.Domain.Entities;
using GoIPGateway.Domain.Enums;
using GoIPGateway.Infrastructure.Services.Executors;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GoIPGateway.Tests;

public class PaymentCycleTests
{
    [Theory]
    [InlineData("تمت العملية بنجاح ورقم الحركة 987654", true)]
    [InlineData("تم تحويل مبلغ 100.00 ج.م إلى 01012345678", true)]
    [InlineData("تم الدفع بنجاح", true)]
    [InlineData("رصيدك غير كافٍ لإتمام العملية", false)]
    [InlineData("الرقم السري غير صحيح", false)]
    [InlineData("فشل التحويل. تم رفض العملية", false)]
    [InlineData("عفواً لا يمكن إتمام العملية حالياً", false)]
    [InlineData("", false)]
    public void DeterminePaymentSuccess_ShouldAccuratelyClassifyNetworkResponses(string response, bool expectedSuccess)
    {
        // Act
        var isSuccess = PaymentExecutor.DeterminePaymentSuccess(response, out var errorMsg, out var successMsg);

        // Assert
        Assert.Equal(expectedSuccess, isSuccess);
        if (expectedSuccess)
        {
            Assert.NotNull(successMsg);
            Assert.Null(errorMsg);
        }
        else
        {
            Assert.NotNull(errorMsg);
            Assert.Null(successMsg);
        }
    }

    [Fact]
    public void ProcessPaymentCommandValidator_WhenServiceCodeIsEmpty_ShouldFailValidation()
    {
        // Arrange
        var validator = new ProcessPaymentCommandValidator();
        var command = new ProcessPaymentCommand(
            ServiceCode: "",
            Amount: 100,
            PhoneNumber: "01012345678"
        );

        // Act
        var result = validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ProcessPaymentCommand.ServiceCode));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-50)]
    public void ProcessPaymentCommandValidator_WhenAmountIsZeroOrNegative_ShouldFailValidation(decimal amount)
    {
        // Arrange
        var validator = new ProcessPaymentCommandValidator();
        var command = new ProcessPaymentCommand(
            ServiceCode: "TRANSFER",
            Amount: amount,
            PhoneNumber: "01012345678"
        );

        // Act
        var result = validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ProcessPaymentCommand.Amount));
    }

    [Theory]
    [InlineData("022345678")] // landline
    [InlineData("12345")]      // too short
    [InlineData("abc010123")]  // letters
    public void ProcessPaymentCommandValidator_WhenPhoneIsInvalid_ShouldFailValidation(string invalidPhone)
    {
        // Arrange
        var validator = new ProcessPaymentCommandValidator();
        var command = new ProcessPaymentCommand(
            ServiceCode: "TRANSFER",
            Amount: 100,
            PhoneNumber: invalidPhone
        );

        // Act
        var result = validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ProcessPaymentCommand.PhoneNumber));
    }

    [Theory]
    [InlineData("01012345678")]
    [InlineData("01112345678")]
    [InlineData("01212345678")]
    [InlineData("01512345678")]
    [InlineData("+201012345678")]
    [InlineData("201012345678")]
    public void ProcessPaymentCommandValidator_WhenPhoneIsValid_ShouldPassValidation(string validPhone)
    {
        // Arrange
        var validator = new ProcessPaymentCommandValidator();
        var command = new ProcessPaymentCommand(
            ServiceCode: "TRANSFER",
            Amount: 100,
            PhoneNumber: validPhone
        );

        // Act
        var result = validator.Validate(command);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ProcessPaymentCommandHandler_ShouldMapParametersAndForwardToService()
    {
        // Arrange
        var mockTxService = new Mock<ITransactionCommandService>();
        ExecuteServiceCommand? capturedCommand = null;

        var expectedResult = new TransactionResultDto
        {
            TransactionId = 1,
            TransactionRef = "GW-20261031-OK",
            Status = TransactionStatus.Completed
        };

        mockTxService.Setup(s => s.ExecuteServiceAsync(It.IsAny<ExecuteServiceCommand>(), It.IsAny<CancellationToken>()))
            .Callback<ExecuteServiceCommand, CancellationToken>((cmd, ct) => capturedCommand = cmd)
            .ReturnsAsync(Result.Success(expectedResult));

        var handler = new ProcessPaymentCommandHandler(mockTxService.Object);

        var command = new ProcessPaymentCommand(
            ServiceCode: "TRANSFER",
            Amount: 250.50m,
            PhoneNumber: "01012345678",
            Operator: OperatorType.Vodafone,
            ExternalRef: "ORD-999"
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(capturedCommand);
        Assert.Equal("TRANSFER", capturedCommand.ServiceCode);
        Assert.Equal(OperatorType.Vodafone, capturedCommand.Operator);
        Assert.Equal("250.5", capturedCommand.Parameters["amount"]);
        Assert.Equal("01012345678", capturedCommand.Parameters["phoneNumber"]);
        Assert.Equal("ORD-999", capturedCommand.ExternalRef);
    }

    [Fact]
    public async Task TenantVerificationBehavior_WhenTenantIdMissing_ShouldFailWithUnauthorizedTenant()
    {
        // Arrange
        var mockTenantProvider = new Mock<ITenantProvider>();
        var mockCurrentUser = new Mock<ICurrentUserService>();
        var mockUow = new Mock<IUnitOfWork>();

        mockTenantProvider.Setup(t => t.GetCurrentTenantId()).Returns((int?)null);
        mockCurrentUser.Setup(c => c.TenantId).Returns((int?)null);

        var behavior = new TenantVerificationBehavior<ProcessPaymentCommand, Result<TransactionResultDto>>(
            mockTenantProvider.Object,
            mockCurrentUser.Object,
            mockUow.Object);

        var command = new ProcessPaymentCommand("TRANSFER", 100);

        // Act
        var result = await behavior.Handle(command, (ct) => Task.FromResult(Result.Success(new TransactionResultDto())), CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Auth.UnauthorizedTenant", result.Error.Code);
    }

    [Fact]
    public async Task TenantVerificationBehavior_WhenTenantIsBlocked_ShouldFailWithTenantBlocked()
    {
        // Arrange
        var mockTenantProvider = new Mock<ITenantProvider>();
        var mockCurrentUser = new Mock<ICurrentUserService>();
        var mockUow = new Mock<IUnitOfWork>();
        var mockTenantRepo = new Mock<ITenantRepository>();

        mockTenantProvider.Setup(t => t.GetCurrentTenantId()).Returns(5);

        var blockedTenant = new Tenant
        {
            Id = 5,
            CompanyName = "Blocked Corp",
            IsActive = true,
            IsBlocked = true,
            BlockReason = "Fraud alert"
        };

        mockTenantRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Tenant, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(blockedTenant);

        mockUow.Setup(u => u.Tenants).Returns(mockTenantRepo.Object);

        var behavior = new TenantVerificationBehavior<ProcessPaymentCommand, Result<TransactionResultDto>>(
            mockTenantProvider.Object,
            mockCurrentUser.Object,
            mockUow.Object);

        var command = new ProcessPaymentCommand("TRANSFER", 100);

        // Act
        var result = await behavior.Handle(command, (ct) => Task.FromResult(Result.Success(new TransactionResultDto())), CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Tenant.Blocked", result.Error.Code);
        Assert.Contains("Fraud alert", result.Error.Message);
    }

    [Fact]
    public async Task TenantVerificationBehavior_WhenTenantIsActive_ShouldProceedToNext()
    {
        // Arrange
        var mockTenantProvider = new Mock<ITenantProvider>();
        var mockCurrentUser = new Mock<ICurrentUserService>();
        var mockUow = new Mock<IUnitOfWork>();
        var mockTenantRepo = new Mock<ITenantRepository>();

        mockTenantProvider.Setup(t => t.GetCurrentTenantId()).Returns(1);

        var activeTenant = new Tenant
        {
            Id = 1,
            CompanyName = "Active Corp",
            IsActive = true,
            IsBlocked = false
        };

        mockTenantRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Tenant, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeTenant);

        mockUow.Setup(u => u.Tenants).Returns(mockTenantRepo.Object);

        var behavior = new TenantVerificationBehavior<ProcessPaymentCommand, Result<TransactionResultDto>>(
            mockTenantProvider.Object,
            mockCurrentUser.Object,
            mockUow.Object);

        var command = new ProcessPaymentCommand("TRANSFER", 100);

        var expectedResult = new TransactionResultDto { TransactionRef = "GW-SUCCESS" };

        // Act
        var result = await behavior.Handle(command, (ct) => Task.FromResult(Result.Success(expectedResult)), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("GW-SUCCESS", result.Value.TransactionRef);
        mockTenantProvider.Verify(t => t.SetCurrentTenantId(1), Times.Once);
    }

    [Fact]
    public void PaymentExecutor_CanHandle_ShouldAcceptPaymentAndTransferTypes()
    {
        // Arrange
        var executor = new PaymentExecutor(null!, null!, null!, null!, null!, NullLogger<PaymentExecutor>.Instance);

        // Assert
        Assert.True(executor.CanHandle(ServiceType.QRPayment));
        Assert.True(executor.CanHandle(ServiceType.Transfer));
        Assert.True(executor.CanHandle(ServiceType.Settlement));
        Assert.False(executor.CanHandle(ServiceType.BalanceInquiry));
        Assert.False(executor.CanHandle(ServiceType.SimNumberInquiry));
        Assert.False(executor.CanHandle(ServiceType.USSD));
    }

    [Fact]
    public void UssdMultiStepExecutor_CanHandle_ShouldOnlyAcceptGenericUssdAndCustom()
    {
        // Arrange
        var executor = new UssdMultiStepExecutor(null!, null!, null!, null!, null!, NullLogger<UssdMultiStepExecutor>.Instance);

        // Assert
        Assert.True(executor.CanHandle(ServiceType.USSD));
        Assert.True(executor.CanHandle(ServiceType.Custom));
        Assert.False(executor.CanHandle(ServiceType.QRPayment));
        Assert.False(executor.CanHandle(ServiceType.Transfer));
        Assert.False(executor.CanHandle(ServiceType.Settlement));
    }

    [Theory]
    [InlineData("amount")]
    [InlineData("phoneNumber")]
    [InlineData("nationalId")]
    [InlineData("accountNumber")]
    [InlineData("invoiceNumber")]
    [InlineData("otpCode")]
    public void AddServiceParameterCommandValidator_WhenParameterKeyIsAllowedStandard_ShouldPassValidation(string key)
    {
        // Arrange
        var validator = new AddServiceParameterCommandValidator();
        var param = new ServiceParameter
        {
            ServiceDefinitionId = 1,
            ParameterKey = key,
            DisplayNameAr = "تجربة"
        };
        var command = new AddServiceParameterCommand(param);

        // Act
        var result = validator.Validate(command);

        // Assert
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("password")]
    [InlineData("pin")]
    [InlineData("walletpassword")]
    [InlineData("merchantCode")]
    [InlineData("simNumber")]
    public void AddServiceParameterCommandValidator_WhenParameterKeyIsReservedInternalKey_ShouldFailValidation(string internalKey)
    {
        // Arrange
        var validator = new AddServiceParameterCommandValidator();
        var param = new ServiceParameter
        {
            ServiceDefinitionId = 1,
            ParameterKey = internalKey,
            DisplayNameAr = "داخلي"
        };
        var command = new AddServiceParameterCommand(param);

        // Act
        var result = validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("مفاتيح النظام الداخلية"));
    }

    [Theory]
    [InlineData("randomKey")]
    [InlineData("some_arbitrary_input")]
    [InlineData("creditCard")]
    public void AddServiceParameterCommandValidator_WhenParameterKeyIsArbitrary_ShouldFailValidation(string unknownKey)
    {
        // Arrange
        var validator = new AddServiceParameterCommandValidator();
        var param = new ServiceParameter
        {
            ServiceDefinitionId = 1,
            ParameterKey = unknownKey,
            DisplayNameAr = "عشوائي"
        };
        var command = new AddServiceParameterCommand(param);

        // Act
        var result = validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("الأنواع الثابتة المعيارية المعتمدة"));
    }
}
