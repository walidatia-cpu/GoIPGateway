using FluentValidation;
using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.Services.ServiceDefinitions;
using GoIPGateway.Domain.Entities;
using MediatR;

namespace GoIPGateway.Application.Features.ServiceDefinitions.Commands;

public record UpdateServiceDefinitionCommand(int Id, ServiceDefinition Service) : IRequest<Result<bool>>;

public class UpdateServiceDefinitionCommandValidator : AbstractValidator<UpdateServiceDefinitionCommand>
{
    public UpdateServiceDefinitionCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Service.ServiceCode).NotEmpty().WithMessage("كود الخدمة مطلوب").MaximumLength(50);
        RuleFor(x => x.Service.NameAr).NotEmpty().WithMessage("اسم الخدمة بالعربية مطلوب").MaximumLength(200);
    }
}

public class UpdateServiceDefinitionCommandHandler : IRequestHandler<UpdateServiceDefinitionCommand, Result<bool>>
{
    private readonly IServiceDefinitionCommandService _commandService;

    public UpdateServiceDefinitionCommandHandler(IServiceDefinitionCommandService commandService)
    {
        _commandService = commandService;
    }

    public async Task<Result<bool>> Handle(UpdateServiceDefinitionCommand request, CancellationToken cancellationToken)
    {
        return await _commandService.UpdateServiceAsync(request.Id, request.Service, cancellationToken);
    }
}
