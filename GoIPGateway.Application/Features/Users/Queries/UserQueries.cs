using GoIPGateway.Application.Common.Interfaces;
using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.DTOs.Identity;
using MediatR;

namespace GoIPGateway.Application.Features.Users.Queries;

public record GetUsersListQuery(int? TenantId = null, string? Role = null) : IRequest<Result<List<UserDto>>>;

public class GetUsersListQueryHandler : IRequestHandler<GetUsersListQuery, Result<List<UserDto>>>
{
    private readonly IIdentityService _identityService;

    public GetUsersListQueryHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<Result<List<UserDto>>> Handle(GetUsersListQuery request, CancellationToken cancellationToken)
    {
        return await _identityService.GetUsersAsync(request.TenantId, request.Role, cancellationToken);
    }
}

public record GetUserByIdQuery(string Id, int? TenantId = null) : IRequest<Result<UserDetailDto>>;

public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, Result<UserDetailDto>>
{
    private readonly IIdentityService _identityService;

    public GetUserByIdQueryHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<Result<UserDetailDto>> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        return await _identityService.GetUserByIdAsync(request.Id, request.TenantId, cancellationToken);
    }
}

public record GetUserCreateFormMetadataQuery(int? TenantId = null) : IRequest<Result<UserCreateFormMetadataDto>>;

public class GetUserCreateFormMetadataQueryHandler : IRequestHandler<GetUserCreateFormMetadataQuery, Result<UserCreateFormMetadataDto>>
{
    private readonly IIdentityService _identityService;

    public GetUserCreateFormMetadataQueryHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<Result<UserCreateFormMetadataDto>> Handle(GetUserCreateFormMetadataQuery request, CancellationToken cancellationToken)
    {
        return await _identityService.GetUserCreateFormMetadataAsync(request.TenantId, cancellationToken);
    }
}

public record GetUserEditFormDataQuery(string Id, int? TenantId = null) : IRequest<Result<UserEditFormDataDto>>;

public class GetUserEditFormDataQueryHandler : IRequestHandler<GetUserEditFormDataQuery, Result<UserEditFormDataDto>>
{
    private readonly IIdentityService _identityService;

    public GetUserEditFormDataQueryHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<Result<UserEditFormDataDto>> Handle(GetUserEditFormDataQuery request, CancellationToken cancellationToken)
    {
        return await _identityService.GetUserEditFormDataAsync(request.Id, request.TenantId, cancellationToken);
    }
}

public record GetUserPermissionsQuery(string UserId) : IRequest<Result<UserPermissionsDto>>;

public class GetUserPermissionsQueryHandler : IRequestHandler<GetUserPermissionsQuery, Result<UserPermissionsDto>>
{
    private readonly IIdentityService _identityService;

    public GetUserPermissionsQueryHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<Result<UserPermissionsDto>> Handle(GetUserPermissionsQuery request, CancellationToken cancellationToken)
    {
        return await _identityService.GetUserPermissionsAsync(request.UserId, cancellationToken);
    }
}

public record GetUserPermissionsFormDataQuery(string UserId) : IRequest<Result<UserPermissionsFormDataDto>>;

public class GetUserPermissionsFormDataQueryHandler : IRequestHandler<GetUserPermissionsFormDataQuery, Result<UserPermissionsFormDataDto>>
{
    private readonly IIdentityService _identityService;

    public GetUserPermissionsFormDataQueryHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<Result<UserPermissionsFormDataDto>> Handle(GetUserPermissionsFormDataQuery request, CancellationToken cancellationToken)
    {
        return await _identityService.GetUserPermissionsFormDataAsync(request.UserId, cancellationToken);
    }
}
