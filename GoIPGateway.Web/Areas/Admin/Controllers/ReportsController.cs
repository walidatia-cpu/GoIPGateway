using System.Text;
using GoIPGateway.Application.DTOs.Reports;
using GoIPGateway.Application.Features.Reports.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoIPGateway.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "SuperAdmin")]
public class ReportsController : Controller
{
    private readonly IMediator _mediator;

    public ReportsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CompanyReportFilterDto filter, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCompanyReportQuery(filter), cancellationToken);
        if (!result.Succeeded || result.Data == null)
        {
            return View("CompanyReport", new CompanyReportDto { Filter = filter });
        }

        return View("CompanyReport", result.Data);
    }

    [HttpGet]
    public async Task<IActionResult> ExportCsv(CompanyReportFilterDto filter, CancellationToken cancellationToken)
    {
        filter.PageSize = 10000; // Export all matching
        filter.PageNumber = 1;
        var result = await _mediator.Send(new GetCompanyReportQuery(filter), cancellationToken);
        var report = result.Data;

        var sb = new StringBuilder();
        // UTF-8 BOM for Arabic support in Excel
        sb.Append('\uFEFF');

        // CSV Header
        sb.AppendLine("رقم المعاملة,الشركة,الجهاز,المنفذ,الشريحة,الشبكة,الخدمة,المبلغ (ج.م),التكلفة (ج.م),الحالة,تاريخ الطلب,الاستجابة / الخطأ");

        if (report != null && report.Transactions.Items.Any())
        {
            foreach (var tx in report.Transactions.Items)
            {
                var companyName = tx.Tenant?.CompanyName ?? "N/A";
                var deviceName = tx.DevicePort?.Device?.DeviceName ?? "N/A";
                var portNumber = tx.DevicePort?.PortNumber.ToString() ?? "-";
                var simNumber = tx.SimCard?.PhoneNumber ?? tx.SimCard?.SimNumber ?? "N/A";
                var op = tx.ServiceDefinition?.Operator?.ToString() ?? tx.SimCard?.Operator.ToString() ?? "N/A";
                var serviceName = tx.ServiceDefinition?.NameAr ?? "N/A";
                var amount = tx.Amount?.ToString("0.00") ?? "0.00";
                var cost = tx.ServiceCost.ToString("0.00");
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

                sb.AppendLine($"\"{tx.TransactionRef}\",\"{companyName}\",\"{deviceName}\",\"{portNumber}\",\"{simNumber}\",\"{op}\",\"{serviceName}\",\"{amount}\",\"{cost}\",\"{status}\",\"{date}\",\"{response}\"");
            }
        }

        var fileName = $"تقرير_شركات_وبوابات_GoIP_{DateTime.UtcNow:yyyyMMdd_HHmm}.csv";
        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv; charset=utf-8", fileName);
    }

    [HttpGet]
    public async Task<IActionResult> Tenants(TenantsPerformanceFilterDto filter, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetTenantsPerformanceReportQuery(filter), cancellationToken);
        if (!result.Succeeded || result.Data == null)
        {
            return View(new TenantsPerformanceReportDto { Filter = filter });
        }

        return View(result.Data);
    }

    [HttpGet]
    public async Task<IActionResult> ExportTenantsCsv(TenantsPerformanceFilterDto filter, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetTenantsPerformanceReportQuery(filter), cancellationToken);
        var report = result.Data;

        var sb = new StringBuilder();
        sb.Append('\uFEFF'); // UTF-8 BOM for Arabic support

        sb.AppendLine("كود الشركة,اسم الشركة,الشخص المسؤول,الهاتف,البريد,حجم المبالغ (ج.م),رسوم المنصة (ج.م),إجمالي العمليات,العمليات الناجحة,العمليات الفاشلة,نسبة النجاح,الأجهزة المخصصة,الشرائح النشطة,الرصيد المالي (ج.م),آخر عملية,أكثر خدمة استخداماً");

        if (report != null && report.Tenants.Any())
        {
            foreach (var t in report.Tenants)
            {
                var id = t.TenantId;
                var name = t.CompanyName.Replace(",", " ");
                var person = (t.ContactPerson ?? "").Replace(",", " ");
                var phone = t.ContactPhone ?? "";
                var email = t.ContactEmail ?? "";
                var volume = t.TotalFinancialVolume.ToString("0.00");
                var fees = t.TotalPlatformFees.ToString("0.00");
                var totalOps = t.TotalOperationsCount;
                var successOps = t.SuccessfulOperationsCount;
                var failOps = t.FailedOperationsCount;
                var rate = t.SuccessRate + "%";
                var devices = t.AssignedDevicesCount;
                var sims = t.ActiveSimsCount;
                var balance = t.CurrentBalance.ToString("0.00");
                var lastOp = t.LastOperationAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "-";
                var topSvc = (t.TopServiceName ?? "-").Replace(",", " ");

                sb.AppendLine($"\"{id}\",\"{name}\",\"{person}\",\"{phone}\",\"{email}\",\"{volume}\",\"{fees}\",\"{totalOps}\",\"{successOps}\",\"{failOps}\",\"{rate}\",\"{devices}\",\"{sims}\",\"{balance}\",\"{lastOp}\",\"{topSvc}\"");
            }
        }

        var fileName = $"تقرير_أداء_وحجم_أعمال_الشركات_{DateTime.UtcNow:yyyyMMdd_HHmm}.csv";
        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv; charset=utf-8", fileName);
    }

    [HttpGet]
    public async Task<IActionResult> Summary(ExecutiveSummaryFilterDto filter, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetExecutiveSummaryReportQuery(filter), cancellationToken);
        if (!result.Succeeded || result.Data == null)
        {
            return View(new ExecutiveSummaryDto { Filter = filter });
        }

        return View(result.Data);
    }

    [HttpGet]
    public async Task<IActionResult> ExportSummaryCsv(ExecutiveSummaryFilterDto filter, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetExecutiveSummaryReportQuery(filter), cancellationToken);
        var report = result.Data;

        var sb = new StringBuilder();
        sb.Append('\uFEFF'); // UTF-8 BOM

        sb.AppendLine("تقرير الإجماليات والتسويات المركزية");
        sb.AppendLine($"نطاق التقرير: {report?.ScopeTitle ?? "المنصة المركزية"}");
        sb.AppendLine($"الفترة المحاسبية: من {report?.Filter.FromDate:yyyy-MM-dd HH:mm} إلى {report?.Filter.ToDate:yyyy-MM-dd HH:mm}");
        sb.AppendLine();

        sb.AppendLine("المؤشر المالي,القيمة");
        sb.AppendLine($"إجمالي حجم المبالغ المنفذة (ج.م),{report?.TotalGrossVolume:0.00}");
        sb.AppendLine($"حجم العمليات الناجحة (ج.م),{report?.TotalSuccessfulVolume:0.00}");
        sb.AppendLine($"حجم العمليات الفاشلة (ج.م),{report?.TotalFailedVolume:0.00}");
        sb.AppendLine($"إجمالي رسوم وأرباح المنصة (ج.م),{report?.TotalPlatformFees:0.00}");
        sb.AppendLine($"صافي التسويات للشركات (ج.م),{report?.NetSettledVolume:0.00}");
        sb.AppendLine($"إجمالي سيولة محافظ الكاش (ج.م),{report?.TotalWalletsLiquidity:0.00}");
        sb.AppendLine($"إجمالي أرصدة الحسابات (ج.م),{report?.TotalAccountsBalance:0.00}");
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

        var fileName = $"تقرير_الإجماليات_والتسويات_{DateTime.UtcNow:yyyyMMdd_HHmm}.csv";
        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv; charset=utf-8", fileName);
    }
}
