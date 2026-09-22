using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SupportDesk.Application.Auth;
using SupportDesk.Domain.Entities;

namespace SupportDesk.Infrastructure.Auth;

public class JwtTokenService : ITokenService
{
    private readonly string _signingKey;
    private readonly string _issuer;
    private readonly TimeSpan _accessTokenLifetime = TimeSpan.FromMinutes(15);

    public JwtTokenService(IConfiguration configuration)
    {
        /* Fail fast and loud if the config is missing — better than a
        confusing null-reference deep inside token generation at runtime. */
        
        _signingKey = configuration["Jwt:SigningKey"]
            ?? throw new InvalidOperationException("Jwt:SigningKey is not configured.");
        _issuer = configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException("Jwt:Issuer is not configured.");
    }

    public (string AccessToken, DateTimeOffset ExpiresAtUtc) GenerateAccessToken(User user)
    {
        var expiresAt = DateTimeOffset.UtcNow.Add(_accessTokenLifetime);

        /* TenantId and UserId are the two claims everything downstream relies on:
         the tenant-resolution middleware reads TenantId to populate ITenantContext,
         which is what makes the EF Core query filter from earlier actually work. */

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim("tenant_id", user.TenantId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("role", user.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(_signingKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _issuer,
            claims: claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    public string GenerateRefreshToken()
    {
        /* 256-bit random token, base64url-encoded. This is opaque — it's not a JWT,
        just a high-entropy string you'll store a HASH of against the User row
        (never the raw token) and compare against on refresh.*/

        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }
}