using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Project.Data;
using TaskManagement.Project.DTOs;
using TaskManagement.Project.Models;

namespace TaskManagement.Project.Controllers;

[ApiController]
[Route("api/teams")]
public class TeamsController : ControllerBase
{
    private readonly ProjectDbContext _context;

    public TeamsController(ProjectDbContext context)
    {
        _context = context;
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

        var team = new Team
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Description = request.Description,
            CreatedBy = request.CreatedBy,
            CreatedAt = DateTime.UtcNow
        };

        _context.Teams.Add(team);

        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetTeam),
            new { teamId = team.Id },
            team);
    }

    [HttpGet]
    public async Task<IActionResult> GetTeams()
    {
        var teams = await _context.Teams
            .AsNoTracking()
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

        _context.TeamMembers.Remove(member);

        await _context.SaveChangesAsync();

        return NoContent();
    }
}