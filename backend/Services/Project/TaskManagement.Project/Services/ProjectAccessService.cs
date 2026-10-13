using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Project.Data;

namespace TaskManagement.Project.Services;

public class ProjectAccessService
{
    private readonly ProjectDbContext _context;
    private readonly ITaskProjectAccessClient _taskProjectAccess;

    public ProjectAccessService(
        ProjectDbContext context,
        ITaskProjectAccessClient taskProjectAccess)
    {
        _context = context;
        _taskProjectAccess = taskProjectAccess;
    }

    public static Guid GetUserId(ClaimsPrincipal user)
    {
        var id =
            user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(id) || !Guid.TryParse(id, out var userId))
        {
            throw new UnauthorizedAccessException("User ID claim not found.");
        }

        return userId;
    }

    public static bool IsAdmin(ClaimsPrincipal user) =>
        user.IsInRole("Admin");

    public async Task<bool> CanAccessTeamAsync(
        Guid userId,
        Guid teamId,
        bool isAdmin)
    {
        if (isAdmin)
        {
            return true;
        }

        return await _context.TeamMembers
            .AsNoTracking()
            .AnyAsync(m => m.TeamId == teamId && m.UserId == userId);
    }

    public async Task<bool> CanAccessProjectAsync(
        Guid userId,
        Guid projectId,
        bool isAdmin)
    {
        if (isAdmin)
        {
            return true;
        }

        var teamId = await _context.Projects
            .AsNoTracking()
            .Where(p => p.Id == projectId)
            .Select(p => p.TeamId)
            .FirstOrDefaultAsync();

        if (teamId == Guid.Empty)
        {
            return false;
        }

        if (await CanAccessTeamAsync(userId, teamId, false))
        {
            return true;
        }

        var fromTasks = await _taskProjectAccess.GetAccessibleProjectIdsAsync();
        return fromTasks.Contains(projectId);
    }

    public async Task<List<Guid>> GetAccessibleTeamIdsAsync(
        Guid userId,
        bool isAdmin)
    {
        if (isAdmin)
        {
            return await _context.Teams
                .AsNoTracking()
                .Select(t => t.Id)
                .ToListAsync();
        }

        return await _context.TeamMembers
            .AsNoTracking()
            .Where(m => m.UserId == userId)
            .Select(m => m.TeamId)
            .Distinct()
            .ToListAsync();
    }

    public async Task<List<Guid>> GetAccessibleProjectIdsAsync(
        Guid userId,
        bool isAdmin)
    {
        if (isAdmin)
        {
            return await _context.Projects
                .AsNoTracking()
                .Select(p => p.Id)
                .ToListAsync();
        }

        var teamIds = await GetAccessibleTeamIdsAsync(userId, false);

        var projectIds = teamIds.Count == 0
            ? new List<Guid>()
            : await _context.Projects
                .AsNoTracking()
                .Where(p => teamIds.Contains(p.TeamId))
                .Select(p => p.Id)
                .ToListAsync();

        var result = projectIds.ToHashSet();
        var fromTasks = await _taskProjectAccess.GetAccessibleProjectIdsAsync();
        foreach (var id in fromTasks)
        {
            result.Add(id);
        }

        return result.ToList();
    }

    public async Task<bool> EnsureTeamMemberForProjectAsync(
        Guid projectId,
        Guid userId)
    {
        var project = await _context.Projects
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == projectId);

        if (project == null)
        {
            return false;
        }

        var alreadyMember = await _context.TeamMembers
            .AnyAsync(m => m.TeamId == project.TeamId && m.UserId == userId);

        if (alreadyMember)
        {
            return true;
        }

        _context.TeamMembers.Add(new Models.TeamMember
        {
            Id = Guid.NewGuid(),
            TeamId = project.TeamId,
            UserId = userId,
            Role = "Member",
            JoinedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        return true;
    }
}
