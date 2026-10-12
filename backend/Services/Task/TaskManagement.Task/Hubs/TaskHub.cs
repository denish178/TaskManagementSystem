using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace TaskManagement.Task.Hubs;

[Authorize]
public class TaskHub : Hub
{
    public static string ProjectGroup(Guid projectId) =>
        $"project:{projectId}";

    public async System.Threading.Tasks.Task JoinProject(Guid projectId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, ProjectGroup(projectId));
    }

    public async System.Threading.Tasks.Task LeaveProject(Guid projectId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, ProjectGroup(projectId));
    }
}
