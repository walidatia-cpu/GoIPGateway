using GoIPGateway.Application.Common.Interfaces;
using GoIPGateway.Application.DTOs;
using GoIPGateway.Application.Features.Devices.Commands;
using GoIPGateway.Application.Features.SimCards.Commands;
using GoIPGateway.Application.Features.Tenants.Commands;
using GoIPGateway.Application.Features.Transactions.Commands;
using GoIPGateway.Application.Features.Transactions.Queries;
using GoIPGateway.Domain.Entities;
using GoIPGateway.Domain.Enums;
using GoIPGateway.Web.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace GoIPGateway.Web.Areas.Admin.Controllers;
[Area("Admin")]
[Authorize(Roles = "SuperAdmin")]
public class TransactionsController : Controller
{
    private readonly IMediator _mediator;

    public TransactionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<IActionResult> Index(int? simCardId, TransactionStatus? status, int page = 1)
    {
        var result = await _mediator.Send(new GetTransactionsListQuery(
            SimCardId: simCardId,
            Status: status,
            PageNumber: page,
            PageSize: 20
        ));

        return View(result.Value);
    }

    public async Task<IActionResult> Details(string refCode)
    {
        var result = await _mediator.Send(new GetTransactionByRefQuery(refCode));
        if (result.IsFailure) return NotFound();
        return PartialView("_TransactionDetailsModal", result.Value);
    }
}