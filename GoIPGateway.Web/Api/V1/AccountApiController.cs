using GoIPGateway.Application.Common.Interfaces;
using GoIPGateway.Application.Features.Tenants.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoIPGateway.Web.Api.V1;

[ApiController]
[Route("api/v1/account")]
[Authorize]
[Produces("application/json")]
public class AccountApiController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantProvider _tenantProvider;

    public AccountApiController(IMediator mediator, ICurrentUserService currentUser, ITenantProvider tenantProvider)
    {
        _mediator = mediator;
        _currentUser = currentUser;
        _tenantProvider = tenantProvider;
    }

    /// <summary>
    /// استرجاع بيانات رصيد الشركة وحدود الاستخدام الحالية
    /// </summary>
    [HttpGet("balance")]
    public async Task<IActionResult> GetBalance(CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId() ?? _currentUser.TenantId;
        if (!tenantId.HasValue)
            return Unauthorized();

        var result = await _mediator.Send(new GetTenantByIdQuery(tenantId.Value), cancellationToken);
        if (!result.Succeeded || result.Data == null)
            return NotFound();

        var tenant = result.Data;
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