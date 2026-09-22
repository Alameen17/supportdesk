using Microsoft.EntityFrameworkCore;
using SupportDesk.Application.Auth;
using SupportDesk.Domain.Entities;
using SupportDesk.Domain.Enums;
using SupportDesk.Infrastructure.Persistence;

namespace SupportDesk.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/register", Register);
        group.MapPost("/login", Login);
        group.MapGet("/me", Me).RequireAuthorization();
    }

    private static async Task<IResult> Register(
        RegisterRequest request,
        AppDbContext db,
        IPasswordHasher hasher,
        ITokenService tokens,
        HttpContext http)
    {
        /* Note: no ITenantContext involved here — registration is one of the
        rare operations that legitimately runs "before" a tenant exists.
        TenantContext.IsResolved being false is exactly why the query filter
        in AppDbContext doesn't block this. */

        var slug = request.TenantName.ToLowerInvariant().Replace(" ", "-");

        var slugTaken = await db.Tenants.IgnoreQueryFilters().AnyAsync(t => t.Slug == slug);
        if (slugTaken)
            return Results.Conflict(new { error = "A tenant with a similar name already exists." });

        var tenant = new Tenant { Name = request.TenantName, Slug = slug };
        db.Tenants.Add(tenant);

        var user = new User
        {
            TenantId = tenant.Id,
            Email = request.Email.ToLowerInvariant(),
            DisplayName = request.DisplayName,
            PasswordHash = hasher.Hash(request.Password),
            Role = TenantRole.Owner
        };
        db.Users.Add(user);

        var (accessToken, expiresAt) = tokens.GenerateAccessToken(user);
        var refreshToken = tokens.GenerateRefreshToken();

        user.RefreshTokenHash = hasher.Hash(refreshToken);
        user.RefreshTokenExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(30);

        await db.SaveChangesAsync();

        SetRefreshTokenCookie(http, refreshToken);

        return Results.Ok(new AuthResponse(accessToken, expiresAt, user.Id, tenant.Id, user.DisplayName));
    }

    private static async Task<IResult> Login(
        LoginRequest request,
        AppDbContext db,
        IPasswordHasher hasher,
        ITokenService tokens,
        HttpContext http)
    {
        /* IgnoreQueryFilters is required here too: at login time we don't yet
        know which tenant we're in (that's the whole point of logging in),
        so the normal tenant-scoped filter would find nothing. */

        var user = await db.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == request.Email.ToLowerInvariant());

        /* Deliberately identical error for "no such user" and "wrong password" —
        distinguishing them lets an attacker enumerate valid emails. */

        if (user is null || !hasher.Verify(request.Password, user.PasswordHash))
            return Results.Unauthorized();

        var (accessToken, expiresAt) = tokens.GenerateAccessToken(user);
        var refreshToken = tokens.GenerateRefreshToken();

        user.RefreshTokenHash = hasher.Hash(refreshToken);
        user.RefreshTokenExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(30);
        await db.SaveChangesAsync();

        SetRefreshTokenCookie(http, refreshToken);

        return Results.Ok(new AuthResponse(accessToken, expiresAt, user.Id, user.TenantId, user.DisplayName));
    }

    private static IResult Me(HttpContext http, ITenantContext tenantContext)
    {
    var userId = http.User.FindFirst("sub")?.Value;
    var email = http.User.FindFirst("email")?.Value;
    var role = http.User.FindFirst("role")?.Value;

    return Results.Ok(new
    {
        UserId = userId,
        Email = email,
        Role = role,
        TenantId = tenantContext.TenantId
    });
    }

    private static void SetRefreshTokenCookie(HttpContext http, string refreshToken)
    {
        http.Response.Cookies.Append("refreshToken", refreshToken, new CookieOptions
        {
            HttpOnly = true,       // JS can never read this — mitigates XSS token theft
            Secure = true,         // HTTPS only; browsers may block this on plain http://localhost, that's fine for now
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(30)
        });
    }
}