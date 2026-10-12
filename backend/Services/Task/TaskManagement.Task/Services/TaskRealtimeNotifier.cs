using Microsoft.AspNetCore.SignalR;
using TaskManagement.Task.Hubs;
using TaskManagement.Task.Models;

namespace TaskManagement.Task.Services;

public class TaskRealtimeNotifier : ITaskRealtimeNotifier
{
    private readonly IHubContext<TaskHub> _hub;

    public TaskRealtimeNotifier(IHubContext<TaskHub> hub)
    {
        _hub = hub;
    }

    public async System.Threading.Tasks.Task NotifyStatusChangedAsync(
        TaskItem task,
        CancellationToken cancellationToken = default)
    {
        await _hub.Clients
            .Group(TaskHub.ProjectGroup(task.ProjectId))
            .SendAsync(
                "TaskStatusChanged",
                new
                {
                    task.Id,
                    task.ProjectId,
                    task.SprintId,
                    task.Status,
                    task.AssigneeId,
                    task.UpdatedAt,
                },
                cancellationToken);
    }

    public async System.Threading.Tasks.Task NotifyAssignedAsync(
        TaskItem task,
        CancellationToken cancellationToken = default)
    {
        await _hub.Clients
            .Group(TaskHub.ProjectGroup(task.ProjectId))
            .SendAsync(
                "TaskAssigned",
                new
                {
                    task.Id,
                    task.ProjectId,
                    task.AssigneeId,
                    task.UpdatedAt,
                },
                cancellationToken);
    }
}
