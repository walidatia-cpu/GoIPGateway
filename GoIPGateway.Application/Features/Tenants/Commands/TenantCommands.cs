using System.Security.Cryptography;
using FluentValidation;
using GoIPGateway.Application.Common.Interfaces;
using GoIPGateway.Application.Common.Models;
using GoIPGateway.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GoIPGateway.Application.Features.Tenants.Commands;

public record CreateTenantCommand(
    string CompanyName,
    string? CompanyNameEn,
    string? ContactPerson,
    string? ContactEmail,
    string? ContactPhone,
    string? Address,
    int MaxDevices = 1,
    int MaxDailyOperations = 1000,
    decimal CreditLimit = 0
) : IRequest<Result<int>>;

public class CreateTenantCommandValidator : AbstractValidator<CreateTenantCommand>
{
    public CreateTenantCommandValidator()
    {
        RuleFor(x => x.CompanyName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ContactEmail).EmailAddress().When(x => !string.IsNullOrEmpty(x.ContactEmail));
        RuleFor(x => x.MaxDevices).GreaterThan(0);
        RuleFor(x => x.MaxDailyOperations).GreaterThan(0);
    }
}

public class CreateTenantCommandHandler : IRequestHandler<CreateTenantCommand, Result<int>>
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public CreateTenantCommandHandler(IAppDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<int>> Handle(CreateTenantCommand request, CancellationToken cancellationToken)
    {
        var tenant = new Tenant
        {
            CompanyName = request.CompanyName,
            CompanyNameEn = request.CompanyNameEn,
            ContactPerson = request.ContactPerson,
            ContactEmail = request.ContactEmail,
            ContactPhone = request.ContactPhone,
            Address = request.Address,
            MaxDevices = request.MaxDevices,
            MaxDailyOperations = request.MaxDailyOperations,
            CreditLimit = request.CreditLimit,
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            CreatedById = _currentUser.UserId
        };

        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync(cancellationToken);

        // Automatically create a default production API Key
        var apiKey = new TenantApiKey
        {
            TenantId = tenant.Id,
            ApiKey = $"gw_live_{Guid.NewGuid():N}",
            SecretKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
            KeyName = "Default Production Key",
            RateLimit = 120,
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };
        _context.TenantApiKeys.Add(apiKey);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(tenant.Id);
    }
}

public record GenerateApiKeyCommand(int TenantId, string KeyName, int RateLimit = 100) : IRequest<Result<ApiKeyDto>>;

public class ApiKeyDto
{
    public int Id { get; set; }
    public string ApiKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string KeyName { get; set; } = string.Empty;
    public int RateLimit { get; set; }
}

public class GenerateApiKeyCommandHandler : IRequestHandler<GenerateApiKeyCommand, Result<ApiKeyDto>>
{
    private readonly IAppDbContext _context;

    public GenerateApiKeyCommandHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<ApiKeyDto>> Handle(GenerateApiKeyCommand request, CancellationToken cancellationToken)
    {
        var tenant = await _context.Tenants.FindAsync(new object[] { request.TenantId }, cancellationToken);
        if (tenant == null)
            return Result.Failure<ApiKeyDto>("Tenant.NotFound", "الشركة غير موجودة.");

        var apiKey = new TenantApiKey
        {
            TenantId = tenant.Id,
            ApiKey = $"gw_{Guid.NewGuid():N}",
            SecretKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
            KeyName = request.KeyName,
            RateLimit = request.RateLimit,
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        _context.TenantApiKeys.Add(apiKey);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(new ApiKeyDto
        {
            Id = apiKey.Id,
            ApiKey = apiKey.ApiKey,
            SecretKey = apiKey.SecretKey,
            KeyName = apiKey.KeyName,
            RateLimit = apiKey.RateLimit
        });
    }
}
