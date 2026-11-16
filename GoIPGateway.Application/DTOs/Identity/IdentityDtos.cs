namespace GoIPGateway.Application.DTOs.Identity;

public record TenantLookupDto(
    int Id,
    string CompanyName,
    string? ContactPerson = null
);

public record PermissionItemDto(
    string Code,
    string NameAr,
    string DescriptionAr,
    bool IsGranted = false,
    bool IsInherited = false,
    bool IsTenantAllowed = true
);

public record PermissionGroupDto(
    string ModuleName,
    string ModuleKey,
    List<PermissionItemDto> Permissions
);

public record UserDto(
    string Id,
    string FullName,
    string Email,
    string? PhoneNumber,
    int? TenantId,
    string? TenantName,
    List<string> Roles,
    int DirectPermissionsCount,
    bool IsActive,
    bool MustChangePassword,
    DateTime CreatedDate,
    DateTime? LastLoginDate,
    DateTime? LastPasswordChangedDate
);

public record UserDetailDto(
    string Id,
    string FullName,
    string Email,
    string? PhoneNumber,
    int? TenantId,
    string? TenantName,
    List<string> Roles,
    bool IsActive,
    bool MustChangePassword
);

public record UserCreateFormMetadataDto(
    List<TenantLookupDto> Tenants,
    List<RoleDto> AvailableRoles,
    string DefaultTempPassword,
    int? TenantId = null
);

public record UserEditFormDataDto(
    string Id,
    string FullName,
    string Email,
    string? PhoneNumber,
    int? TenantId,
    string? TenantName,
    bool IsActive,
    bool MustChangePassword,
    List<string> SelectedRoles,
    List<TenantLookupDto> Tenants,
    List<RoleDto> AvailableRoles
);

public record CreateUserDto(
    string FullName,
    string Email,
    string? PhoneNumber,
    int? TenantId,
    List<string> SelectedRoles,
    string? TempPassword,
    bool AutoGeneratePassword = true,
    bool MustChangePassword = true,
    bool IsActive = true
);

public record CreateUserResultDto(
    string UserId,
    string Email,
    string FullName,
    string GeneratedPassword
);

public record UpdateUserDto(
    string Id,
    string FullName,
    string Email,
    string? PhoneNumber,
    int? TenantId,
    List<string> SelectedRoles,
    bool IsActive,
    bool MustChangePassword
);

public record SetTempPasswordResultDto(
    string UserId,
    string FullName,
    string Email,
    string TemporaryPassword,
    string? CompanyName = null
);

public record UserPermissionsDto(
    string UserId,
    string FullName,
    string Email,
    int? TenantId,
    string? TenantName,
    List<string> UserRoles,
    List<string> DirectPermissions,
    List<string> InheritedRolePermissions
);

public record UserPermissionsFormDataDto(
    string UserId,
    string FullName,
    string Email,
    int? TenantId,
    string? TenantName,
    List<string> UserRoles,
    List<string> DirectPermissions,
    List<string> InheritedRolePermissions,
    List<PermissionGroupDto> PermissionGroups
);

public record RoleDto(
    string Id,
    string Name,
    string? Description,
    int? TenantId,
    string? TenantName,
    bool IsSystemRole,
    int UsersCount,
    int PermissionsCount
);

public record RoleDetailDto(
    string Id,
    string Name,
    string? Description,
    int? TenantId,
    string? TenantName,
    bool IsSystemRole,
    List<string> SelectedPermissions
);

public record RoleFormMetadataDto(
    List<TenantLookupDto> Tenants,
    List<PermissionGroupDto> PermissionGroups,
    int? TenantId = null
);

public record RoleEditFormDataDto(
    string Id,
    string Name,
    string? Description,
    int? TenantId,
    string? TenantName,
    bool IsSystemRole,
    List<string> SelectedPermissions,
    List<TenantLookupDto> Tenants,
    List<PermissionGroupDto> PermissionGroups
);

public record CreateRoleDto(
    string Name,
    string? Description,
    int? TenantId,
    List<string> SelectedPermissions
);

public record UpdateRoleDto(
    string Id,
    string Name,
    string? Description,
    int? TenantId,
    List<string> SelectedPermissions
);
