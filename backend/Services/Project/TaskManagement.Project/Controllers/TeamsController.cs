using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Project.Data;
using TaskManagement.Project.DTOs;
using TaskManagement.Project.Models;
using TaskManagement.Project.Services;

namespace TaskManagement.Project.Controllers;

[Authorize]
[ApiController]
[Route("api/teams")]
public class TeamsController : ControllerBase
{
    private readonly ProjectDbContext _context;
    private readonly ProjectAccessService _access;

    public TeamsController(
        ProjectDbContext context,
        ProjectAccessService access)
    {
        _context = context;
        _access = access;
    }

    [HttpPost]
    public async Task<IActionResult> CreateTeam(
        CreateTeamRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new
            {
                message = "Team name is required."
            });
        }

        var userId = ProjectAccessService.GetUserId(User);

        var team = new Team
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Description = request.Description,
            CreatedBy = request.CreatedBy == Guid.Empty ? userId : request.CreatedBy,
            CreatedAt = DateTime.UtcNow
        };

        _context.Teams.Add(team);

        _context.TeamMembers.Add(new TeamMember
        {
            Id = Guid.NewGuid(),
            TeamId = team.Id,
            UserId = userId,
            Role = "Owner",
            JoinedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetTeam),
            new { teamId = team.Id },
            team);
    }

    [HttpGet]
    public async Task<IActionResult> GetTeams()
    {
        var userId = ProjectAccessService.GetUserId(User);
        var isAdmin = ProjectAccessService.IsAdmin(User);
        var accessibleTeamIds = await _access.GetAccessibleTeamIdsAsync(userId, isAdmin);

        var teams = await _context.Teams
            .AsNoTracking()
            .Where(team => accessibleTeamIds.Contains(team.Id))
            .Select(team => new
            {
                team.Id,
                team.Name,
                team.Description,
                team.CreatedBy,
                team.CreatedAt,
                MemberCount = team.Members.Count,
                ProjectCount = team.Projects.Count
            })
            .ToListAsync();

        return Ok(teams);
    }

    [HttpGet("{teamId:guid}")]
    public async Task<IActionResult> GetTeam(Guid teamId)
    {
        var team = await _context.Teams
            .AsNoTracking()
            .Include(x => x.Members)
            .Include(x => x.Projects)
            .FirstOrDefaultAsync(x => x.Id == teamId);

        if (team == null)
        {
            return NotFound(new
            {
                message = "Team not found."
            });
        }

        var userId = ProjectAccessService.GetUserId(User);
        var isAdmin = ProjectAccessService.IsAdmin(User);

        if (!await _access.CanAccessTeamAsync(userId, teamId, isAdmin))
        {
            return Forbid();
        }

        return Ok(team);
    }

    [HttpPut("{teamId:guid}")]
    public async Task<IActionResult> UpdateTeam(
        Guid teamId,
        UpdateTeamRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new
            {
                message = "Team name is required."
            });
        }

        var team = await _context.Teams
            .FirstOrDefaultAsync(x => x.Id == teamId);

        if (team == null)
        {
            return NotFound(new
            {
                message = "Team not found."
            });
        }

        var userId = ProjectAccessService.GetUserId(User);
        var isAdmin = ProjectAccessService.IsAdmin(User);

        if (!await _access.CanAccessTeamAsync(userId, teamId, isAdmin))
        {
            return Forbid();
        }

        team.Name = request.Name.Trim();
        team.Description = request.Description;

        await _context.SaveChangesAsync();

        return Ok(team);
    }

    [HttpDelete("{teamId:guid}")]
    public async Task<IActionResult> DeleteTeam(Guid teamId)
    {
        var team = await _context.Teams
            .FirstOrDefaultAsync(x => x.Id == teamId);

        if (team == null)
        {
            return NotFound(new
            {
                message = "Team not found."
            });
        }

        var userId = ProjectAccessService.GetUserId(User);
        var isAdmin = ProjectAccessService.IsAdmin(User);

        if (!await _access.CanAccessTeamAsync(userId, teamId, isAdmin))
        {
            return Forbid();
        }

        _context.Teams.Remove(team);

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("{teamId:guid}/members")]
    public async Task<IActionResult> AddMember(
        Guid teamId,
        AddTeamMemberRequest request)
    {
        var teamExists = await _context.Teams
            .AnyAsync(x => x.Id == teamId);

        if (!teamExists)
        {
            return NotFound(new
            {
                message = "Team not found."
            });
        }

        var userId = ProjectAccessService.GetUserId(User);
        var isAdmin = ProjectAccessService.IsAdmin(User);

        if (!await _access.CanAccessTeamAsync(userId, teamId, isAdmin))
        {
            return Forbid();
        }

        if (request.UserId == Guid.Empty)
        {
            return BadRequest(new
            {
                message = "UserId is required."
            });
        }

        var alreadyMember = await _context.TeamMembers
            .AnyAsync(x =>
                x.TeamId == teamId &&
                x.UserId == request.UserId);

        if (alreadyMember)
        {
            return Conflict(new
            {
                message = "User is already a team member."
            });
        }

        var member = new TeamMember
        {
            Id = Guid.NewGuid(),
            TeamId = teamId,
            UserId = request.UserId,
            Role = string.IsNullOrWhiteSpace(request.Role)
                ? "Member"
                : request.Role.Trim(),
            JoinedAt = DateTime.UtcNow
        };

        _context.TeamMembers.Add(member);

        await _context.SaveChangesAsync();

        return Ok(member);
    }

    [HttpGet("{teamId:guid}/members")]
    public async Task<IActionResult> GetMembers(Guid teamId)
    {
        var teamExists = await _context.Teams
            .AnyAsync(x => x.Id == teamId);

        if (!teamExists)
        {
            return NotFound(new
            {
                message = "Team not found."
            });
        }

        var currentUserId = ProjectAccessService.GetUserId(User);
        var isAdmin = ProjectAccessService.IsAdmin(User);

        if (!await _access.CanAccessTeamAsync(currentUserId, teamId, isAdmin))
        {
            return Forbid();
        }

        var members = await _context.TeamMembers
            .AsNoTracking()
            .Where(x => x.TeamId == teamId)
            .ToListAsync();

        return Ok(members);
    }

    [HttpDelete("{teamId:guid}/members/{userId:guid}")]
    public async Task<IActionResult> RemoveMember(
        Guid teamId,
        Guid userId)
    {
        var member = await _context.TeamMembers
            .FirstOrDefaultAsync(x =>
                x.TeamId == teamId &&
                x.UserId == userId);

        if (member == null)
        {
            return NotFound(new
            {
                message = "Team member not found."
            });
        }

        var currentUserId = ProjectAccessService.GetUserId(User);
        var isAdmin = ProjectAccessService.IsAdmin(User);

        if (!await _access.CanAccessTeamAsync(currentUserId, teamId, isAdmin))
        {
            return Forbid();
        }

        _context.TeamMembers.Remove(member);

        await _context.SaveChangesAsync();

        return NoContent();
    }
}