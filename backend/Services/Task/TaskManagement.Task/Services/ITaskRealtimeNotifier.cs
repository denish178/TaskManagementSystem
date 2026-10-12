using TaskManagement.Task.Models;

namespace TaskManagement.Task.Services;

public interface ITaskRealtimeNotifier
{
    System.Threading.Tasks.Task NotifyStatusChangedAsync(
        TaskItem task,
        CancellationToken cancellationToken = default);

    System.Threading.Tasks.Task NotifyAssignedAsync(
        TaskItem task,
        CancellationToken cancellationToken = default);
}
