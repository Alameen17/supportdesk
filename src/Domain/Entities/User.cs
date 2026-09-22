using SupportDesk.Domain.Common;
using SupportDesk.Domain.Enums;

namespace SupportDesk.Domain.Entities;

public class User : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public required string Email { get; set; }
    public required string DisplayName { get; set; }
    public required string PasswordHash { get; set; }

    public TenantRole Role { get; set; } = TenantRole.Agent;

    public string? RefreshTokenHash { get; set; }
    public DateTimeOffset? RefreshTokenExpiresAtUtc { get; set; }

    public ICollection<Ticket> AssignedTickets { get; set; } = [];
}