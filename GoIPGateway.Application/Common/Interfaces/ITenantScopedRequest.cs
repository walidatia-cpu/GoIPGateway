namespace GoIPGateway.Application.Common.Interfaces;

/// <summary>
/// علامة (Marker Interface) للطلبات والعمليات التي تتطلب وجود شركة/تاجر مفعل وغير محظور
/// يتم التحقق منها استباقياً في بايبلاين MediatR عبر TenantVerificationBehavior
/// </summary>
public interface ITenantScopedRequest
{
}

