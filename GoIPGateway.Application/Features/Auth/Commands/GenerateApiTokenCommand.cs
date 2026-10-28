using FluentValidation;
using GoIPGateway.Application.Common.Interfaces;
using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.Services.Tenants;
using MediatR;

namespace GoIPGateway.Application.Features.Auth.Commands;

public record GenerateApiTokenCommand(string ApiKey, string SecretKey) : IRequest<Result<string>>;

public class GenerateApiTokenCommandValidator : AbstractValidator<GenerateApiTokenCommand>
{
    public GenerateApiTokenCommandValidator()
    {
        RuleFor(x => x.ApiKey).NotEmpty().WithMessage("ApiKey is required.");
        RuleFor(x => x.SecretKey).NotEmpty().WithMessage("SecretKey is required.");
    }
}

public class GenerateApiTokenCommandHandler : IRequestHandler<GenerateApiTokenCommand, Result<string>>
{
    private readonly ITenantQueryService _tenantQueryService;
    private readonly IJwtTokenService _jwtService;

    public GenerateApiTokenCommandHandler(ITenantQueryService tenantQueryService, IJwtTokenService jwtService)
    {
        _tenantQueryService = tenantQueryService;
        _jwtService = jwtService;
    }

    public async Task<Result<string>> Handle(GenerateApiTokenCommand request, CancellationToken cancellationToken)
    {
        var tenantResult = await _tenantQueryService.ValidateApiKeyAsync(request.ApiKey, request.SecretKey, cancellationToken);
        if (tenantResult.IsFailure)
        {
            return Result.Failure<string>(tenantResult.Error);
        }

        var tenant = tenantResult.Value;
        var token = _jwtService.GenerateToken(tenant.Id.ToString(), tenant.CompanyName, "TenantApi", tenant.Id, null);
        return Result.Success(token);
    }
}
