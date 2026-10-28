using FluentValidation;
using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.Services.ServiceDefinitions;
using GoIPGateway.Domain.Entities;
using MediatR;

namespace GoIPGateway.Application.Features.ServiceDefinitions.Commands;

public record CreateServiceDefinitionCommand(ServiceDefinition Service) : IRequest<Result<int>>;

public class CreateServiceDefinitionCommandValidator : AbstractValidator<CreateServiceDefinitionCommand>
{
    public CreateServiceDefinitionCommandValidator()
    {
        RuleFor(x => x.Service.ServiceCode).NotEmpty().WithMessage("كود الخدمة مطلوب").MaximumLength(50);
        RuleFor(x => x.Service.NameAr).NotEmpty().WithMessage("اسم الخدمة بالعربية مطلوب").MaximumLength(200);
    }
}

public class CreateServiceDefinitionCommandHandler : IRequestHandler<CreateServiceDefinitionCommand, Result<int>>
{
    private readonly IServiceDefinitionCommandService _commandService;

    public CreateServiceDefinitionCommandHandler(IServiceDefinitionCommandService commandService)
    {
        _commandService = commandService;
    }

    public async Task<Result<int>> Handle(CreateServiceDefinitionCommand request, CancellationToken cancellationToken)
    {
        return await _commandService.CreateServiceAsync(request.Service, cancellationToken);
    }
}
