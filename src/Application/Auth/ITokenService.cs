using SupportDesk.Domain.Entities;

namespace SupportDesk.Application.Auth;

public interface ITokenService
{
    (string AccessToken, DateTimeOffset ExpiresAtUtc) GenerateAccessToken(User user);
    string GenerateRefreshToken();
}