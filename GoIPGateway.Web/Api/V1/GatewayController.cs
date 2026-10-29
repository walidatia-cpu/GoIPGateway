using GoIPGateway.Application.Common.Interfaces;
using GoIPGateway.Application.DTOs;
using GoIPGateway.Application.Features.ServiceDefinitions.Queries;
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
[Route("api/v1/services")]
[Authorize]
[Produces("application/json")]
public class GatewayController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantProvider _tenantProvider;

    public GatewayController(IMediator mediator, ICurrentUserService currentUser, ITenantProvider tenantProvider)
    {
        _mediator = mediator;
        _currentUser = currentUser;
        _tenantProvider = tenantProvider;
    }

    /// <summary>
    /// استعلام قائمة الخدمات المفعلة للشركة مع تفاصيل الحقول والبارامترات المطلوبة
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetServices(CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId() ?? _currentUser.TenantId;
        if (!tenantId.HasValue)
            return Unauthorized();

        var query = new GetSubscribedServicesQuery(tenantId.Value);
        var result = await _mediator.Send(query, cancellationToken);
        if (!result.Succeeded || result.Data == null)
            return BadRequest(new { success = false, message = result.Error?.Message ?? "تعذر جلب الخدمات" });

        var dtos = result.Data.Select(s => new
        {
            s.Id,
            s.ServiceCode,
            nameAr = s.NameAr,
            nameEn = s.NameEn,
            category = s.Category.ToString(),
            operatorType = s.Operator?.ToString(),
            baseCost = s.BaseCost,
            parameters = s.Parameters.Where(p => p.IsActive).OrderBy(p => p.SortOrder).Select(p => new
            {
                p.ParameterKey,
                displayNameAr = p.DisplayNameAr ?? p.ParameterName,
                displayNameEn = p.DisplayNameEn,
                dataType = p.DataType.ToString(),
                p.IsRequired,
                p.DefaultValue
            })
        });

        return Ok(dtos);
    }

    /// <summary>
    /// تنفيذ خدمة عبر بوابة GoIP (USSD / QR / Transfer / إلخ)
    /// </summary>
    [HttpPost("execute")]
    public async Task<IActionResult> Execute([FromBody] ExecuteServiceRequestDto request)
    {
        var parameters = request.GetNormalizedParameters();

        var command = new ExecuteServiceCommand(
            request.ServiceCode,
            request.ExternalRef,
            request.SimCardId,
            request.Operator,
            parameters,
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