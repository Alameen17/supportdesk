using SupportDesk.Infrastructure.Persistence;

namespace SupportDesk.Api.Middleware;

/* <summary>
 Runs once per request, after authentication has parsed the JWT into
 HttpContext.User. Reads the tenant_id claim and pushes it into the
 scoped ITenantContext, which AppDbContext's query filter reads from.
 This is the middleware that makes the whole tenant-isolation chain real:
 JWT claim -> TenantContext -> EF Core query filter.
 </summary> */
public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;

    public TenantResolutionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext)
    {
        var tenantClaim = context.User.FindFirst("tenant_id");

        if (tenantClaim is not null && Guid.TryParse(tenantClaim.Value, out var tenantId))
        {
            ((TenantContext)tenantContext).SetTenant(tenantId);
        }

        /* No claim present (e.g. hitting /api/auth/login) is fine — IsResolved
         stays false, and endpoints that need a tenant are protected by
        [Authorize] anyway, so they'd never reach here without a valid JWT. */

        await _next(context);
    }
}