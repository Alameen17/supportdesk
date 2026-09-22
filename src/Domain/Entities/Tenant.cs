using SupportDesk.Domain.Common;

namespace SupportDesk.Domain.Entities;

public class Tenant : BaseEntity
{
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<User> Users { get; set; } = [];
    public ICollection<Ticket> Tickets { get; set; } = [];
}