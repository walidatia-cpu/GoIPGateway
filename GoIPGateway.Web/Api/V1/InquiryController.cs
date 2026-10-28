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