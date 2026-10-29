using System.Text;
using GoIPGateway.Application.Common.Interfaces;
using GoIPGateway.Application.DTOs.Reports;
using GoIPGateway.Application.Features.Reports.Queries;
using GoIPGateway.Application.Features.Tenants.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoIPGateway.Web.Areas.Company.Controllers;

[Area("Company")]
[Authorize(Roles = "TenantAdmin,TenantOperator,TenantViewer,SuperAdmin")]
public class ReportsController : Controller
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantProvider _tenantProvider;

    public ReportsController(IMediator mediator, ICurrentUserService currentUser, ITenantProvider tenantProvider)
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
    public async Task<IActionResult> Index(CompanyReportFilterDto filter, CancellationToken cancellationToken)
    {
        var tenantId = await ResolveTenantIdAsync(cancellationToken);
        if (!tenantId.HasValue) return Forbid();

        // Enforce Company Scope
        filter.TenantId = tenantId.Value;

        var result = await _mediator.Send(new GetCompanyReportQuery(filter), cancellationToken);
        if (!result.Succeeded || result.Data == null)
        {
            return View(new CompanyReportDto { Filter = filter });
        }

        return View(result.Data);
    }

    [HttpGet]
    public async Task<IActionResult> ExportCsv(CompanyReportFilterDto filter, CancellationToken cancellationToken)
    {
        var tenantId = await ResolveTenantIdAsync(cancellationToken);
        if (!tenantId.HasValue) return Forbid();

        filter.TenantId = tenantId.Value;
        filter.PageSize = 10000;
        filter.PageNumber = 1;

        var result = await _mediator.Send(new GetCompanyReportQuery(filter), cancellationToken);
        var report = result.Data;

        var sb = new StringBuilder();
        sb.Append('\uFEFF');

        sb.AppendLine("رقم المعاملة,الجهاز,المنفذ,الشريحة,الشبكة,الخدمة,المبلغ (ج.م),التكلفة (ج.م),الحالة,تاريخ الطلب,الاستجابة / الخطأ");

        if (report != null && report.Transactions.Items.Any())
        {
            foreach (var tx in report.Transactions.Items)
            {
                var deviceName = tx.DevicePort?.Device?.DeviceName ?? "N/A";
                var portNumber = tx.DevicePort?.PortNumber.ToString() ?? "-";
                var simNumber = tx.SimCard?.PhoneNumber ?? tx.SimCard?.SimNumber ?? "N/A";
                var op = tx.ServiceDefinition?.Operator?.ToString() ?? tx.SimCard?.Operator.ToString() ?? "N/A";
                var serviceName = tx.ServiceDefinition?.NameAr ?? "N/A";
                var amount = tx.Amount?.ToString("0.00") ?? "0.00";
                var cost = tx.TenantCharge.ToString("0.00");
                var status = tx.Status switch
                {
                    Domain.Enums.TransactionStatus.Completed => "ناجحة",
                    Domain.Enums.TransactionStatus.Failed => "فاشلة",
                    Domain.Enums.TransactionStatus.Processing => "قيد المعالجة",
                    Domain.Enums.TransactionStatus.Pending => "معلقة",
                    _ => tx.Status.ToString()
                };
                var date = tx.RequestedAt.ToString("yyyy-MM-dd HH:mm:ss");
                var response = (tx.ErrorMessage ?? tx.RawResponse ?? "").Replace(",", " ").Replace("\n", " ").Replace("\r", " ");

                sb.AppendLine($"\"{tx.TransactionRef}\",\"{deviceName}\",\"{portNumber}\",\"{simNumber}\",\"{op}\",\"{serviceName}\",\"{amount}\",\"{cost}\",\"{status}\",\"{date}\",\"{response}\"");
            }
        }

        var fileName = $"تقرير_معاملات_الشركة_{DateTime.UtcNow:yyyyMMdd_HHmm}.csv";
        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv; charset=utf-8", fileName);
    }

    [HttpGet]
    public async Task<IActionResult> Summary(ExecutiveSummaryFilterDto filter, CancellationToken cancellationToken)
    {
        var tenantId = await ResolveTenantIdAsync(cancellationToken);
        if (!tenantId.HasValue) return Forbid();

        filter.TenantId = tenantId.Value;

        var result = await _mediator.Send(new GetExecutiveSummaryReportQuery(filter), cancellationToken);
        if (!result.Succeeded || result.Data == null)
        {
            return View(new ExecutiveSummaryDto { Filter = filter, IsAdminScope = false });
        }

        return View(result.Data);
    }

    [HttpGet]
    public async Task<IActionResult> ExportSummaryCsv(ExecutiveSummaryFilterDto filter, CancellationToken cancellationToken)
    {
        var tenantId = await ResolveTenantIdAsync(cancellationToken);
        if (!tenantId.HasValue) return Forbid();

        filter.TenantId = tenantId.Value;

        var result = await _mediator.Send(new GetExecutiveSummaryReportQuery(filter), cancellationToken);
        var report = result.Data;

        var sb = new StringBuilder();
        sb.Append('\uFEFF');

        sb.AppendLine("تقرير الإجماليات والتسويات المالية للشركة");
        sb.AppendLine($"اسم الشركة / التاجر: {report?.ScopeTitle}");
        sb.AppendLine($"الفترة المحاسبية: من {report?.Filter.FromDate:yyyy-MM-dd HH:mm} إلى {report?.Filter.ToDate:yyyy-MM-dd HH:mm}");
        sb.AppendLine();

        sb.AppendLine("المؤشر المالي,القيمة");
        sb.AppendLine($"إجمالي المبالغ المنفذة (ج.م),{report?.TotalGrossVolume:0.00}");
        sb.AppendLine($"حجم العمليات الناجحة (ج.م),{report?.TotalSuccessfulVolume:0.00}");
        sb.AppendLine($"حجم العمليات الفاشلة (ج.م),{report?.TotalFailedVolume:0.00}");
        sb.AppendLine($"إجمالي تكلفة الخدمات المستحقة (ج.م),{report?.TotalPlatformFees:0.00}");
        sb.AppendLine($"صافي المبالغ المستقرة (ج.م),{report?.NetSettledVolume:0.00}");
        sb.AppendLine($"إجمالي سيولة محافظ الكاش الحالية (ج.م),{report?.TotalWalletsLiquidity:0.00}");
        sb.AppendLine($"رصيد الحساب المالي (ج.م),{report?.TotalAccountsBalance:0.00}");
        sb.AppendLine($"إجمالي عدد العمليات,{report?.TotalOperationsCount}");
        sb.AppendLine($"العمليات الناجحة,{report?.CompletedCount}");
        sb.AppendLine($"العمليات الفاشلة,{report?.FailedCount}");
        sb.AppendLine($"نسبة النجاح العامة,{report?.SuccessRate}%");
        sb.AppendLine($"متوسط قيمة المعاملة (ج.م),{report?.AverageTransactionSize:0.00}");
        sb.AppendLine();

        sb.AppendLine("توزيع الشبكات والمحافظ");
        sb.AppendLine("الشبكة,عدد العمليات,العمليات الناجحة,نسبة النجاح,حجم المبالغ (ج.م),السيولة النقدية المتاحة (ج.م)");
        if (report != null)
        {
            foreach (var op in report.OperatorAggregates)
            {
                sb.AppendLine($"\"{op.NameAr}\",\"{op.TotalOperations}\",\"{op.SuccessfulOperations}\",\"{op.SuccessRate}%\",\"{op.TotalVolume:0.00}\",\"{op.CurrentLiquidity:0.00}\"");
            }
        }

        var fileName = $"تقرير_إجماليات_الشركة_{DateTime.UtcNow:yyyyMMdd_HHmm}.csv";
        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv; charset=utf-8", fileName);
    }
}
