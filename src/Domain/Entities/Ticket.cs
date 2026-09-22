using SupportDesk.Domain.Common;
using SupportDesk.Domain.Enums;

namespace SupportDesk.Domain.Entities;

public class Ticket : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public required string Subject { get; set; }
    public required string Description { get; set; }

    public required string CustomerEmail { get; set; }
    public string? CustomerName { get; set; }

    public TicketStatus Status { get; set; } = TicketStatus.Open;
    public TicketPriority Priority { get; set; } = TicketPriority.Normal;

    public Guid? AssignedToUserId { get; set; }
    public User? AssignedToUser { get; set; }

    public DateTimeOffset? ResolvedAtUtc { get; set; }

    [System.ComponentModel.DataAnnotations.Timestamp]
    public byte[]? RowVersion { get; set; }

    public ICollection<TicketComment> Comments { get; set; } = [];
}