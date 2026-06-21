using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Zibzie.HealthCore.Domain.Entities;
using Zibzie.HealthCore.Domain.Security;

namespace Zibzie.HealthCore.Api.Security;

public class JwtTokenService : IJwtTokenService
{
    private const int MinimumSigningKeyBytes = 32;

    private readonly JwtOptions _jwtOptions;

    public JwtTokenService(IOptions<JwtOptions> jwtOptions)
    {
        _jwtOptions = jwtOptions.Value;
    }

    public AdminAccessToken CreateAdminAccessToken(AdminUser adminUser)
    {
        ArgumentNullException.ThrowIfNull(adminUser);

        var signingKey = RequiredSigningKey();
        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.AddMinutes(Math.Max(1, _jwtOptions.AccessTokenMinutes));
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, adminUser.Id.ToString()),
            new("user_id", adminUser.Id.ToString()),
            new("product_code", ProductCodes.InternalAdmin),
            new("product_role", adminUser.ProductRole),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(
                JwtRegisteredClaimNames.Iat,
                now.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64)
        };

        if (!string.IsNullOrWhiteSpace(adminUser.DisplayName))
        {
            claims.Add(new Claim("name", adminUser.DisplayName.Trim()));
        }

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return new AdminAccessToken
        {
            AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
            ExpiresAt = expiresAt
        };
    }

    private string RequiredSigningKey()
    {
        var signingKey = _jwtOptions.EffectiveSigningKey;

        if (string.IsNullOrWhiteSpace(signingKey))
        {
            throw new InvalidOperationException("JWT signing key is not configured.");
        }

        if (Encoding.UTF8.GetByteCount(signingKey) < MinimumSigningKeyBytes)
        {
            throw new InvalidOperationException("JWT signing key is too short.");
        }

        return signingKey;
    }
}
