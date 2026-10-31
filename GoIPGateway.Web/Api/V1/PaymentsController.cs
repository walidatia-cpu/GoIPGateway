using GoIPGateway.Application.DTOs;
using GoIPGateway.Application.Features.Transactions.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoIPGateway.Web.Api.V1;

/// <summary>
/// بوابة الدفع والتحويل الموحدة عبر أجهزة GoIP
/// قناة دفع واحدة تعتمد على إرسال كود الخدمة (ServiceCode) والمبلغ
/// </summary>
[ApiController]
[Route("api/v1/payments")]
[Authorize]
[Produces("application/json")]
public class PaymentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PaymentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// قناة الدفع الموحدة: تنفيذ عملية دفع أو تحويل بإرسال كود الخدمة
    /// يتم التحقق من صحة المدخلات استباقياً عبر FluentValidation و MediatR Pipeline
    /// </summary>
    /// <param name="command">أمر الدفع متضمناً كود الخدمة (مثل TRANSFER أو QR_PAY) والمبلغ والبارامترات</param>
    /// <response code="200">تم تنفيذ العملية واكتمالها بنجاح</response>
    /// <response code="400">فشل التحقق الاستباقي أو رفض العملية</response>
    /// <response code="401">غير مصرح</response>
    [HttpPost]
    [ProducesResponseType(typeof(TransactionResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ProcessPayment([FromBody] ProcessPaymentCommand command)
    {
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
