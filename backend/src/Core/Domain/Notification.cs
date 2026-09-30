namespace Core.Domain;

public sealed class Notification
{
    public Guid Id { get; set; }

    public string Type { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public Guid PayrollId { get; set; }

    public Payroll Payroll { get; set; } = null!;

    public string? UserId { get; set; }

    public string? RoleTarget { get; set; }

    public bool IsRead { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
