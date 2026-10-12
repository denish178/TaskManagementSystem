using System.Security.Claims;
using TaskManagement.Task.Models;

namespace TaskManagement.Task.Services;

public class TaskAuthorizationService
{
    private readonly IProjectAccessClient _projectAccess;

    public TaskAuthorizationService(IProjectAccessClient projectAccess)
    {
        _projectAccess = projectAccess;
    }

    public async Task<bool> CanAccessTaskAsync(
        ClaimsPrincipal user,
        TaskItem task)
    {
        return await CanAccessTaskFromPartsAsync(
            user,
            task.ProjectId,
            task.AssigneeId,
            task.CreatedBy);
    }

    public async Task<bool> CanAccessTaskFromPartsAsync(
        ClaimsPrincipal user,
        Guid projectId,
        Guid? assigneeId,
        Guid createdBy)
    {
        if (user.IsInRole("Admin"))
        {
            return true;
        }

        var userId = GetUserId(user);

        if (assigneeId == userId || createdBy == userId)
        {
            return true;
        }

        return await _projectAccess.CanAccessProjectAsync(projectId);
    }

    public static Guid GetUserId(ClaimsPrincipal user)
    {
        var userId =
            user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("sub");

        if (!Guid.TryParse(userId, out var parsedUserId))
        {
            throw new UnauthorizedAccessException(
                "User ID claim is missing or invalid.");
        }

        return parsedUserId;
    }
}
