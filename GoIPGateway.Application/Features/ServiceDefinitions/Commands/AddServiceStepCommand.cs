using FluentValidation;
using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.Services.ServiceDefinitions;
using GoIPGateway.Domain.Entities;
using MediatR;

namespace GoIPGateway.Application.Features.ServiceDefinitions.Commands;

public record AddServiceStepCommand(int ServiceId, ServiceUssdStep Step) : IRequest<Result<int>>;

public class AddServiceStepCommandValidator : AbstractValidator<AddServiceStepCommand>
{
    public AddServiceStepCommandValidator()
    {
        RuleFor(x => x.ServiceId).GreaterThan(0);
        RuleFor(x => x.Step.UssdCommand).NotEmpty().WithMessage("أمر الـ USSD مطلوب");
    }
}

public class AddServiceStepCommandHandler : IRequestHandler<AddServiceStepCommand, Result<int>>
{
    private readonly IServiceDefinitionCommandService _commandService;

    public AddServiceStepCommandHandler(IServiceDefinitionCommandService commandService)
    {
        _commandService = commandService;
    }

    public async Task<Result<int>> Handle(AddServiceStepCommand request, CancellationToken cancellationToken)
    {
        return await _commandService.AddStepAsync(request.ServiceId, request.Step, cancellationToken);
    }
}
