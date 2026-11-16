using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.DTOs.Identity;

namespace GoIPGateway.Application.Common.Interfaces;

public interface IIdentityService
{
    // Users
    Task<Result<List<UserDto>>> GetUsersAsync(int? tenantId, string? role, CancellationToken cancellationToken = default);
    Task<Result<UserDetailDto>> GetUserByIdAsync(string id, int? tenantId = null, CancellationToken cancellationToken = default);
    Task<Result<UserCreateFormMetadataDto>> GetUserCreateFormMetadataAsync(int? tenantId = null, CancellationToken cancellationToken = default);
    Task<Result<UserEditFormDataDto>> GetUserEditFormDataAsync(string id, int? tenantId = null, CancellationToken cancellationToken = default);
    Task<Result<CreateUserResultDto>> CreateUserAsync(CreateUserDto dto, CancellationToken cancellationToken = default);
    Task<Result<bool>> UpdateUserAsync(UpdateUserDto dto, int? tenantId = null, CancellationToken cancellationToken = default);
    Task<Result<SetTempPasswordResultDto>> SetTempPasswordAsync(string userId, string? customPassword, int? tenantId = null, CancellationToken cancellationToken = default);
    Task<Result<SetTempPasswordResultDto>> SetTenantAdminTempPasswordAsync(int tenantId, string? customPassword, CancellationToken cancellationToken = default);
    Task<Result<bool>> ToggleUserStatusAsync(string userId, int? tenantId = null, CancellationToken cancellationToken = default);
    Task<Result<UserPermissionsDto>> GetUserPermissionsAsync(string userId, CancellationToken cancellationToken = default);
    Task<Result<UserPermissionsFormDataDto>> GetUserPermissionsFormDataAsync(string userId, CancellationToken cancellationToken = default);
    Task<Result<bool>> UpdateUserPermissionsAsync(string userId, List<string> permissions, CancellationToken cancellationToken = default);
    Task<Result<bool>> ChangeRequiredPasswordAsync(string userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default);
    Task<Result<bool>> ValidateTenantAccessAsync(int tenantId, CancellationToken cancellationToken = default);

    // Roles
    Task<Result<List<RoleDto>>> GetRolesAsync(int? tenantId, bool isSystemAdmin, CancellationToken cancellationToken = default);
    Task<Result<RoleDetailDto>> GetRoleByIdAsync(string id, int? tenantId = null, CancellationToken cancellationToken = default);
    Task<Result<RoleFormMetadataDto>> GetRoleCreateFormMetadataAsync(int? tenantId = null, bool isSystemAdmin = true, CancellationToken cancellationToken = default);
    Task<Result<RoleEditFormDataDto>> GetRoleEditFormDataAsync(string id, int? tenantId = null, bool isSystemAdmin = true, CancellationToken cancellationToken = default);
    Task<Result<string>> CreateRoleAsync(CreateRoleDto dto, CancellationToken cancellationToken = default);
    Task<Result<bool>> UpdateRoleAsync(UpdateRoleDto dto, int? tenantId = null, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteRoleAsync(string id, int? tenantId = null, CancellationToken cancellationToken = default);
    Task<Result<string>> EnsureTenantDefaultRoleAsync(int tenantId, string companyName, CancellationToken cancellationToken = default);
    Task<Result<bool>> SyncTenantRoleNameAsync(int tenantId, string newCompanyName, CancellationToken cancellationToken = default);
}
