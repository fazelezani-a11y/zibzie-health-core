using Microsoft.AspNetCore.Mvc;
using Zibzie.HealthCore.Api.Security;
using Zibzie.HealthCore.Application.Security;
using Zibzie.HealthCore.Domain.Security;

namespace Zibzie.HealthCore.Api.Controllers;

[ApiController]
[Route("api/health-core/auth/admin")]
public class AdminAuthController : ControllerBase
{
    private const string InvalidCredentialsMessage = "Invalid username or password.";

    private readonly IAdminAuthService _adminAuthService;
    private readonly IHealthCoreRequestContextProvider _requestContextProvider;

    public AdminAuthController(
        IAdminAuthService adminAuthService,
        IHealthCoreRequestContextProvider requestContextProvider)
    {
        _adminAuthService = adminAuthService;
        _requestContextProvider = requestContextProvider;
    }

    [HttpPost("login")]
    public async Task<ActionResult<AdminLoginResponseDto>> Login(
        [FromBody] AdminLoginRequestDto? request,
        CancellationToken cancellationToken)
    {
        var requestContext = _requestContextProvider.GetCurrent();
        var result = await _adminAuthService.LoginAsync(
            request?.Username,
            request?.Password,
            requestContext,
            cancellationToken);

        if (!result.Succeeded || result.Token is null || !result.AdminUserId.HasValue)
        {
            return Unauthorized(new { message = InvalidCredentialsMessage });
        }

        return Ok(new AdminLoginResponseDto
        {
            AccessToken = result.Token.AccessToken,
            TokenType = result.Token.TokenType,
            ExpiresAt = result.Token.ExpiresAt,
            ProductCode = result.ProductCode,
            ProductRole = result.ProductRole ?? string.Empty,
            Admin = new AdminUserInfoDto
            {
                Id = result.AdminUserId.Value,
                Username = result.Username ?? string.Empty,
                DisplayName = result.DisplayName,
                ProductRole = result.ProductRole ?? string.Empty
            }
        });
    }

    [HttpGet("me")]
    public ActionResult<AdminMeResponseDto> Me()
    {
        var requestContext = _requestContextProvider.GetCurrent();

        if (!requestContext.IsAuthenticated
            || !requestContext.UserId.HasValue
            || requestContext.ProductCode != ProductCodes.InternalAdmin
            || string.IsNullOrWhiteSpace(requestContext.ProductRole))
        {
            return Unauthorized(new { message = "Authentication is required." });
        }

        return Ok(new AdminMeResponseDto
        {
            UserId = requestContext.UserId.Value,
            ProductCode = requestContext.ProductCode,
            ProductRole = requestContext.ProductRole,
            DisplayName = User.FindFirst("name")?.Value
        });
    }
}

public sealed class AdminLoginRequestDto
{
    public string? Username { get; set; }

    public string? Password { get; set; }
}

public sealed class AdminLoginResponseDto
{
    public string AccessToken { get; set; } = string.Empty;

    public string TokenType { get; set; } = "Bearer";

    public DateTimeOffset ExpiresAt { get; set; }

    public string ProductCode { get; set; } = string.Empty;

    public string ProductRole { get; set; } = string.Empty;

    public AdminUserInfoDto Admin { get; set; } = new();
}

public sealed class AdminUserInfoDto
{
    public Guid Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public string? DisplayName { get; set; }

    public string ProductRole { get; set; } = string.Empty;
}

public sealed class AdminMeResponseDto
{
    public Guid UserId { get; set; }

    public string ProductCode { get; set; } = string.Empty;

    public string ProductRole { get; set; } = string.Empty;

    public string? DisplayName { get; set; }
}
