namespace TaskManagement.Task.Services;

public interface INotificationClient
{
    System.Threading.Tasks.Task NotifyTaskAssignedAsync(
        Guid assigneeId,
        Guid taskId,
        string taskTitle,
        CancellationToken cancellationToken = default);
}
