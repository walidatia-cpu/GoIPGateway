using GoIPGateway.Application.Features.Auth.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GoIPGateway.Web.Api.V1;

[ApiController]
[Route("api/v1/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    public record TokenRequest(string ApiKey, string SecretKey);
    public record TokenResponse(string Token, int ExpiresIn, string TokenType = "Bearer");

    /// <summary>
    /// الحصول على رمز JWT باستخدام مفاتيح API الخاصة بالشركة
    /// </summary>
    [HttpPost("token")]
    public async Task<IActionResult> GenerateToken([FromBody] TokenRequest request, CancellationToken cancellationToken)
    {
        var command = new GenerateApiTokenCommand(request.ApiKey, request.SecretKey);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.Succeeded || string.IsNullOrEmpty(result.Data))
        {
            return Unauthorized(new { message = result.Error?.Message ?? "Invalid API Key or Secret Key, or Tenant account is suspended." });
        }

        return Ok(new TokenResponse(result.Data, 86400));
    }
}