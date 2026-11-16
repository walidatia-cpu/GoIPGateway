using GoIPGateway.Application.Common.Interfaces;
using GoIPGateway.Application.Common.Security;
using GoIPGateway.Application.Features.Tenants.Queries;
using GoIPGateway.Application.Features.Transactions.Queries;
using GoIPGateway.Domain.Enums;
using GoIPGateway.Infrastructure.Identity.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoIPGateway.Web.Areas.Company.Controllers;

[Area("Company")]
[Authorize]
public class TransactionsController : Controller
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantProvider _tenantProvider;

    public TransactionsController(IMediator mediator, ICurrentUserService currentUser, ITenantProvider tenantProvider)
    {
        _mediator = mediator;
        _currentUser = currentUser;
        _tenantProvider = tenantProvider;
    }

    private async Task<int?> ResolveTenantIdAsync(CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId() ?? _currentUser.TenantId;
        if (tenantId.HasValue) return tenantId.Value;

        var tenantsResult = await _mediator.Send(new GetTenantsListQuery(), cancellationToken);
        if (tenantsResult.Succeeded && tenantsResult.Data != null)
        {
            var firstActive = tenantsResult.Data.FirstOrDefault(t => t.IsActive && !t.IsDelete);
            return firstActive?.Id;
        }

        return null;
    }

    [HttpGet]
    [HasPermission(AppPermissions.Transactions.View)]
    public async Task<IActionResult> Index(TransactionStatus? status, int page = 1, CancellationToken cancellationToken = default)
    {
        var tenantId = await ResolveTenantIdAsync(cancellationToken);

        var result = await _mediator.Send(new GetTransactionsListQuery(
            TenantId: tenantId,
            Status: status,
            PageNumber: page,
            PageSize: 20
        ), cancellationToken);

        return View(result.Value);
    }
}