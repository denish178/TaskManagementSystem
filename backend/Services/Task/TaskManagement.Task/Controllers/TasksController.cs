using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TaskManagement.Task.Data;
using TaskManagement.Task.DTOs;
using TaskManagement.Task.Models;

namespace TaskManagement.Task.Controllers;

[ApiController]
[Route("api/tasks")]
[Authorize]
public class TasksController : ControllerBase
{
    private readonly TaskDbContext _context;

    private static readonly string[] AllowedStatuses =
    {
        "Todo",
        "InProgress",
        "Testing",
        "Done"
    };

    public TasksController(TaskDbContext context)
    {
        _context = context;
    }

    // =========================================================
    // CREATE TASK
    // POST /api/tasks
    // =========================================================

    [HttpPost]
    public async Task<IActionResult> CreateTask(
        CreateTaskRequest request)
    {
        if (request.ProjectId == Guid.Empty)
        {
            return BadRequest(new
            {
                message = "ProjectId is required."
            });
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest(new
            {
                message = "Task title is required."
            });
        }

        var title = request.Title.Trim();

        if (title.Length > 250)
        {
            return BadRequest(new
            {
                message = "Task title cannot exceed 250 characters."
            });
        }

        if (request.Description?.Length > 5000)
        {
            return BadRequest(new
            {
                message = "Task description cannot exceed 5000 characters."
            });
        }

        var userId = GetCurrentUserId();
        var now = DateTime.UtcNow;

        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            ProjectId = request.ProjectId,
            SprintId = request.SprintId,
            Title = title,

            Description = string.IsNullOrWhiteSpace(request.Description)
                ? null
                : request.Description.Trim(),

            Priority = string.IsNullOrWhiteSpace(request.Priority)
                ? "Medium"
                : request.Priority.Trim(),

            Status = "Todo",
            AssigneeId = null,
            CreatedBy = userId,
            DueDate = request.DueDate,
            CreatedAt = now,
            UpdatedAt = now
        };

        _context.Tasks.Add(task);

        await _context.SaveChangesAsync();

