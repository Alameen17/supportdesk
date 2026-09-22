namespace SupportDesk.Infrastructure.Persistence;

public interface ITenantContext
{
    Guid TenantId { get; }
    bool IsResolved { get; }
}

public class TenantContext : ITenantContext
{
    private Guid? _tenantId;

    public Guid TenantId => _tenantId
        ?? throw new InvalidOperationException(
            "TenantContext was accessed before being resolved. " +
            "This usually means tenant-scoped data is being queried from an endpoint " +
            "that doesn't require authentication.");

    public bool IsResolved => _tenantId.HasValue;

    public void SetTenant(Guid tenantId) => _tenantId = tenantId;
}