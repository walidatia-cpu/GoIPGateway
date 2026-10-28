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