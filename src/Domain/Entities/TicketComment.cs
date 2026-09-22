using SupportDesk.Domain.Common;

namespace SupportDesk.Domain.Entities;

public class TicketComment : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }

    public Guid TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;

    public Guid AuthorUserId { get; set; }
    public User AuthorUser { get; set; } = null!;

    public required string Body { get; set; }
    public bool IsInternalNote { get; set; } = false;
}