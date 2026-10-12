namespace TaskManagement.Task.Services;

public interface ITaskAuditService
{
    System.Threading.Tasks.Task RecordAsync(
        Guid taskId,
        Guid actorUserId,
        string action,
        string? oldValue,
        string? newValue,
        CancellationToken cancellationToken = default);
}
