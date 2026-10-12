using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Project.Data;

namespace TaskManagement.Project.Services;

public class ProjectAccessService
{
    private readonly ProjectDbContext _context;

    public ProjectAccessService(ProjectDbContext context)
    {
        _context = context;
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

        return await CanAccessTeamAsync(userId, teamId, false);
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

        if (teamIds.Count == 0)
        {
            return [];
        }

        return await _context.Projects
            .AsNoTracking()
            .Where(p => teamIds.Contains(p.TeamId))
            .Select(p => p.Id)
            .ToListAsync();
    }
}
