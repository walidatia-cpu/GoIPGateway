using FluentValidation;
using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.Services.ServiceDefinitions;
using GoIPGateway.Domain.Entities;
using MediatR;

namespace GoIPGateway.Application.Features.ServiceDefinitions.Commands;

public record AddServiceParameterCommand(ServiceParameter Parameter) : IRequest<Result<int>>;

public class AddServiceParameterCommandValidator : AbstractValidator<AddServiceParameterCommand>
{
    public AddServiceParameterCommandValidator()
    {
        RuleFor(x => x.Parameter.ServiceDefinitionId)
            .GreaterThan(0)
            .WithMessage("معرف الخدمة غير صالح.");

        RuleFor(x => x.Parameter.ParameterKey)
            .NotEmpty()
            .WithMessage("مفتاح البارامتر مطلوب (مثل: amount أو merchantCode).");
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
            param.ParameterKey = (param.ParameterKey ?? string.Empty).Trim().Trim('{', '}').Trim();
            if (string.IsNullOrWhiteSpace(param.ParameterName))
            {
                param.ParameterName = !string.IsNullOrWhiteSpace(param.ParameterKey)
                    ? (char.ToUpper(param.ParameterKey[0]) + (param.ParameterKey.Length > 1 ? param.ParameterKey[1..] : ""))
                    : (!string.IsNullOrWhiteSpace(param.DisplayNameAr) ? param.DisplayNameAr : "Param");
            }
            if (string.IsNullOrWhiteSpace(param.DisplayNameAr))
            {
                param.DisplayNameAr = param.ParameterName;
            }
        }

        return await _commandService.AddParameterAsync(request.Parameter, cancellationToken);
    }
}
