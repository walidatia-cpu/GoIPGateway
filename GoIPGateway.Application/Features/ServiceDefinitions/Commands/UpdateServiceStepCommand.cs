using FluentValidation;
using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.Services.ServiceDefinitions;
using GoIPGateway.Domain.Entities;
using MediatR;

namespace GoIPGateway.Application.Features.ServiceDefinitions.Commands;

public record UpdateServiceStepCommand(int StepId, ServiceUssdStep Step) : IRequest<Result<bool>>;

public class UpdateServiceStepCommandValidator : AbstractValidator<UpdateServiceStepCommand>
{
    public UpdateServiceStepCommandValidator()
    {
        RuleFor(x => x.StepId).GreaterThan(0);
        RuleFor(x => x.Step.UssdCommand).NotEmpty().WithMessage("أمر الـ USSD مطلوب");
    }
}

public class UpdateServiceStepCommandHandler : IRequestHandler<UpdateServiceStepCommand, Result<bool>>
{
    private readonly IServiceDefinitionCommandService _commandService;

    public UpdateServiceStepCommandHandler(IServiceDefinitionCommandService commandService)
    {
        _commandService = commandService;
    }

    public async Task<Result<bool>> Handle(UpdateServiceStepCommand request, CancellationToken cancellationToken)
    {
        return await _commandService.UpdateStepAsync(request.StepId, request.Step, cancellationToken);
    }
}
