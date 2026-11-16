using System.Security.Claims;
using GoIPGateway.Application.Common.Security;
using GoIPGateway.Infrastructure.Identity.Permissions;
using GoIPGateway.Web.Extensions;
using GoIPGateway.Web.Middleware;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace GoIPGateway.Tests;

public class UserPermissionsAndTempPasswordTests
{
    [Fact]
    public void GenerateSecureTempPassword_ShouldMeetComplexityRequirements()
    {
        // Act
        var password = PasswordHelper.GenerateSecureTempPassword(12);

        // Assert
        Assert.NotNull(password);
        Assert.Equal(12, password.Length);
        Assert.Contains(password, c => char.IsUpper(c));
        Assert.Contains(password, c => char.IsLower(c));
        Assert.Contains(password, c => char.IsDigit(c));
        Assert.Contains(password, c => "!@#$%&*".Contains(c));
    }

    [Fact]
    public async Task PermissionHandler_SuperAdmin_ShouldAutomaticallySucceed()
    {
        // Arrange
        var handler = new PermissionAuthorizationHandler();
        var requirement = new PermissionRequirement(AppPermissions.Devices.Create);
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "admin"),
            new Claim(ClaimTypes.Role, "SuperAdmin")
        }, "TestAuth"));

        var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

        // Act
        await handler.HandleAsync(context);

        // Assert
        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task PermissionHandler_UserWithPermissionClaim_ShouldSucceed()
    {
        // Arrange
        var handler = new PermissionAuthorizationHandler();
        var requirement = new PermissionRequirement(AppPermissions.SimCards.Recharge);
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "employee"),
            new Claim(ClaimTypes.Role, "TenantOperator"),
            new Claim(AppPermissions.ClaimType, AppPermissions.SimCards.Recharge)
        }, "TestAuth"));

        var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

        // Act
        await handler.HandleAsync(context);

        // Assert
        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public void ClaimsPrincipalExtensions_HasPermission_ShouldCheckCorrectly()
    {
        // 1. Owner has unrestricted access
        var owner = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("IsOwner", "True")
        }, "TestAuth"));
        Assert.True(owner.HasPermission(AppPermissions.Devices.Create));

        // 2. SuperAdmin has unrestricted access
        var superAdmin = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, "SuperAdmin")
        }, "TestAuth"));
        Assert.True(superAdmin.HasPermission(AppPermissions.Devices.Create));

        // 3. User with explicit claim has access
        var grantedUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "employee"),
            new Claim(AppPermissions.ClaimType, AppPermissions.SimCards.View)
        }, "TestAuth"));
        Assert.True(grantedUser.HasPermission(AppPermissions.SimCards.View));
        Assert.False(grantedUser.HasPermission(AppPermissions.Devices.Create));

        // 4. User without claim has NO access
        var noPermUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "empty_user"),
            new Claim(ClaimTypes.Role, "TenantAdmin")
        }, "TestAuth"));
        Assert.False(noPermUser.HasPermission(AppPermissions.Devices.View));
        Assert.False(noPermUser.HasPermission(AppPermissions.SimCards.View));

        // 5. Unauthenticated user has NO access
        var unauthUser = new ClaimsPrincipal(new ClaimsIdentity());
        Assert.False(unauthUser.HasPermission(AppPermissions.Devices.View));
    }

    [Fact]
    public async Task PermissionHandler_UserWithoutPermissionClaim_ShouldNotSucceed()
    {
        // Arrange
        var handler = new PermissionAuthorizationHandler();
        var requirement = new PermissionRequirement(AppPermissions.Tenants.Create);
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "operator"),
            new Claim(ClaimTypes.Role, "TenantOperator")
        }, "TestAuth"));

        var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

        // Act
        await handler.HandleAsync(context);

        // Assert
        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task PermissionHandler_TenantAdminWithoutPermissionClaim_MustNotSucceed()
    {
        // Arrange - Company user with TenantAdmin role but ZERO permissions assigned
        var handler = new PermissionAuthorizationHandler();
        var requirement = new PermissionRequirement(AppPermissions.Devices.View);
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "company_admin"),
            new Claim(ClaimTypes.Role, "TenantAdmin"),
            new Claim("TenantId", "10")
        }, "TestAuth"));

        var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

        // Act
        await handler.HandleAsync(context);

        // Assert - MUST NOT bypass; should fail authorization
        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task PermissionHandler_TenantAdminWithPermissionClaim_ShouldSucceed()
    {
        // Arrange - Company user with TenantAdmin role and explicit permission claim
        var handler = new PermissionAuthorizationHandler();
        var requirement = new PermissionRequirement(AppPermissions.Devices.View);
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "company_admin"),
            new Claim(ClaimTypes.Role, "TenantAdmin"),
            new Claim("TenantId", "10"),
            new Claim(AppPermissions.ClaimType, AppPermissions.Devices.View)
        }, "TestAuth"));

        var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

        // Act
        await handler.HandleAsync(context);

        // Assert
        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task ForcePasswordChangeMiddleware_WhenUserMustChangePassword_ShouldRedirectToChangePassword()
    {
        // Arrange
        var redirectedPath = string.Empty;
        var nextCalled = false;

        var middleware = new ForcePasswordChangeMiddleware(innerContext =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        var context = new DefaultHttpContext();
        context.Request.Path = "/Company/Dashboard";
        context.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "new_user"),
            new Claim("MustChangePassword", "True")
        }, "TestAuth"));

        context.Response.OnStarting(() => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.False(nextCalled);
        Assert.Equal(302, context.Response.StatusCode);
        Assert.Equal("/Account/ChangePasswordRequired", context.Response.Headers.Location.ToString());
    }

    [Fact]
    public async Task ForcePasswordChangeMiddleware_WhenUserOnAllowedPath_ShouldProceed()
    {
        // Arrange
        var nextCalled = false;

        var middleware = new ForcePasswordChangeMiddleware(innerContext =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        var context = new DefaultHttpContext();
        context.Request.Path = "/Account/ChangePasswordRequired";
        context.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "new_user"),
            new Claim("MustChangePassword", "True")
        }, "TestAuth"));

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(nextCalled);
    }

    [Fact]
    public async Task ForcePasswordChangeMiddleware_WhenMustChangePasswordIsFalse_ShouldProceedNormally()
    {
        // Arrange
        var nextCalled = false;

        var middleware = new ForcePasswordChangeMiddleware(innerContext =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        var context = new DefaultHttpContext();
        context.Request.Path = "/Company/Dashboard";
        context.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "regular_user"),
            new Claim("MustChangePassword", "False")
        }, "TestAuth"));

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(nextCalled);
    }

    [Fact]
    public async Task CreateUserCommandHandler_UsingMediatorAndResultPattern_ShouldReturnSuccessResult()
    {
        // Arrange
        var mockService = new Moq.Mock<GoIPGateway.Application.Common.Interfaces.IIdentityService>();
        var dto = new GoIPGateway.Application.DTOs.Identity.CreateUserDto(
            "Test Employee", "emp@test.local", "01000000000", 1, new List<string> { "TenantOperator" }, "Pass#12345");

        mockService.Setup(s => s.CreateUserAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GoIPGateway.Application.Common.Models.Result.Success(
                new GoIPGateway.Application.DTOs.Identity.CreateUserResultDto("usr_123", dto.Email, dto.FullName, "Pass#12345")));

        var handler = new GoIPGateway.Application.Features.Users.Commands.CreateUserCommandHandler(mockService.Object);

        // Act
        var result = await handler.Handle(new GoIPGateway.Application.Features.Users.Commands.CreateUserCommand(dto), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.Equal("usr_123", result.Value.UserId);
        Assert.Equal("Pass#12345", result.Value.GeneratedPassword);
    }

    [Fact]
    public async Task SetTenantAdminTempPasswordHandler_ShouldReturnResultPatternSuccess()
    {
        // Arrange
        var mockService = new Moq.Mock<GoIPGateway.Application.Common.Interfaces.IIdentityService>();
        mockService.Setup(s => s.SetTenantAdminTempPasswordAsync(10, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GoIPGateway.Application.Common.Models.Result.Success(
                new GoIPGateway.Application.DTOs.Identity.SetTempPasswordResultDto("admin_id", "Admin Name", "admin@tenant10.com", "TempPass#99", "Tenant 10")));

        var handler = new GoIPGateway.Application.Features.Tenants.Commands.SetTenantAdminTempPasswordCommandHandler(mockService.Object);

        // Act
        var result = await handler.Handle(new GoIPGateway.Application.Features.Tenants.Commands.SetTenantAdminTempPasswordCommand(10), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.Equal("TempPass#99", result.Value.TemporaryPassword);
        Assert.Equal("Tenant 10", result.Value.CompanyName);
    }

    [Fact]
    public async Task GetRoleCreateFormMetadataQueryHandler_ShouldInvokeIdentityService()
    {
        // Arrange
        var mockService = new Moq.Mock<GoIPGateway.Application.Common.Interfaces.IIdentityService>();
        var fakeMetadata = new GoIPGateway.Application.DTOs.Identity.RoleFormMetadataDto(
            new List<GoIPGateway.Application.DTOs.Identity.TenantLookupDto> { new(1, "Company Alpha") },
            new List<GoIPGateway.Application.DTOs.Identity.PermissionGroupDto>
            {
                new("الأجهزة", "Devices", new List<GoIPGateway.Application.DTOs.Identity.PermissionItemDto>
                {
                    new("Permissions.Devices.View", "عرض الأجهزة", "وصف", false, false, true)
                })
            }
        );

        mockService.Setup(s => s.GetRoleCreateFormMetadataAsync(null, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GoIPGateway.Application.Common.Models.Result.Success(fakeMetadata));

        var handler = new GoIPGateway.Application.Features.Roles.Queries.GetRoleCreateFormMetadataQueryHandler(mockService.Object);

        // Act
        var result = await handler.Handle(new GoIPGateway.Application.Features.Roles.Queries.GetRoleCreateFormMetadataQuery(), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Tenants);
        Assert.Single(result.Value.PermissionGroups);
        mockService.Verify(s => s.GetRoleCreateFormMetadataAsync(null, true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetUserCreateFormMetadataQueryHandler_ShouldInvokeIdentityService()
    {
        // Arrange
        var mockService = new Moq.Mock<GoIPGateway.Application.Common.Interfaces.IIdentityService>();
        var fakeMetadata = new GoIPGateway.Application.DTOs.Identity.UserCreateFormMetadataDto(
            new List<GoIPGateway.Application.DTOs.Identity.TenantLookupDto> { new(2, "Company Beta") },
            new List<GoIPGateway.Application.DTOs.Identity.RoleDto> { new("role_1", "Manager", "Desc", 2, "Company Beta", false, 1, 5) },
            "Secure#Temp99"
        );

        mockService.Setup(s => s.GetUserCreateFormMetadataAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GoIPGateway.Application.Common.Models.Result.Success(fakeMetadata));

        var handler = new GoIPGateway.Application.Features.Users.Queries.GetUserCreateFormMetadataQueryHandler(mockService.Object);

        // Act
        var result = await handler.Handle(new GoIPGateway.Application.Features.Users.Queries.GetUserCreateFormMetadataQuery(2), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.Equal("Secure#Temp99", result.Value.DefaultTempPassword);
        Assert.Single(result.Value.AvailableRoles);
        mockService.Verify(s => s.GetUserCreateFormMetadataAsync(2, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AdminRolesController_CreateGet_ShouldOnlyCallMediatorAndReturnViewModel()
    {
        // Arrange
        var mockMediator = new Moq.Mock<MediatR.IMediator>();
        var fakeMetadata = new GoIPGateway.Application.DTOs.Identity.RoleFormMetadataDto(
            new List<GoIPGateway.Application.DTOs.Identity.TenantLookupDto> { new(1, "Company Alpha") },
            new List<GoIPGateway.Application.DTOs.Identity.PermissionGroupDto>
            {
                new("الأجهزة", "Devices", new List<GoIPGateway.Application.DTOs.Identity.PermissionItemDto>
                {
                    new("Permissions.Devices.View", "عرض الأجهزة", "وصف", false, false, true)
                })
            }
        );

        mockMediator.Setup(m => m.Send(It.IsAny<GoIPGateway.Application.Features.Roles.Queries.GetRoleCreateFormMetadataQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GoIPGateway.Application.Common.Models.Result.Success(fakeMetadata));

        var controller = new GoIPGateway.Web.Areas.Admin.Controllers.RolesController(mockMediator.Object);

        // Act
        var actionResult = await controller.Create(CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<Microsoft.AspNetCore.Mvc.ViewResult>(actionResult);
        var model = Assert.IsType<GoIPGateway.Web.Models.Roles.CreateOrEditRoleViewModel>(viewResult.Model);
        Assert.Single(model.PermissionGroups);
        Assert.Equal("الأجهزة", model.PermissionGroups[0].ModuleName);
        mockMediator.Verify(m => m.Send(It.IsAny<GoIPGateway.Application.Features.Roles.Queries.GetRoleCreateFormMetadataQuery>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PermissionHandler_Owner_ShouldAutomaticallySucceedOnAnyRequirement()
    {
        // Arrange
        var handler = new PermissionAuthorizationHandler();
        var requirement = new PermissionRequirement("Permissions.Restricted.CoreOperations");
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "owner_user"),
            new Claim("IsOwner", "True")
        }, "TestAuth"));

        var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

        // Act
        await handler.HandleAsync(context);

        // Assert
        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public void CurrentUserService_WhenOwner_ShouldReturnIsOwnerAndIsSuperAdmin()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "owner_id"),
            new Claim(ClaimTypes.Name, "owner@goipgateway.com"),
            new Claim("IsOwner", "True")
        }, "TestAuth"));

        var mockAccessor = new Moq.Mock<IHttpContextAccessor>();
        mockAccessor.Setup(a => a.HttpContext).Returns(httpContext);

        var currentUserService = new GoIPGateway.Web.Services.CurrentUserService(mockAccessor.Object);

        // Act & Assert
        Assert.True(currentUserService.IsOwner);
        Assert.True(currentUserService.IsSuperAdmin);
    }

    [Fact]
    public async Task CreateTenantAsync_ShouldCallEnsureTenantDefaultRoleAsync()
    {
        // Arrange
        var mockUow = new Mock<GoIPGateway.Application.Common.Interfaces.Repositories.IUnitOfWork>();
        var mockTenantsRepo = new Mock<GoIPGateway.Application.Common.Interfaces.Repositories.ITenantRepository>();
        var mockKeysRepo = new Mock<GoIPGateway.Application.Common.Interfaces.Repositories.IRepository<GoIPGateway.Domain.Entities.TenantApiKey, int>>();
        var mockCurrentUser = new Mock<GoIPGateway.Application.Common.Interfaces.ICurrentUserService>();
        var mockIdentityService = new Mock<GoIPGateway.Application.Common.Interfaces.IIdentityService>();

        mockUow.Setup(u => u.Tenants).Returns(mockTenantsRepo.Object);
        mockUow.Setup(u => u.TenantApiKeys).Returns(mockKeysRepo.Object);
        mockCurrentUser.Setup(c => c.UserId).Returns("admin_id");
        mockIdentityService.Setup(i => i.EnsureTenantDefaultRoleAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GoIPGateway.Application.Common.Models.Result.Success("إدارة شركة فاست باي"));

        var commandService = new GoIPGateway.Application.Services.Tenants.TenantCommandService(
            mockUow.Object,
            mockCurrentUser.Object,
            mockIdentityService.Object);

        var command = new GoIPGateway.Application.Features.Tenants.Commands.CreateTenantCommand(
            "شركة فاست باي",
            "FastPay",
            "أحمد",
            "ahmed@fastpay.com",
            "01000000000",
            "القاهرة");

        // Act
        var result = await commandService.CreateTenantAsync(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        mockIdentityService.Verify(i => i.EnsureTenantDefaultRoleAsync(It.IsAny<int>(), "شركة فاست باي", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateTenantAsync_ShouldCallSyncTenantRoleNameAsync()
    {
        // Arrange
        var mockUow = new Mock<GoIPGateway.Application.Common.Interfaces.Repositories.IUnitOfWork>();
        var mockCurrentUser = new Mock<GoIPGateway.Application.Common.Interfaces.ICurrentUserService>();
        var mockIdentityService = new Mock<GoIPGateway.Application.Common.Interfaces.IIdentityService>();

        var existingTenant = new GoIPGateway.Domain.Entities.Tenant
        {
            Id = 5,
            CompanyName = "الاسم القديم",
            IsActive = true
        };

        mockUow.Setup(u => u.Tenants.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(existingTenant);
        mockCurrentUser.Setup(c => c.UserId).Returns("admin_id");
        mockIdentityService.Setup(i => i.SyncTenantRoleNameAsync(5, "الاسم الجديد", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GoIPGateway.Application.Common.Models.Result.Success(true));

        var commandService = new GoIPGateway.Application.Services.Tenants.TenantCommandService(
            mockUow.Object,
            mockCurrentUser.Object,
            mockIdentityService.Object);

        // Act
        var result = await commandService.UpdateTenantAsync(5, "الاسم الجديد", null, null, null, null, null, null, 1, 1000, 10000, 0, true, false, null);

        // Assert
        Assert.True(result.IsSuccess);
        mockIdentityService.Verify(i => i.SyncTenantRoleNameAsync(5, "الاسم الجديد", It.IsAny<CancellationToken>()), Times.Once);
    }
}

