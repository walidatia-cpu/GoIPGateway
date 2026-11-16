using GoIPGateway.Application.Common.Interfaces;
using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.DTOs.Identity;
using MediatR;

namespace GoIPGateway.Application.Features.Roles.Queries;

public record GetRolesListQuery(int? TenantId = null, bool IsSystemAdmin = true) : IRequest<Result<List<RoleDto>>>;

public class GetRolesListQueryHandler : IRequestHandler<GetRolesListQuery, Result<List<RoleDto>>>
{
    private readonly IIdentityService _identityService;

    public GetRolesListQueryHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<Result<List<RoleDto>>> Handle(GetRolesListQuery request, CancellationToken cancellationToken)
    {
        return await _identityService.GetRolesAsync(request.TenantId, request.IsSystemAdmin, cancellationToken);
    }
}

public record GetRoleByIdQuery(string Id, int? TenantId = null) : IRequest<Result<RoleDetailDto>>;

public class GetRoleByIdQueryHandler : IRequestHandler<GetRoleByIdQuery, Result<RoleDetailDto>>
{
    private readonly IIdentityService _identityService;

    public GetRoleByIdQueryHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<Result<RoleDetailDto>> Handle(GetRoleByIdQuery request, CancellationToken cancellationToken)
    {
        return await _identityService.GetRoleByIdAsync(request.Id, request.TenantId, cancellationToken);
    }
}

public record GetRoleCreateFormMetadataQuery(int? TenantId = null, bool IsSystemAdmin = true) : IRequest<Result<RoleFormMetadataDto>>;

public class GetRoleCreateFormMetadataQueryHandler : IRequestHandler<GetRoleCreateFormMetadataQuery, Result<RoleFormMetadataDto>>
{
    private readonly IIdentityService _identityService;

    public GetRoleCreateFormMetadataQueryHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<Result<RoleFormMetadataDto>> Handle(GetRoleCreateFormMetadataQuery request, CancellationToken cancellationToken)
    {
        return await _identityService.GetRoleCreateFormMetadataAsync(request.TenantId, request.IsSystemAdmin, cancellationToken);
    }
}

public record GetRoleEditFormDataQuery(string Id, int? TenantId = null, bool IsSystemAdmin = true) : IRequest<Result<RoleEditFormDataDto>>;

public class GetRoleEditFormDataQueryHandler : IRequestHandler<GetRoleEditFormDataQuery, Result<RoleEditFormDataDto>>
{
    private readonly IIdentityService _identityService;

    public GetRoleEditFormDataQueryHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<Result<RoleEditFormDataDto>> Handle(GetRoleEditFormDataQuery request, CancellationToken cancellationToken)
    {
        return await _identityService.GetRoleEditFormDataAsync(request.Id, request.TenantId, request.IsSystemAdmin, cancellationToken);
    }
}