        return CreatedAtAction(
    nameof(GetTask),
    new { taskId = task.Id },
    new
    {
        task.Id,
        task.ProjectId,
        task.SprintId,
        task.Title,
        task.Description,
        task.Status,
        task.Priority,
        task.AssigneeId,
        task.CreatedBy,
        task.DueDate,
        task.CreatedAt,
        task.UpdatedAt
    });
    }

    // =========================================================
    // GET ALL TASKS
    // GET /api/tasks
    //
    // Optional filters:
    // ?projectId=
    // ?sprintId=
    // ?status=
    // ?priority=
    // ?assigneeId=
    // =========================================================

    [HttpGet]
    public async Task<IActionResult> GetTasks(
        [FromQuery] Guid? projectId,
        [FromQuery] Guid? sprintId,
        [FromQuery] string? status,
        [FromQuery] string? priority,
        [FromQuery] Guid? assigneeId)
    {
        IQueryable<TaskItem> query = _context.Tasks
            .AsNoTracking();

        if (projectId.HasValue)
        {
            query = query.Where(x =>
                x.ProjectId == projectId.Value);
        }

        if (sprintId.HasValue)
        {
            query = query.Where(x =>
                x.SprintId == sprintId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalizedStatus = NormalizeStatus(status);

            if (normalizedStatus == null)
            {
                return BadRequest(new
                {
                    message =
                        "Invalid status. Allowed values: Todo, InProgress, Testing, Done."
                });
            }

            query = query.Where(x =>
                x.Status == normalizedStatus);
        }

        if (!string.IsNullOrWhiteSpace(priority))
        {
            query = query.Where(x =>
                x.Priority == priority.Trim());
        }

        if (assigneeId.HasValue)
        {
            query = query.Where(x =>
                x.AssigneeId == assigneeId.Value);
        }

        var tasks = await query
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.Id,
                x.ProjectId,
                x.SprintId,
                x.Title,
                x.Description,
                x.Status,
                x.Priority,
                x.AssigneeId,
                x.CreatedBy,
                x.DueDate,
                x.CreatedAt,
                x.UpdatedAt,

                SubTaskCount = x.SubTasks.Count,

                CompletedSubTaskCount =
                    x.SubTasks.Count(s => s.IsCompleted)
            })
            .ToListAsync();

        return Ok(tasks);
    }

    // =========================================================
    // GET SINGLE TASK
    // GET /api/tasks/{taskId}
    // =========================================================

    [HttpGet("{taskId:guid}")]
    public async Task<IActionResult> GetTask(
        Guid taskId)
    {
        var task = await _context.Tasks
            .AsNoTracking()
            .Where(x => x.Id == taskId)
            .Select(x => new
            {
                x.Id,
                x.ProjectId,
                x.SprintId,
                x.Title,
                x.Description,
                x.Status,
                x.Priority,
                x.AssigneeId,
                x.CreatedBy,
                x.DueDate,
                x.CreatedAt,
                x.UpdatedAt,

                SubTasks = x.SubTasks
                    .OrderBy(s => s.CreatedAt)
                    .Select(s => new
                    {
                        s.Id,
                        s.TaskId,
                        s.Title,
                        s.IsCompleted,
                        s.CreatedAt
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();

        if (task == null)
        {
            return NotFound(new
            {
                message = "Task not found."
            });
        }

        return Ok(task);
    }

    // =========================================================
    // UPDATE TASK
    // PUT /api/tasks/{taskId}
    // =========================================================

    [HttpPut("{taskId:guid}")]
    public async Task<IActionResult> UpdateTask(
        Guid taskId,
        UpdateTaskRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest(new
            {
                message = "Task title is required."
            });
        }

        var title = request.Title.Trim();

        if (title.Length > 250)
        {
            return BadRequest(new
            {
                message = "Task title cannot exceed 250 characters."
            });
        }

        if (request.Description?.Length > 5000)
        {
            return BadRequest(new
            {
                message = "Task description cannot exceed 5000 characters."
            });
        }

        var task = await _context.Tasks
            .FirstOrDefaultAsync(x => x.Id == taskId);

        if (task == null)
        {
            return NotFound(new
            {
                message = "Task not found."
            });
        }

        task.Title = title;

        task.Description =
            string.IsNullOrWhiteSpace(request.Description)
                ? null
                : request.Description.Trim();

        task.SprintId = request.SprintId;

        if (!string.IsNullOrWhiteSpace(request.Priority))
        {
            task.Priority = request.Priority.Trim();
        }

        task.DueDate = request.DueDate;
        task.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            task.Id,
            task.ProjectId,
            task.SprintId,
            task.Title,
            task.Description,
            task.Status,
            task.Priority,
            task.AssigneeId,
            task.CreatedBy,
            task.DueDate,
            task.CreatedAt,
            task.UpdatedAt
        });
    }

    // =========================================================
    // DELETE TASK
    // DELETE /api/tasks/{taskId}
    // =========================================================

    [HttpDelete("{taskId:guid}")]
    public async Task<IActionResult> DeleteTask(
        Guid taskId)
    {
        var task = await _context.Tasks
            .FirstOrDefaultAsync(x => x.Id == taskId);

        if (task == null)
        {
            return NotFound(new
            {
                message = "Task not found."
            });
        }

        _context.Tasks.Remove(task);

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // =========================================================
    // UPDATE STATUS
    // PATCH /api/tasks/{taskId}/status
    // =========================================================

    [HttpPatch("{taskId:guid}/status")]
    public async Task<IActionResult> UpdateStatus(
        Guid taskId,
        UpdateTaskStatusRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Status))
        {
            return BadRequest(new
            {
                message = "Status is required."
            });
        }

        var status = NormalizeStatus(request.Status);

        if (status == null)
        {
            return BadRequest(new
            {
                message =
                    "Invalid status. Allowed values: Todo, InProgress, Testing, Done."
            });
        }

        var task = await _context.Tasks
            .FirstOrDefaultAsync(x => x.Id == taskId);

        if (task == null)
        {
            return NotFound(new
            {
                message = "Task not found."
            });
        }

        task.Status = status;
        task.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            task.Id,
            task.ProjectId,
            task.SprintId,
            task.Title,
            task.Description,
            task.Status,
            task.Priority,
            task.AssigneeId,
            task.CreatedBy,
            task.DueDate,
            task.CreatedAt,
            task.UpdatedAt
        });
    }

    // =========================================================
    // ASSIGN TASK
    // PATCH /api/tasks/{taskId}/assign
    //
    // ONLY ADMIN CAN ASSIGN TASKS
    // =========================================================

    [Authorize(Roles = "Admin")]
    [HttpPatch("{taskId:guid}/assign")]
    public async Task<IActionResult> AssignTask(
        Guid taskId,
        AssignTaskRequest request)
    {
        if (request.AssigneeId == Guid.Empty)
        {
            return BadRequest(new
            {
                message = "AssigneeId is required."
            });
        }

        var task = await _context.Tasks
            .FirstOrDefaultAsync(x => x.Id == taskId);

        if (task == null)
        {
            return NotFound(new
            {
                message = "Task not found."
            });
        }

        task.AssigneeId = request.AssigneeId;
        task.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            task.Id,
            task.ProjectId,
            task.SprintId,
            task.Title,
            task.Description,
            task.Status,
            task.Priority,
            task.AssigneeId,
            task.CreatedBy,
            task.DueDate,
            task.CreatedAt,
            task.UpdatedAt
        });
    }

    // =========================================================
    // PROJECT DASHBOARD DATA
    // GET /api/tasks/project/{projectId}/dashboard-data
    //
    // Kept temporarily.
    // We will inspect Dashboard Service before deciding
    // whether these endpoints should be removed.
    // =========================================================

    [HttpGet("project/{projectId:guid}/dashboard-data")]
    public async Task<IActionResult> GetProjectDashboardData(
        Guid projectId)
    {
        var totalTasks = await _context.Tasks
            .CountAsync(x => x.ProjectId == projectId);

        var todo = await _context.Tasks
            .CountAsync(x =>
                x.ProjectId == projectId &&
                x.Status == "Todo");

        var inProgress = await _context.Tasks
            .CountAsync(x =>
                x.ProjectId == projectId &&
                x.Status == "InProgress");

        var testing = await _context.Tasks
            .CountAsync(x =>
                x.ProjectId == projectId &&
                x.Status == "Testing");

        var done = await _context.Tasks
            .CountAsync(x =>
                x.ProjectId == projectId &&
                x.Status == "Done");

        var overdueTasks = await _context.Tasks
            .CountAsync(x =>
                x.ProjectId == projectId &&
                x.DueDate.HasValue &&
                x.DueDate.Value < DateTime.UtcNow &&
                x.Status != "Done");

        return Ok(new
        {
            projectId,
            totalTasks,

            statusCounts = new
            {
                todo,
                inProgress,
                testing,
                done
            },

            overdueTasks
        });
    }

    // =========================================================
    // SPRINT DASHBOARD DATA
    // GET /api/tasks/sprint/{sprintId}/dashboard-data
    //
    // Kept temporarily.
    // We will inspect Dashboard Service before deciding
    // whether these endpoints should be removed.
    // =========================================================

    [HttpGet("sprint/{sprintId:guid}/dashboard-data")]
    public async Task<IActionResult> GetSprintDashboardData(
        Guid sprintId)
    {
        var totalTasks = await _context.Tasks
            .CountAsync(x => x.SprintId == sprintId);

        var completedTasks = await _context.Tasks
            .CountAsync(x =>
                x.SprintId == sprintId &&
                x.Status == "Done");

        var todo = await _context.Tasks
            .CountAsync(x =>
                x.SprintId == sprintId &&
                x.Status == "Todo");

        var inProgress = await _context.Tasks
            .CountAsync(x =>
                x.SprintId == sprintId &&
                x.Status == "InProgress");

        var testing = await _context.Tasks
            .CountAsync(x =>
                x.SprintId == sprintId &&
                x.Status == "Testing");

        var progressPercentage = totalTasks == 0
            ? 0
            : Math.Round(
                (double)completedTasks / totalTasks * 100,
                2);

        return Ok(new
        {
            sprintId,
            totalTasks,
            completedTasks,
            todo,
            inProgress,
            testing,
            done = completedTasks,
            progressPercentage
        });
    }

    // =========================================================
    // HELPERS
    // =========================================================

    private Guid GetCurrentUserId()
    {
        var userId =
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (!Guid.TryParse(userId, out var parsedUserId))
        {
            throw new UnauthorizedAccessException(
                "User ID claim is missing or invalid.");
        }

        return parsedUserId;
    }

    private static string? NormalizeStatus(string status)
    {
        return AllowedStatuses.FirstOrDefault(
            x => x.Equals(
                status.Trim(),
                StringComparison.OrdinalIgnoreCase));
    }
}