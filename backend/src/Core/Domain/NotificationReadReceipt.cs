namespace Core.Domain;

public sealed class NotificationReadReceipt
{
    public Guid Id { get; set; }

    public Guid NotificationId { get; set; }

    public Notification Notification { get; set; } = null!;

    public string UserId { get; set; } = string.Empty;

    public DateTimeOffset ReadAt { get; set; }
}
