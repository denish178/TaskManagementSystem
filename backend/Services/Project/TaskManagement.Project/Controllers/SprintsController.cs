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
[Route("api/sprints")]
public class SprintsController : ControllerBase
{
    private readonly ProjectDbContext _context;
    private readonly ProjectAccessService _access;

    public SprintsController(
        ProjectDbContext context,
        ProjectAccessService access)
    {
        _context = context;
        _access = access;
    }

    private async Task<IActionResult?> RequireProjectAccessAsync(Guid projectId)
    {
        var userId = ProjectAccessService.GetUserId(User);
        var isAdmin = ProjectAccessService.IsAdmin(User);

        if (!await _access.CanAccessProjectAsync(userId, projectId, isAdmin))
        {
            return Forbid();
        }

        return null;
    }

    [HttpPost]
    public async Task<IActionResult> CreateSprint(
        CreateSprintRequest request)
    {
        if (request.ProjectId == Guid.Empty)
        {
            return BadRequest(new
            {
                message = "ProjectId is required."
            });
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new
            {
                message = "Sprint name is required."
            });
        }

        if (request.EndDate < request.StartDate)
        {
            return BadRequest(new
            {
                message = "End date cannot be before start date."
            });
        }

        var projectExists = await _context.Projects
            .AnyAsync(x => x.Id == request.ProjectId);

        if (!projectExists)
        {
            return BadRequest(new
            {
                message = "Specified project does not exist."
            });
        }

        var denied = await RequireProjectAccessAsync(request.ProjectId);
        if (denied != null)
        {
            return denied;
        }

        var sprint = new Sprint
        {
            Id = Guid.NewGuid(),
            ProjectId = request.ProjectId,
            Name = request.Name.Trim(),
            Goal = request.Goal,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            CreatedAt = DateTime.UtcNow
        };

        _context.Sprints.Add(sprint);

        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetSprint),
            new { sprintId = sprint.Id },
            sprint);
    }

    [HttpGet("project/{projectId:guid}")]
    public async Task<IActionResult> GetProjectSprints(
        Guid projectId)
    {
        var projectExists = await _context.Projects
            .AnyAsync(x => x.Id == projectId);

        if (!projectExists)
        {
            return NotFound(new
            {
                message = "Project not found."
            });
        }

        var denied = await RequireProjectAccessAsync(projectId);
        if (denied != null)
        {
            return denied;
        }

        var sprints = await _context.Sprints
            .AsNoTracking()
            .Where(x => x.ProjectId == projectId)
            .OrderBy(x => x.StartDate)
            .ToListAsync();

        return Ok(sprints);
    }

    [HttpGet("{sprintId:guid}")]
    public async Task<IActionResult> GetSprint(
        Guid sprintId)
    {
        var sprint = await _context.Sprints
            .AsNoTracking()
            .Include(x => x.Project)
            .FirstOrDefaultAsync(x => x.Id == sprintId);

        if (sprint == null)
        {
            return NotFound(new
            {
                message = "Sprint not found."
            });
        }

        var denied = await RequireProjectAccessAsync(sprint.ProjectId);
        if (denied != null)
        {
            return denied;
        }

        return Ok(sprint);
    }

    [HttpPut("{sprintId:guid}")]
    public async Task<IActionResult> UpdateSprint(
        Guid sprintId,
        UpdateSprintRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new
            {
                message = "Sprint name is required."
            });
        }

        if (request.EndDate < request.StartDate)
        {
            return BadRequest(new
            {
                message = "End date cannot be before start date."
            });
        }

        var sprint = await _context.Sprints
            .FirstOrDefaultAsync(x => x.Id == sprintId);

        if (sprint == null)
        {
            return NotFound(new
            {
                message = "Sprint not found."
            });
        }

        var denied = await RequireProjectAccessAsync(sprint.ProjectId);
        if (denied != null)
        {
            return denied;
        }

        sprint.Name = request.Name.Trim();
        sprint.Goal = request.Goal;
        sprint.StartDate = request.StartDate;
        sprint.EndDate = request.EndDate;

        await _context.SaveChangesAsync();

        return Ok(sprint);
    }

    [HttpDelete("{sprintId:guid}")]
    public async Task<IActionResult> DeleteSprint(
        Guid sprintId)
    {
        var sprint = await _context.Sprints
            .FirstOrDefaultAsync(x => x.Id == sprintId);

        if (sprint == null)
        {
            return NotFound(new
            {
                message = "Sprint not found."
            });
        }

        var denied = await RequireProjectAccessAsync(sprint.ProjectId);
        if (denied != null)
        {
            return denied;
        }

        _context.Sprints.Remove(sprint);

        await _context.SaveChangesAsync();

        return NoContent();
    }
}