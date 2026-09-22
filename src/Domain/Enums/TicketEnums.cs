namespace SupportDesk.Domain.Enums;

public enum TicketStatus
{
    Open = 0,
    InProgress = 1,
    WaitingOnCustomer = 2,
    Resolved = 3,
    Closed = 4
}

public enum TicketPriority
{
    Low = 0,
    Normal = 1,
    High = 2,
    Urgent = 3
}

public enum TenantRole
{
    Agent = 0,
    Admin = 1,
    Owner = 2
}