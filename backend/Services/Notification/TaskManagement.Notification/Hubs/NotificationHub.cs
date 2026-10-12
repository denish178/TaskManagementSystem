using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace TaskManagement.Notification.Hubs;

[Authorize]
public class NotificationHub : Hub
{
    public static string UserGroup(Guid userId) => $"user:{userId}";

    public override async System.Threading.Tasks.Task OnConnectedAsync()
    {
        var id =
            Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? Context.User?.FindFirstValue("sub");

        if (Guid.TryParse(id, out var userId))
        {
            await Groups.AddToGroupAsync(
                Context.ConnectionId,
                UserGroup(userId));
        }

        await base.OnConnectedAsync();
    }
}
