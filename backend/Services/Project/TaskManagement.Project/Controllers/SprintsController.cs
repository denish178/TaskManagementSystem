using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Project.Data;
using TaskManagement.Project.DTOs;
using TaskManagement.Project.Models;

namespace TaskManagement.Project.Controllers;

[ApiController]
[Route("api/sprints")]
public class SprintsController : ControllerBase
{
    private readonly ProjectDbContext _context;

    public SprintsController(ProjectDbContext context)
    {
        _context = context;
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

        _context.Sprints.Remove(sprint);

        await _context.SaveChangesAsync();

        return NoContent();
    }
}