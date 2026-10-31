using FluentValidation;
using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.Services.ServiceDefinitions;
using GoIPGateway.Domain.Common;
using GoIPGateway.Domain.Entities;
using MediatR;

namespace GoIPGateway.Application.Features.ServiceDefinitions.Commands;

public record AddServiceParameterCommand(ServiceParameter Parameter) : IRequest<Result<int>>;

public class AddServiceParameterCommandValidator : AbstractValidator<AddServiceParameterCommand>
{
    public AddServiceParameterCommandValidator()
    {
        RuleFor(x => x.Parameter)
            .NotNull()
            .WithMessage("بيانات البارامتر مطلوبة.");

        When(x => x.Parameter != null, () =>
        {
            RuleFor(x => x.Parameter.ServiceDefinitionId)
                .GreaterThan(0)
                .WithMessage("معرف الخدمة غير صالح.");

            RuleFor(x => x.Parameter.ParameterKey)
                .NotEmpty()
                .WithMessage("مفتاح البارامتر مطلوب.")
                .Must(key => !StandardServiceParameters.IsReservedInternalKey(key))
                .WithMessage("لا يمكن إضافة مفاتيح النظام الداخلية (مثل: Password, Pin, MerchantCode) كبارامتر خارجي؛ هذه القيم يستبدلها النظام تلقائياً من إعدادات الشريحة أو الجهاز.")
                .Must(key => StandardServiceParameters.IsAllowedStandardParameter(key))
                .WithMessage("نوع البارامتر غير مدعوم. مسموح فقط باختيار الأنواع الثابتة المعيارية المعتمدة (المبلغ، رقم الهاتف، الرقم القومي، رقم الحساب، رقم الفاتورة، كود OTP).");
        });
    }
}

public class AddServiceParameterCommandHandler : IRequestHandler<AddServiceParameterCommand, Result<int>>
{
    private readonly IServiceDefinitionCommandService _commandService;

    public AddServiceParameterCommandHandler(IServiceDefinitionCommandService commandService)
    {
        _commandService = commandService;
    }

    public async Task<Result<int>> Handle(AddServiceParameterCommand request, CancellationToken cancellationToken)
    {
        var param = request.Parameter;
        if (param != null)
        {
            param.ParameterKey = StandardServiceParameters.CleanKey(param.ParameterKey ?? string.Empty);

            var standard = StandardServiceParameters.FindByKey(param.ParameterKey);
            if (standard != null)
            {
                if (string.IsNullOrWhiteSpace(param.ParameterName))
                    param.ParameterName = standard.Name;

                if (string.IsNullOrWhiteSpace(param.DisplayNameAr))
                    param.DisplayNameAr = standard.DisplayNameAr;

                if (string.IsNullOrWhiteSpace(param.DisplayNameEn))
                    param.DisplayNameEn = standard.DisplayNameEn;

                // Sync data type with standard definition
                param.DataType = standard.DataType;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(param.ParameterName))
                {
                    param.ParameterName = !string.IsNullOrWhiteSpace(param.ParameterKey)
                        ? (char.ToUpper(param.ParameterKey[0]) + (param.ParameterKey.Length > 1 ? param.ParameterKey[1..] : ""))
                        : "Param";
                }
                if (string.IsNullOrWhiteSpace(param.DisplayNameAr))
                {
                    param.DisplayNameAr = param.ParameterName;
                }
            }
        }

        return await _commandService.AddParameterAsync(request.Parameter, cancellationToken);
    }
}
