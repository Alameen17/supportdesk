namespace SupportDesk.Application.Auth;

// What the client sends to create a brand new tenant + its first user (the Owner).
public record RegisterRequest(
    string TenantName,
    string Email,
    string Password,
    string DisplayName);

// What the client sends to log in to an existing tenant.
public record LoginRequest(
    string Email,
    string Password);

// What we hand back after a successful register or login.
// AccessToken is short-lived and goes in the Authorization header.
// The refresh token itself is NOT in this DTO — it's set as an httpOnly
// cookie by the endpoint directly, so client-side JS can never read it.
public record AuthResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAtUtc,
    Guid UserId,
    Guid TenantId,
    string DisplayName);