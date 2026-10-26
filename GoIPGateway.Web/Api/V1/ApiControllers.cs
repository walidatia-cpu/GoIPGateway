using GoIPGateway.Application.Common.Interfaces;
using GoIPGateway.Application.DTOs;
using GoIPGateway.Application.Features.Transactions.Commands;
using GoIPGateway.Application.Features.Transactions.Queries;
using GoIPGateway.Domain.Enums;
using GoIPGateway.Infrastructure.Services;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GoIPGateway.Web.Api.V1;

[ApiController]
[Route("api/v1/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAppDbContext _context;
    private readonly IJwtTokenService _jwtService;

    public AuthController(IAppDbContext context, IJwtTokenService jwtService)
    {
        _context = context;
        _jwtService = jwtService;
    }

    public record TokenRequest(string ApiKey, string SecretKey);
    public record TokenResponse(string Token, int ExpiresIn, string TokenType = "Bearer");

    /// <summary>
    /// الحصول على رمز JWT باستخدام مفاتيح API الخاصة بالشركة
    /// </summary>
    [HttpPost("token")]
    public async Task<IActionResult> GenerateToken([FromBody] TokenRequest request)
    {
        var key = await _context.TenantApiKeys
            .Include(k => k.Tenant)
            .FirstOrDefaultAsync(k => k.ApiKey == request.ApiKey && k.SecretKey == request.SecretKey && k.IsActive);

        if (key == null || !key.Tenant.IsActive || key.Tenant.IsBlocked)
        {
            return Unauthorized(new { message = "Invalid API Key or Secret Key, or Tenant account is suspended." });
        }

        key.LastUsedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var token = _jwtService.GenerateToken(key.TenantId.ToString(), key.Tenant.CompanyName, "TenantApi", key.TenantId, key.AllowedServices);
        return Ok(new TokenResponse(token, 86400));
    }
}

[ApiController]
[Route("api/v1/services")]
[Authorize]
[Produces("application/json")]
public class GatewayController : ControllerBase
{
    private readonly IMediator _mediator;

    public GatewayController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// تنفيذ خدمة عبر بوابة GoIP (USSD / QR / Transfer / إلخ)
    /// </summary>
    [HttpPost("execute")]
    public async Task<IActionResult> Execute([FromBody] ExecuteServiceRequestDto request)
    {
        var command = new ExecuteServiceCommand(
            request.ServiceCode,
            request.ExternalRef,
            request.SimCardId,
            request.Operator,
            request.Parameters,
            request.CallbackUrl,
            RequestSource.API
        );

        var result = await _mediator.Send(command);
        if (result.IsFailure)
        {
            return BadRequest(new
            {
                success = false,
                errorCode = result.Error.Code,
                message = result.Error.Message,
                validationErrors = result.ValidationErrors
            });
        }

        return Ok(result.Value);
    }
}

[ApiController]
[Route("api/v1/transactions")]
[Authorize]
[Produces("application/json")]
public class TransactionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public TransactionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// الاستعلام عن حالة ونتيجة عملية باستخدام المرجع
    /// </summary>
    [HttpGet("{transactionRef}")]
    public async Task<IActionResult> GetByRef(string transactionRef)
    {
        var result = await _mediator.Send(new GetTransactionByRefQuery(transactionRef));
        if (result.IsFailure)
            return NotFound(new { success = false, message = result.Error.Message });

        return Ok(result.Value);
    }

    /// <summary>
    /// استرجاع قائمة العمليات مع ترقيم الصفحات والفلاتر
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] int? simCardId,
        [FromQuery] TransactionStatus? status,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetTransactionsListQuery(
            SimCardId: simCardId,
            Status: status,
            FromDate: from,
            ToDate: to,
            PageNumber: page,
            PageSize: pageSize
        ));

        return Ok(result.Value);
    }
}

[ApiController]
[Route("api/v1/inquiry")]
[Authorize]
[Produces("application/json")]
public class InquiryController : ControllerBase
{
    private readonly IMediator _mediator;

    public InquiryController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// استعلام فوري عن رصيد الشريحة عبر USSD
    /// </summary>
    [HttpPost("balance")]
    public async Task<IActionResult> QueryBalance([FromBody] BalanceInquiryRequestDto request)
    {
        var command = new ExecuteServiceCommand(
            "BAL_INQ",
            null,
            request.SimCardId,
            request.Operator,
            new Dictionary<string, string>(),
            null,
            RequestSource.API
        );

        var result = await _mediator.Send(command);
        if (result.IsFailure)
            return BadRequest(new { success = false, message = result.Error.Message });

        return Ok(result.Value);
    }

    /// <summary>
    /// استعلام فوري عن رقم الشريحة عبر USSD
    /// </summary>
    [HttpPost("sim-number")]
    public async Task<IActionResult> QuerySimNumber([FromBody] SimNumberInquiryRequestDto request)
    {
        var command = new ExecuteServiceCommand(
            "NUM_INQ",
            null,
            request.SimCardId,
            null,
            new Dictionary<string, string>(),
            null,
            RequestSource.API
        );

        var result = await _mediator.Send(command);
        if (result.IsFailure)
            return BadRequest(new { success = false, message = result.Error.Message });

        return Ok(result.Value);
    }
}

[ApiController]
[Route("api/v1/account")]
[Authorize]
[Produces("application/json")]
public class AccountApiController : ControllerBase
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantProvider _tenantProvider;

    public AccountApiController(IAppDbContext context, ICurrentUserService currentUser, ITenantProvider tenantProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _tenantProvider = tenantProvider;
    }

    /// <summary>
    /// استرجاع بيانات رصيد الشركة وحدود الاستخدام الحالية
    /// </summary>
    [HttpGet("balance")]
    public async Task<IActionResult> GetBalance()
    {
        var tenantId = _tenantProvider.GetCurrentTenantId() ?? _currentUser.TenantId;
        if (!tenantId.HasValue)
            return Unauthorized();

        var tenant = await _context.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tenantId.Value);

        if (tenant == null)
            return NotFound();

        return Ok(new
        {
            companyName = tenant.CompanyName,
            balance = tenant.Balance,
            creditLimit = tenant.CreditLimit,
            maxDailyOperations = tenant.MaxDailyOperations,
            isActive = tenant.IsActive,
            isBlocked = tenant.IsBlocked
        });
    }
}
