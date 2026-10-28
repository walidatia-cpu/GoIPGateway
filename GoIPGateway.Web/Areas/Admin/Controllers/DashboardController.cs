using GoIPGateway.Application.Features.Dashboard.Queries;
using GoIPGateway.Web.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoIPGateway.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "SuperAdmin")]
public class DashboardController : Controller
{
    private readonly IMediator _mediator;

    public DashboardController(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetAdminDashboardQuery(), cancellationToken);
        if (!result.Succeeded || result.Data == null)
        {
            return View(new AdminDashboardViewModel());
        }

        return View(AdminDashboardViewModel.FromDto(result.Data));
    }
}