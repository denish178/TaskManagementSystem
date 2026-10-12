namespace TaskManagement.Task.Models;

public class TaskAuditEntry
{
    public Guid Id { get; set; }

    public Guid TaskId { get; set; }

    public Guid ActorUserId { get; set; }

    /// <summary>StatusChanged or Assigned.</summary>
    public string Action { get; set; } = string.Empty;

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
