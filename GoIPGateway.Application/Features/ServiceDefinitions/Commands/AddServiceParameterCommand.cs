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
        RuleFor(x => x.Parameter.ServiceDefinitionId).GreaterThan(0);
        RuleFor(x => x.Parameter.ParameterName).NotEmpty().WithMessage("اسم البارامتر مطلوب");
        RuleFor(x => x.Parameter.ParameterKey).NotEmpty().WithMessage("مفتاح البارامتر مطلوب");
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
        return await _commandService.AddParameterAsync(request.Parameter, cancellationToken);
    }
}
