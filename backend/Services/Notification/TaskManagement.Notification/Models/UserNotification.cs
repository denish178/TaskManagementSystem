namespace TaskManagement.Notification.Models;

public class UserNotification
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string Title { get; set; } = "";

    public string Message { get; set; } = "";

    public string? LinkUrl { get; set; }

    public bool IsRead { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
