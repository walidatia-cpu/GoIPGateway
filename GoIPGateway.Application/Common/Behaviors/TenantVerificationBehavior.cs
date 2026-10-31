using GoIPGateway.Application.Common.Interfaces;
using GoIPGateway.Application.Common.Interfaces.Repositories;
using GoIPGateway.Application.Common.Models;
using MediatR;

namespace GoIPGateway.Application.Common.Behaviors;

/// <summary>
/// وسيط بايبلاين للتحقق الاستباقي من حساب الشركة/التاجر قبل دخول الطلب إلى السيرفس
/// يمنع تكرار كود التحقق من الـ Tenant في كل معالج خدمة
/// </summary>
public class TenantVerificationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ITenantProvider _tenantProvider;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public TenantVerificationBehavior(
        ITenantProvider tenantProvider,
        ICurrentUserService currentUser,
        IUnitOfWork unitOfWork)
    {
        _tenantProvider = tenantProvider;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is not ITenantScopedRequest)
        {
            return await next();
        }

        var tenantId = _tenantProvider.GetCurrentTenantId() ?? _currentUser.TenantId;
        if (!tenantId.HasValue)
        {
            return CreateFailure("Auth.UnauthorizedTenant", "تنفيذ العمليات مخصص حصرياً للشركات والتجار من خلال حساباتهم. لا يُسمح لمدير النظام (Admin) بتنفيذ عمليات مباشرة.");
        }

        var tenant = await _unitOfWork.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId.Value && !t.IsDelete, cancellationToken);
        if (tenant == null)
        {
            return CreateFailure("Tenant.NotFound", "الشركة غير موجودة.");
        }

        if (!tenant.IsActive || tenant.IsBlocked)
        {
            return CreateFailure("Tenant.Blocked", $"حساب الشركة متوقف أو محظور: {tenant.BlockReason}");
        }

        // تثبيت هوية التاجر في مزود السياق لجميع الاستعلامات التالية
        _tenantProvider.SetCurrentTenantId(tenant.Id);

        return await next();
    }

    private static TResponse CreateFailure(string code, string message)
    {
        if (typeof(TResponse).IsGenericType &&
            typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
        {
            var resultType = typeof(TResponse).GetGenericArguments()[0];
            var method = typeof(Result)
                .GetMethods()
                .First(m => m.Name == nameof(Result.Failure) && m.IsGenericMethod && m.GetParameters().Length == 3)
                .MakeGenericMethod(resultType);

            return (TResponse)method.Invoke(null, new object[] { code, message, ErrorType.Failure })!;
        }

        if (typeof(TResponse) == typeof(Result))
        {
            return (TResponse)(object)Result.Failure(code, message);
        }

        throw new UnauthorizedAccessException(message);
    }
}

