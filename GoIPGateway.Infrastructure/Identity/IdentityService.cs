using System.Security.Claims;
using GoIPGateway.Application.Common.Interfaces;
using GoIPGateway.Application.Common.Interfaces.Repositories;
using GoIPGateway.Application.Common.Models;
using GoIPGateway.Application.Common.Security;
using GoIPGateway.Application.DTOs.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GoIPGateway.Infrastructure.Identity;

public class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly IUnitOfWork _unitOfWork;

    public IdentityService(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IUnitOfWork unitOfWork)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<List<UserDto>>> GetUsersAsync(int? tenantId, string? role, CancellationToken cancellationToken = default)
    {
        // The Owner user is completely hidden from all user listings
        var usersQuery = _userManager.Users.AsQueryable().Where(u => !u.IsOwner);

        if (tenantId.HasValue)
        {
            usersQuery = usersQuery.Where(u => u.TenantId == tenantId.Value);
        }

        var users = await usersQuery.OrderByDescending(u => u.CreatedDate).ToListAsync(cancellationToken);
        var tenants = await _unitOfWork.Tenants.AsQueryable().IgnoreQueryFilters().ToDictionaryAsync(t => t.Id, t => t.CompanyName, cancellationToken);

        var list = new List<UserDto>();
        foreach (var user in users)
        {
            var userRoles = await _userManager.GetRolesAsync(user);
            if (!string.IsNullOrEmpty(role) && !userRoles.Contains(role))
                continue;

            var userClaims = await _userManager.GetClaimsAsync(user);
            var directPermCount = userClaims.Count(c => c.Type == AppPermissions.ClaimType);

            list.Add(new UserDto(
                user.Id,
                user.FullName ?? user.UserName ?? "مستخدم",
                user.Email ?? string.Empty,
                user.PhoneNumber,
                user.TenantId,
                user.TenantId.HasValue && tenants.TryGetValue(user.TenantId.Value, out var tName) ? tName : "النظام الرئيسي",
                userRoles.ToList(),
                directPermCount,
                user.IsActive,
                user.MustChangePassword,
                user.CreatedDate,
                user.LastLoginDate,
                user.LastPasswordChangedDate
            ));
        }

        return Result.Success(list);
    }

    public async Task<Result<UserDetailDto>> GetUserByIdAsync(string id, int? tenantId = null, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null || user.IsOwner || (tenantId.HasValue && user.TenantId != tenantId.Value))
            return Result.Failure<UserDetailDto>(Error.NotFound("User.NotFound", "المستخدم غير موجود."));

        var roles = await _userManager.GetRolesAsync(user);

        string? tenantName = null;
        if (user.TenantId.HasValue)
        {
            var tenant = await _unitOfWork.Tenants.GetByIdAsync(user.TenantId.Value);
            tenantName = tenant?.CompanyName;
        }

        var dto = new UserDetailDto(
            user.Id,
            user.FullName ?? string.Empty,
            user.Email ?? string.Empty,
            user.PhoneNumber,
            user.TenantId,
            tenantName,
            roles.ToList(),
            user.IsActive,
            user.MustChangePassword
        );

        return Result.Success(dto);
    }

    public async Task<Result<UserCreateFormMetadataDto>> GetUserCreateFormMetadataAsync(int? tenantId = null, CancellationToken cancellationToken = default)
    {
        var tenants = await _unitOfWork.Tenants.AsQueryable()
            .Where(t => !t.IsDelete)
            .OrderBy(t => t.CompanyName)
            .Select(t => new TenantLookupDto(t.Id, t.CompanyName, t.ContactPerson))
            .ToListAsync(cancellationToken);

        var rolesQuery = _roleManager.Roles.AsQueryable();
        if (tenantId.HasValue)
        {
            rolesQuery = rolesQuery.Where(r => (r.IsSystemRole && r.Name != "SuperAdmin") || r.TenantId == tenantId.Value);
        }

        var roles = await rolesQuery.OrderBy(r => r.Name).ToListAsync(cancellationToken);
        var roleDtos = roles.Select(r => new RoleDto(r.Id, r.Name ?? "", r.Description, r.TenantId, null, r.IsSystemRole, 0, 0)).ToList();

        var defaultPassword = PasswordHelper.GenerateSecureTempPassword(10);

        return Result.Success(new UserCreateFormMetadataDto(tenants, roleDtos, defaultPassword, tenantId));
    }

    public async Task<Result<UserEditFormDataDto>> GetUserEditFormDataAsync(string id, int? tenantId = null, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null || user.IsOwner || (tenantId.HasValue && user.TenantId != tenantId.Value))
            return Result.Failure<UserEditFormDataDto>(Error.NotFound("User.NotFound", "المستخدم غير موجود."));

        var roles = await _userManager.GetRolesAsync(user);

        var tenants = await _unitOfWork.Tenants.AsQueryable()
            .Where(t => !t.IsDelete)
            .OrderBy(t => t.CompanyName)
            .Select(t => new TenantLookupDto(t.Id, t.CompanyName, t.ContactPerson))
            .ToListAsync(cancellationToken);

        var rolesQuery = _roleManager.Roles.AsQueryable();
        if (tenantId.HasValue)
        {
            rolesQuery = rolesQuery.Where(r => (r.IsSystemRole && r.Name != "SuperAdmin") || r.TenantId == tenantId.Value);
        }

        var availableRoles = await rolesQuery.OrderBy(r => r.Name).ToListAsync(cancellationToken);
        var roleDtos = availableRoles.Select(r => new RoleDto(r.Id, r.Name ?? "", r.Description, r.TenantId, null, r.IsSystemRole, 0, 0)).ToList();

        string? tenantName = null;
        if (user.TenantId.HasValue)
        {
            var tenant = await _unitOfWork.Tenants.GetByIdAsync(user.TenantId.Value);
            tenantName = tenant?.CompanyName;
        }

        var dto = new UserEditFormDataDto(
            user.Id,
            user.FullName ?? string.Empty,
            user.Email ?? string.Empty,
            user.PhoneNumber,
            user.TenantId,
            tenantName,
            user.IsActive,
            user.MustChangePassword,
            roles.ToList(),
            tenants,
            roleDtos
        );

        return Result.Success(dto);
    }

    public async Task<Result<CreateUserResultDto>> CreateUserAsync(CreateUserDto dto, CancellationToken cancellationToken = default)
    {
        var existingUser = await _userManager.FindByEmailAsync(dto.Email);
        if (existingUser != null)
            return Result.Failure<CreateUserResultDto>(Error.Conflict("User.EmailExists", "البريد الإلكتروني مسجل بالفعل لمستخدم آخر."));

        var effectivePassword = dto.AutoGeneratePassword || string.IsNullOrWhiteSpace(dto.TempPassword)
            ? PasswordHelper.GenerateSecureTempPassword(10)
            : dto.TempPassword;

        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            FullName = dto.FullName,
            PhoneNumber = dto.PhoneNumber,
            TenantId = dto.TenantId,
            IsActive = dto.IsActive,
            MustChangePassword = dto.MustChangePassword,
            TemporaryPasswordSetDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, effectivePassword);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Result.Failure<CreateUserResultDto>(Error.Validation("User.CreateFailed", errors));
        }

        // Filter out SuperAdmin if tenantId is specified
        var safeRoles = dto.SelectedRoles ?? new List<string>();
        if (dto.TenantId.HasValue)
        {
            safeRoles = safeRoles.Where(r => r != "SuperAdmin").ToList();
            if (!safeRoles.Any()) safeRoles.Add("TenantOperator");
        }

        if (safeRoles.Any())
        {
            await _userManager.AddToRolesAsync(user, safeRoles);
        }

        var resDto = new CreateUserResultDto(user.Id, user.Email, user.FullName, effectivePassword);
        return Result.Success(resDto);
    }

    public async Task<Result<bool>> UpdateUserAsync(UpdateUserDto dto, int? tenantId = null, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(dto.Id);
        if (user == null || (tenantId.HasValue && user.TenantId != tenantId.Value))
            return Result.Failure<bool>(Error.NotFound("User.NotFound", "المستخدم غير موجود."));

        if (user.IsOwner)
            return Result.Failure<bool>(Error.Validation("User.OwnerProtected", "حساب مالك المنصة محمي بالكامل ولا يمكن تعديل بياناته من أي مستخدم."));

        user.FullName = dto.FullName;
        user.PhoneNumber = dto.PhoneNumber;
        if (!tenantId.HasValue)
        {
            user.TenantId = dto.TenantId;
        }
        user.IsActive = dto.IsActive;
        user.MustChangePassword = dto.MustChangePassword;

        if (user.Email != dto.Email)
        {
            var emailExists = await _userManager.FindByEmailAsync(dto.Email);
            if (emailExists != null && emailExists.Id != user.Id)
                return Result.Failure<bool>(Error.Conflict("User.EmailExists", "البريد الإلكتروني مسجل مسبقاً لمستخدم آخر."));

            user.Email = dto.Email;
            user.UserName = dto.Email;
        }

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
            return Result.Failure<bool>(Error.Validation("User.UpdateFailed", errors));
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        var selectedRoles = dto.SelectedRoles ?? new List<string>();

        if (tenantId.HasValue)
        {
            selectedRoles = selectedRoles.Where(r => r != "SuperAdmin").ToList();
        }

        var rolesToAdd = selectedRoles.Except(currentRoles).ToList();
        var rolesToRemove = currentRoles.Except(selectedRoles).ToList();

        if (rolesToRemove.Any())
            await _userManager.RemoveFromRolesAsync(user, rolesToRemove);

        if (rolesToAdd.Any())
            await _userManager.AddToRolesAsync(user, rolesToAdd);

        return Result.Success(true);
    }

    public async Task<Result<SetTempPasswordResultDto>> SetTempPasswordAsync(string userId, string? customPassword, int? tenantId = null, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || (tenantId.HasValue && user.TenantId != tenantId.Value))
            return Result.Failure<SetTempPasswordResultDto>(Error.NotFound("User.NotFound", "المستخدم غير موجود."));

        if (user.IsOwner)
            return Result.Failure<SetTempPasswordResultDto>(Error.Validation("User.OwnerProtected", "حساب مالك المنصة محمي بالكامل ولا يمكن تعيين كلمة مرور مؤقتة له."));

        var newPassword = string.IsNullOrWhiteSpace(customPassword)
            ? PasswordHelper.GenerateSecureTempPassword(10)
            : customPassword;

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var resetResult = await _userManager.ResetPasswordAsync(user, token, newPassword);

        if (!resetResult.Succeeded)
        {
            var errors = string.Join(", ", resetResult.Errors.Select(e => e.Description));
            return Result.Failure<SetTempPasswordResultDto>(Error.Validation("User.PasswordResetFailed", $"فشل تعيين كلمة المرور: {errors}"));
        }

        user.MustChangePassword = true;
        user.TemporaryPasswordSetDate = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        var dto = new SetTempPasswordResultDto(user.Id, user.FullName ?? user.UserName ?? "مستخدم", user.Email ?? string.Empty, newPassword);
        return Result.Success(dto);
    }

    public async Task<Result<SetTempPasswordResultDto>> SetTenantAdminTempPasswordAsync(int tenantId, string? customPassword, CancellationToken cancellationToken = default)
    {
        var tenant = await _unitOfWork.Tenants.AsQueryable().IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);
        if (tenant == null)
            return Result.Failure<SetTempPasswordResultDto>(Error.NotFound("Tenant.NotFound", "الشركة غير موجودة."));

        var newPassword = string.IsNullOrWhiteSpace(customPassword)
            ? PasswordHelper.GenerateSecureTempPassword(10)
            : customPassword;

        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.TenantId == tenantId, cancellationToken);
        if (user == null)
        {
            var email = !string.IsNullOrWhiteSpace(tenant.ContactEmail)
                ? tenant.ContactEmail
                : $"admin@company{tenantId}.local";

            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = tenant.ContactPerson ?? tenant.CompanyName,
                TenantId = tenantId,
                IsActive = true,
                MustChangePassword = true,
                TemporaryPasswordSetDate = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow,
                EmailConfirmed = true
            };

            var createRes = await _userManager.CreateAsync(user, newPassword);
            if (!createRes.Succeeded)
            {
                var errors = string.Join(", ", createRes.Errors.Select(e => e.Description));
                return Result.Failure<SetTempPasswordResultDto>(Error.Validation("User.CreateFailed", errors));
            }

            // Automatically ensure dedicated tenant role with default permissions exists and link user
            var roleRes = await EnsureTenantDefaultRoleAsync(tenantId, tenant.CompanyName, cancellationToken);
            if (roleRes.IsSuccess)
            {
                await _userManager.AddToRoleAsync(user, roleRes.Value);
            }
        }
        else
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetRes = await _userManager.ResetPasswordAsync(user, token, newPassword);
            if (!resetRes.Succeeded)
            {
                var errors = string.Join(", ", resetRes.Errors.Select(e => e.Description));
                return Result.Failure<SetTempPasswordResultDto>(Error.Validation("User.PasswordResetFailed", errors));
            }

            user.MustChangePassword = true;
            user.TemporaryPasswordSetDate = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            // Ensure user is attached to the company's dedicated role
            var roleRes = await EnsureTenantDefaultRoleAsync(tenantId, tenant.CompanyName, cancellationToken);
            if (roleRes.IsSuccess)
            {
                var userRoles = await _userManager.GetRolesAsync(user);
                if (!userRoles.Contains(roleRes.Value))
                {
                    await _userManager.AddToRoleAsync(user, roleRes.Value);
                }
            }
        }

        var dto = new SetTempPasswordResultDto(user.Id, user.FullName ?? user.UserName ?? "مدير الشركة", user.Email ?? string.Empty, newPassword, tenant.CompanyName);
        return Result.Success(dto);
    }

    public async Task<Result<bool>> ToggleUserStatusAsync(string userId, int? tenantId = null, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || (tenantId.HasValue && user.TenantId != tenantId.Value))
            return Result.Failure<bool>(Error.NotFound("User.NotFound", "المستخدم غير موجود."));

        if (user.IsOwner)
            return Result.Failure<bool>(Error.Validation("User.OwnerProtected", "حساب مالك المنصة محمي بشكل دائم ولا يمكن إيقافه أو تعطيله."));

        user.IsActive = !user.IsActive;
        await _userManager.UpdateAsync(user);

        return Result.Success(user.IsActive);
    }

    public async Task<Result<UserPermissionsDto>> GetUserPermissionsAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || user.IsOwner)
            return Result.Failure<UserPermissionsDto>(Error.NotFound("User.NotFound", "المستخدم غير موجود."));

        var userRoles = await _userManager.GetRolesAsync(user);
        var userClaims = await _userManager.GetClaimsAsync(user);
        var directClaims = userClaims.Where(c => c.Type == AppPermissions.ClaimType).Select(c => c.Value).ToList();

        var inheritedClaims = new HashSet<string>();
        foreach (var roleName in userRoles)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role != null)
            {
                var rClaims = await _roleManager.GetClaimsAsync(role);
                foreach (var rc in rClaims.Where(c => c.Type == AppPermissions.ClaimType))
                {
                    inheritedClaims.Add(rc.Value);
                }
            }
        }

        string? tenantName = null;
        if (user.TenantId.HasValue)
        {
            var tenant = await _unitOfWork.Tenants.GetByIdAsync(user.TenantId.Value);
            tenantName = tenant?.CompanyName;
        }

        var dto = new UserPermissionsDto(
            user.Id,
            user.FullName ?? user.UserName ?? "مستخدم",
            user.Email ?? string.Empty,
            user.TenantId,
            tenantName,
            userRoles.ToList(),
            directClaims,
            inheritedClaims.ToList()
        );

        return Result.Success(dto);
    }

    public async Task<Result<UserPermissionsFormDataDto>> GetUserPermissionsFormDataAsync(string userId, CancellationToken cancellationToken = default)
    {
        var permRes = await GetUserPermissionsAsync(userId, cancellationToken);
        if (!permRes.IsSuccess || permRes.Value == null)
            return Result.Failure<UserPermissionsFormDataDto>(permRes.Error);

        var data = permRes.Value;
        var directSet = data.DirectPermissions.ToHashSet();
        var inheritedSet = data.InheritedRolePermissions.ToHashSet();

        var catalog = AppPermissions.GetForSystem();
        var groups = catalog
            .GroupBy(p => new { p.ModuleAr, p.ModuleKey })
            .Select(g => new PermissionGroupDto(
                g.Key.ModuleAr,
                g.Key.ModuleKey,
                g.Select(p => new PermissionItemDto(
                    p.Code,
                    p.NameAr,
                    p.DescriptionAr,
                    directSet.Contains(p.Code),
                    inheritedSet.Contains(p.Code),
                    p.IsTenantAllowed
                )).ToList()
            )).ToList();

        var formDto = new UserPermissionsFormDataDto(
            data.UserId,
            data.FullName,
            data.Email,
            data.TenantId,
            data.TenantName,
            data.UserRoles,
            data.DirectPermissions,
            data.InheritedRolePermissions,
            groups
        );

        return Result.Success(formDto);
    }

    public async Task<Result<bool>> UpdateUserPermissionsAsync(string userId, List<string> permissions, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return Result.Failure<bool>(Error.NotFound("User.NotFound", "المستخدم غير موجود."));

        if (user.IsOwner)
            return Result.Failure<bool>(Error.Validation("User.OwnerProtected", "حساب مالك المنصة يتمتع بصلاحيات شاملة ومطلقة ولا يمكن تعديل صلاحياته."));

        permissions ??= new List<string>();

        var currentClaims = await _userManager.GetClaimsAsync(user);
        var currentPermClaims = currentClaims.Where(c => c.Type == AppPermissions.ClaimType).ToList();

        foreach (var claim in currentPermClaims)
        {
            await _userManager.RemoveClaimAsync(user, claim);
        }

        foreach (var perm in permissions)
        {
            await _userManager.AddClaimAsync(user, new Claim(AppPermissions.ClaimType, perm));
        }

        return Result.Success(true);
    }

    public async Task<Result<bool>> ChangeRequiredPasswordAsync(string userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return Result.Failure<bool>(Error.NotFound("User.NotFound", "المستخدم غير موجود."));

        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Result.Failure<bool>(Error.Validation("User.PasswordChangeFailed", errors));
        }

        user.MustChangePassword = false;
        user.LastPasswordChangedDate = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        return Result.Success(true);
    }

    public async Task<Result<bool>> ValidateTenantAccessAsync(int tenantId, CancellationToken cancellationToken = default)
    {
        var tenant = await _unitOfWork.Tenants.AsQueryable()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);

        if (tenant == null || tenant.IsDelete)
            return Result.Failure<bool>(Error.NotFound("Tenant.Deleted", "حساب الشركة التابع لها هذا المستخدم محذوف من النظام، ولا يمكن تسجيل الدخول."));

        if (!tenant.IsActive || tenant.IsBlocked)
        {
            var reason = !string.IsNullOrWhiteSpace(tenant.BlockReason) ? $" ({tenant.BlockReason})" : "";
            return Result.Failure<bool>(Error.Validation("Tenant.Suspended", $"حساب الشركة موقوف حالياً من قبل الإدارة{reason}. يرجى مراجعة إدارة المنصة."));
        }

        return Result.Success(true);
    }

    public async Task<Result<List<RoleDto>>> GetRolesAsync(int? tenantId, bool isSystemAdmin, CancellationToken cancellationToken = default)
    {
        var rolesQuery = _roleManager.Roles.AsQueryable();

        if (tenantId.HasValue)
        {
            rolesQuery = rolesQuery.Where(r => r.TenantId == tenantId.Value || (r.IsSystemRole && r.Name != "SuperAdmin"));
        }

        var roles = await rolesQuery.OrderByDescending(r => r.IsSystemRole).ThenBy(r => r.Name).ToListAsync(cancellationToken);
        var tenants = await _unitOfWork.Tenants.AsQueryable().IgnoreQueryFilters().ToDictionaryAsync(t => t.Id, t => t.CompanyName, cancellationToken);

        var list = new List<RoleDto>();
        foreach (var role in roles)
        {
            var roleClaims = await _roleManager.GetClaimsAsync(role);
            var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!);
            var count = tenantId.HasValue ? usersInRole.Count(u => u.TenantId == tenantId.Value) : usersInRole.Count;

            list.Add(new RoleDto(
                role.Id,
                role.Name ?? string.Empty,
                role.Description,
                role.TenantId,
                role.TenantId.HasValue && tenants.TryGetValue(role.TenantId.Value, out var tName) ? tName : (role.TenantId.HasValue ? "مجموعة خاصة" : "النظام العام"),
                role.IsSystemRole,
                count,
                roleClaims.Count(c => c.Type == AppPermissions.ClaimType)
            ));
        }

        return Result.Success(list);
    }

    public async Task<Result<RoleDetailDto>> GetRoleByIdAsync(string id, int? tenantId = null, CancellationToken cancellationToken = default)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role == null || (tenantId.HasValue && role.TenantId != tenantId.Value))
            return Result.Failure<RoleDetailDto>(Error.NotFound("Role.NotFound", "مجموعة الصلاحيات غير موجودة."));

        var roleClaims = await _roleManager.GetClaimsAsync(role);
        var selectedPerms = roleClaims.Where(c => c.Type == AppPermissions.ClaimType).Select(c => c.Value).ToList();

        string? tenantName = null;
        if (role.TenantId.HasValue)
        {
            var tenant = await _unitOfWork.Tenants.GetByIdAsync(role.TenantId.Value);
            tenantName = tenant?.CompanyName;
        }

        var dto = new RoleDetailDto(
            role.Id,
            role.Name ?? string.Empty,
            role.Description,
            role.TenantId,
            tenantName,
            role.IsSystemRole,
            selectedPerms
        );

        return Result.Success(dto);
    }

    public async Task<Result<RoleFormMetadataDto>> GetRoleCreateFormMetadataAsync(int? tenantId = null, bool isSystemAdmin = true, CancellationToken cancellationToken = default)
    {
        var tenants = await _unitOfWork.Tenants.AsQueryable()
            .Where(t => !t.IsDelete)
            .OrderBy(t => t.CompanyName)
            .Select(t => new TenantLookupDto(t.Id, t.CompanyName, t.ContactPerson))
            .ToListAsync(cancellationToken);

        var catalog = (!isSystemAdmin || tenantId.HasValue) ? AppPermissions.GetForTenant() : AppPermissions.GetForSystem();
        var groups = catalog
            .GroupBy(p => new { p.ModuleAr, p.ModuleKey })
            .Select(g => new PermissionGroupDto(
                g.Key.ModuleAr,
                g.Key.ModuleKey,
                g.Select(p => new PermissionItemDto(
                    p.Code,
                    p.NameAr,
                    p.DescriptionAr,
                    false,
                    false,
                    p.IsTenantAllowed
                )).ToList()
            )).ToList();

        return Result.Success(new RoleFormMetadataDto(tenants, groups, tenantId));
    }

    public async Task<Result<RoleEditFormDataDto>> GetRoleEditFormDataAsync(string id, int? tenantId = null, bool isSystemAdmin = true, CancellationToken cancellationToken = default)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role == null || (tenantId.HasValue && role.TenantId != tenantId.Value))
            return Result.Failure<RoleEditFormDataDto>(Error.NotFound("Role.NotFound", "مجموعة الصلاحيات غير موجودة."));

        var roleClaims = await _roleManager.GetClaimsAsync(role);
        var selectedPerms = roleClaims.Where(c => c.Type == AppPermissions.ClaimType).Select(c => c.Value).ToHashSet();

        var tenants = await _unitOfWork.Tenants.AsQueryable()
            .Where(t => !t.IsDelete)
            .OrderBy(t => t.CompanyName)
            .Select(t => new TenantLookupDto(t.Id, t.CompanyName, t.ContactPerson))
            .ToListAsync(cancellationToken);

        string? tenantName = null;
        if (role.TenantId.HasValue)
        {
            var tenant = await _unitOfWork.Tenants.GetByIdAsync(role.TenantId.Value);
            tenantName = tenant?.CompanyName;
        }

        var catalog = (!isSystemAdmin || tenantId.HasValue) ? AppPermissions.GetForTenant() : AppPermissions.GetForSystem();
        var groups = catalog
            .GroupBy(p => new { p.ModuleAr, p.ModuleKey })
            .Select(g => new PermissionGroupDto(
                g.Key.ModuleAr,
                g.Key.ModuleKey,
                g.Select(p => new PermissionItemDto(
                    p.Code,
                    p.NameAr,
                    p.DescriptionAr,
                    selectedPerms.Contains(p.Code),
                    false,
                    p.IsTenantAllowed
                )).ToList()
            )).ToList();

        var dto = new RoleEditFormDataDto(
            role.Id,
            role.Name ?? string.Empty,
            role.Description,
            role.TenantId,
            tenantName,
            role.IsSystemRole,
            selectedPerms.ToList(),
            tenants,
            groups
        );

        return Result.Success(dto);
    }

    public async Task<Result<string>> CreateRoleAsync(CreateRoleDto dto, CancellationToken cancellationToken = default)
    {
        var roleName = dto.Name.Trim();
        if (await _roleManager.RoleExistsAsync(roleName))
            return Result.Failure<string>(Error.Conflict("Role.Exists", "اسم الدور أو المجموعة موجود بالفعل مسبقاً."));

        var role = new ApplicationRole(roleName)
        {
            Description = dto.Description,
            TenantId = dto.TenantId,
            IsSystemRole = false
        };

        var result = await _roleManager.CreateAsync(role);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Result.Failure<string>(Error.Validation("Role.CreateFailed", errors));
        }

        // Business rule: if role is tenant-bound, ensure permissions are strictly tenant-allowed
        var safePermissions = dto.SelectedPermissions ?? new List<string>();
        if (dto.TenantId.HasValue)
        {
            var allowedCodes = AppPermissions.GetForTenant().Select(p => p.Code).ToHashSet();
            safePermissions = safePermissions.Where(p => allowedCodes.Contains(p)).ToList();
        }

        foreach (var perm in safePermissions)
        {
            await _roleManager.AddClaimAsync(role, new Claim(AppPermissions.ClaimType, perm));
        }

        return Result.Success(role.Id);
    }

    public async Task<Result<bool>> UpdateRoleAsync(UpdateRoleDto dto, int? tenantId = null, CancellationToken cancellationToken = default)
    {
        var role = await _roleManager.FindByIdAsync(dto.Id);
        if (role == null || (tenantId.HasValue && role.TenantId != tenantId.Value))
            return Result.Failure<bool>(Error.NotFound("Role.NotFound", "مجموعة الصلاحيات غير موجودة."));

        var roleName = dto.Name.Trim();
        if (!role.IsSystemRole && role.Name != roleName)
        {
            var exists = await _roleManager.RoleExistsAsync(roleName);
            if (exists)
                return Result.Failure<bool>(Error.Conflict("Role.NameConflict", "الاسم الجديد للمجموعة مستخدم بالفعل."));

            role.Name = roleName;
        }

        role.Description = dto.Description;
        if (!role.IsSystemRole && !tenantId.HasValue)
        {
            role.TenantId = dto.TenantId;
        }

        var updateResult = await _roleManager.UpdateAsync(role);
        if (!updateResult.Succeeded)
        {
            var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
            return Result.Failure<bool>(Error.Validation("Role.UpdateFailed", errors));
        }

        var existingClaims = await _roleManager.GetClaimsAsync(role);
        var permClaims = existingClaims.Where(c => c.Type == AppPermissions.ClaimType).ToList();

        foreach (var claim in permClaims)
        {
            await _roleManager.RemoveClaimAsync(role, claim);
        }

        var safePermissions = dto.SelectedPermissions ?? new List<string>();
        if (role.TenantId.HasValue || tenantId.HasValue)
        {
            var allowedCodes = AppPermissions.GetForTenant().Select(p => p.Code).ToHashSet();
            safePermissions = safePermissions.Where(p => allowedCodes.Contains(p)).ToList();
        }

        foreach (var perm in safePermissions)
        {
            await _roleManager.AddClaimAsync(role, new Claim(AppPermissions.ClaimType, perm));
        }

        return Result.Success(true);
    }

    public async Task<Result<bool>> DeleteRoleAsync(string id, int? tenantId = null, CancellationToken cancellationToken = default)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role == null || (tenantId.HasValue && role.TenantId != tenantId.Value))
            return Result.Failure<bool>(Error.NotFound("Role.NotFound", "مجموعة الصلاحيات غير موجودة."));

        if (role.IsSystemRole)
            return Result.Failure<bool>(Error.Forbidden("Role.SystemProtected", "لا يمكن حذف الأدوار الأساسية الخاصة بالنظام."));

        var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!);
        var count = tenantId.HasValue ? usersInRole.Count(u => u.TenantId == tenantId.Value) : usersInRole.Count;
        if (count > 0)
            return Result.Failure<bool>(Error.Conflict("Role.InUse", $"لا يمكن حذف المجموعة '{role.Name}' لوجود {count} مستخدم مرتبطين بها حالياً."));

        await _roleManager.DeleteAsync(role);
        return Result.Success(true);
    }

    public async Task<Result<string>> EnsureTenantDefaultRoleAsync(int tenantId, string companyName, CancellationToken cancellationToken = default)
    {
        var trimmedName = string.IsNullOrWhiteSpace(companyName) ? $"الشركة #{tenantId}" : companyName.Trim();
        var targetRoleName = $"إدارة {trimmedName}";

        // 1. Check if a dedicated role already exists for this tenant
        var existingRole = await _roleManager.Roles.FirstOrDefaultAsync(r => r.TenantId == tenantId, cancellationToken);
        if (existingRole != null)
        {
            // If it exists, ensure it has tenant permissions if it has none
            var claims = await _roleManager.GetClaimsAsync(existingRole);
            if (!claims.Any(c => c.Type == AppPermissions.ClaimType))
            {
                foreach (var perm in AppPermissions.GetForTenant())
                {
                    await _roleManager.AddClaimAsync(existingRole, new Claim(AppPermissions.ClaimType, perm.Code));
                }
            }
            return Result.Success(existingRole.Name!);
        }

        // 2. Role does not exist for this tenant, create one
        var finalRoleName = targetRoleName;
        if (await _roleManager.RoleExistsAsync(finalRoleName))
        {
            finalRoleName = $"{targetRoleName} #{tenantId}";
        }

        var newRole = new ApplicationRole(finalRoleName)
        {
            Description = $"الدور القياسي وإدارة الصلاحيات لشركة {trimmedName}",
            TenantId = tenantId,
            IsSystemRole = false
        };

        var createResult = await _roleManager.CreateAsync(newRole);
        if (!createResult.Succeeded)
        {
            var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
            return Result.Failure<string>(Error.Validation("Role.CreateFailed", errors));
        }

        // Assign all tenant-allowed permissions by default so the company is fully operational immediately
        foreach (var perm in AppPermissions.GetForTenant())
        {
            await _roleManager.AddClaimAsync(newRole, new Claim(AppPermissions.ClaimType, perm.Code));
        }

        return Result.Success(newRole.Name!);
    }

    public async Task<Result<bool>> SyncTenantRoleNameAsync(int tenantId, string newCompanyName, CancellationToken cancellationToken = default)
    {
        var trimmed = newCompanyName?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed)) return Result.Success(true);

        var role = await _roleManager.Roles.FirstOrDefaultAsync(r => r.TenantId == tenantId, cancellationToken);
        if (role != null)
        {
            var newRoleName = $"إدارة {trimmed}";
            if (role.Name != newRoleName)
            {
                if (!await _roleManager.RoleExistsAsync(newRoleName))
                {
                    role.Name = newRoleName;
                }
            }
            role.Description = $"الدور القياسي وإدارة الصلاحيات لشركة {trimmed}";
            await _roleManager.UpdateAsync(role);
        }

        return Result.Success(true);
    }
}
