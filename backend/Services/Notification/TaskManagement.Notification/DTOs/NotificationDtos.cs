namespace TaskManagement.Notification.DTOs;

public record CreateNotificationRequest(
    Guid UserId,
    string Title,
    string Message,
    string? LinkUrl);

public record NotificationResponse(
    Guid Id,
    Guid UserId,
    string Title,
    string Message,
    string? LinkUrl,
    bool IsRead,
    DateTime CreatedAtUtc);
