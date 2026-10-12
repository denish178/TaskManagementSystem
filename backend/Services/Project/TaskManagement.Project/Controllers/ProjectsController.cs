using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Project.Data;
using TaskManagement.Project.DTOs;
using TaskManagement.Project.Models;
using ProjectModel = TaskManagement.Project.Models.Project;
using Microsoft.AspNetCore.Authorization;
using TaskManagement.Project.Services;

namespace TaskManagement.Project.Controllers;

[Authorize]
[ApiController]
[Route("api/projects")]
public class ProjectsController : ControllerBase
{
    private readonly ProjectDbContext _context;
    private readonly ProjectAccessService _access;

    public ProjectsController(
        ProjectDbContext context,
        ProjectAccessService access)
    {
        _context = context;
        _access = access;
    }

    [HttpGet("accessible-ids")]
    public async Task<IActionResult> GetAccessibleProjectIds()
    {
        var userId = ProjectAccessService.GetUserId(User);
        var isAdmin = ProjectAccessService.IsAdmin(User);
        var ids = await _access.GetAccessibleProjectIdsAsync(userId, isAdmin);
        return Ok(ids);
    }

    [HttpGet("{projectId:guid}/access")]
    public async Task<IActionResult> CheckProjectAccess(Guid projectId)
    {
        var exists = await _context.Projects
            .AsNoTracking()
            .AnyAsync(p => p.Id == projectId);

        if (!exists)
        {
            return NotFound(new { message = "Project not found." });
        }

        var userId = ProjectAccessService.GetUserId(User);
        var isAdmin = ProjectAccessService.IsAdmin(User);
        var allowed = await _access.CanAccessProjectAsync(userId, projectId, isAdmin);

        if (!allowed)
        {
            return Forbid();
        }

        return Ok(new { allowed = true });
    }

    [HttpPost]
    public async Task<IActionResult> CreateProject(
        CreateProjectRequest request)
    {
        if (request.TeamId == Guid.Empty)
        {
            return BadRequest(new
            {
                message = "TeamId is required."
            });
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new
            {
                message = "Project name is required."
            });
        }

        var teamExists = await _context.Teams
            .AnyAsync(x => x.Id == request.TeamId);

        if (!teamExists)
        {
            return BadRequest(new
            {
                message = "Specified team does not exist."
            });
        }

        var userId = ProjectAccessService.GetUserId(User);
        var isAdmin = ProjectAccessService.IsAdmin(User);

        if (!await _access.CanAccessTeamAsync(userId, request.TeamId, isAdmin))
        {
            return Forbid();
        }

        var project = new ProjectModel
        {
            Id = Guid.NewGuid(),
            TeamId = request.TeamId,
            Name = request.Name.Trim(),
            Description = request.Description,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Projects.Add(project);

        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetProject),
            new { projectId = project.Id },
            project);
    }

    [HttpGet]
    public async Task<IActionResult> GetProjects()
    {
        var userId = ProjectAccessService.GetUserId(User);
        var isAdmin = ProjectAccessService.IsAdmin(User);
        var accessibleIds = await _access.GetAccessibleProjectIdsAsync(userId, isAdmin);

        var projects = await _context.Projects
            .AsNoTracking()
            .Where(project => accessibleIds.Contains(project.Id))
            .Select(project => new
            {
                project.Id,
                project.TeamId,
                project.Name,
                project.Description,
                project.CreatedBy,
                project.CreatedAt,
                SprintCount = project.Sprints.Count
            })
            .ToListAsync();

        return Ok(projects);
    }

    [HttpGet("{projectId:guid}")]
    public async Task<IActionResult> GetProject(
        Guid projectId)
    {
        var project = await _context.Projects
            .AsNoTracking()
            .Include(x => x.Team)
            .Include(x => x.Sprints)
            .FirstOrDefaultAsync(x => x.Id == projectId);

        if (project == null)
        {
            return NotFound(new
            {
                message = "Project not found."
            });
        }

        var userId = ProjectAccessService.GetUserId(User);
        var isAdmin = ProjectAccessService.IsAdmin(User);

        if (!await _access.CanAccessProjectAsync(userId, projectId, isAdmin))
        {
            return Forbid();
        }

        return Ok(project);
    }

    [HttpPut("{projectId:guid}")]
    public async Task<IActionResult> UpdateProject(
        Guid projectId,
        UpdateProjectRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new
            {
                message = "Project name is required."
            });
        }

        var project = await _context.Projects
            .FirstOrDefaultAsync(x => x.Id == projectId);

        if (project == null)
        {
            return NotFound(new
            {
                message = "Project not found."
            });
        }

        var userId = ProjectAccessService.GetUserId(User);
        var isAdmin = ProjectAccessService.IsAdmin(User);

        if (!await _access.CanAccessProjectAsync(userId, projectId, isAdmin))
        {
            return Forbid();
        }

        project.Name = request.Name.Trim();
        project.Description = request.Description;

        await _context.SaveChangesAsync();

        return Ok(project);
    }

    [HttpDelete("{projectId:guid}")]
    public async Task<IActionResult> DeleteProject(
        Guid projectId)
    {
        var project = await _context.Projects
            .FirstOrDefaultAsync(x => x.Id == projectId);

        if (project == null)
        {
            return NotFound(new
            {
                message = "Project not found."
            });
        }

        var userId = ProjectAccessService.GetUserId(User);
        var isAdmin = ProjectAccessService.IsAdmin(User);

        if (!await _access.CanAccessProjectAsync(userId, projectId, isAdmin))
        {
            return Forbid();
        }

        _context.Projects.Remove(project);

        await _context.SaveChangesAsync();

        return NoContent();
    }
}