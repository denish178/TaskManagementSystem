using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace TaskManagement.Comment.Hubs;

[Authorize]
public class CommentHub : Hub
{
    public static string TaskGroup(Guid taskId) => $"task:{taskId}";

    public async System.Threading.Tasks.Task JoinTask(Guid taskId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, TaskGroup(taskId));
    }

    public async System.Threading.Tasks.Task LeaveTask(Guid taskId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, TaskGroup(taskId));
    }
}
