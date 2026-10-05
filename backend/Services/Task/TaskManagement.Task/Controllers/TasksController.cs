using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Task.Data;
using TaskManagement.Task.DTOs;
using TaskManagement.Task.Models;

namespace TaskManagement.Task.Controllers;

[ApiController]
[Route("api/tasks")]
public class TasksController : ControllerBase
{
    private readonly TaskDbContext _context;

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

        var task = new TaskItem
        {
            Id = Guid.NewGuid(),

            ProjectId = request.ProjectId,

            SprintId = request.SprintId,

            Title = request.Title.Trim(),

            Description = request.Description,

            Priority = string.IsNullOrWhiteSpace(request.Priority)
                ? "Medium"
                : request.Priority.Trim(),

            Status = "Todo",

            AssigneeId = null,

            CreatedBy = request.CreatedBy,

            DueDate = request.DueDate,

            CreatedAt = DateTime.UtcNow,

            UpdatedAt = DateTime.UtcNow
        };

        _context.Tasks.Add(task);

        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetTask),
            new { taskId = task.Id },
            task);
    }


    // =========================================================
    // GET ALL TASKS
    //
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
            query = query.Where(x =>
                x.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(priority))
        {
            query = query.Where(x =>
                x.Priority == priority);
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

        var task = await _context.Tasks
            .FirstOrDefaultAsync(x => x.Id == taskId);

        if (task == null)
        {
            return NotFound(new
            {
                message = "Task not found."
            });
        }

        task.Title = request.Title.Trim();

        task.Description = request.Description;

        task.SprintId = request.SprintId;

        if (!string.IsNullOrWhiteSpace(request.Priority))
        {
            task.Priority = request.Priority.Trim();
        }

        task.DueDate = request.DueDate;

        task.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(task);
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

        var allowedStatuses = new[]
        {
            "Todo",
            "InProgress",
            "Review",
            "Done"
        };

        var status = request.Status.Trim();

        if (!allowedStatuses.Contains(
                status,
                StringComparer.OrdinalIgnoreCase))
        {
            return BadRequest(new
            {
                message =
                    "Invalid status. Allowed values: Todo, InProgress, Review, Done."
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

        task.Status = allowedStatuses
            .First(x => x.Equals(
                status,
                StringComparison.OrdinalIgnoreCase));

        task.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(task);
    }


    // =========================================================
    // ASSIGN TASK
    // PATCH /api/tasks/{taskId}/assign
    //
    // Admin authorization will be added later
    // when Auth Service / JWT integration is connected.
    // =========================================================

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

        return Ok(task);
    }


    // =========================================================
    // PROJECT DASHBOARD DATA
    //
    // GET
    // /api/tasks/project/{projectId}/dashboard-data
    //
    // Response:
    //
    // {
    //   "projectId": "...",
    //   "totalTasks": 25,
    //   "statusCounts": {
    //      "todo": 8,
    //      "inProgress": 7,
    //      "testing": 5,
    //      "done": 5
    //   },
    //   "overdueTasks": 3
    // }
    // =========================================================

    [HttpGet("project/{projectId:guid}/dashboard-data")]
    public async Task<IActionResult> GetProjectDashboardData(
        Guid projectId)
    {
        var tasks = await _context.Tasks
            .AsNoTracking()
            .Where(x => x.ProjectId == projectId)
            .ToListAsync();

        var totalTasks = tasks.Count;

        var todo = tasks.Count(x =>
            x.Status.Equals(
                "Todo",
                StringComparison.OrdinalIgnoreCase));

        var inProgress = tasks.Count(x =>
            x.Status.Equals(
                "InProgress",
                StringComparison.OrdinalIgnoreCase));

        var testing = tasks.Count(x =>
            x.Status.Equals(
                "Review",
                StringComparison.OrdinalIgnoreCase));

        var done = tasks.Count(x =>
            x.Status.Equals(
                "Done",
                StringComparison.OrdinalIgnoreCase));

        var overdueTasks = tasks.Count(x =>
            x.DueDate.HasValue &&
            x.DueDate.Value < DateTime.UtcNow &&
            !x.Status.Equals(
                "Done",
                StringComparison.OrdinalIgnoreCase));

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
    //
    // GET
    // /api/tasks/sprint/{sprintId}/dashboard-data
    //
    // Response:
    //
    // {
    //   "sprintId": "...",
    //   "totalTasks": 20,
    //   "completedTasks": 12,
    //   "todo": 3,
    //   "inProgress": 3,
    //   "testing": 2,
    //   "done": 12,
    //   "progressPercentage": 60
    // }
    // =========================================================

    [HttpGet("sprint/{sprintId:guid}/dashboard-data")]
    public async Task<IActionResult> GetSprintDashboardData(
        Guid sprintId)
    {
        var tasks = await _context.Tasks
            .AsNoTracking()
            .Where(x => x.SprintId == sprintId)
            .ToListAsync();

        var totalTasks = tasks.Count;

        var completedTasks = tasks.Count(x =>
            x.Status.Equals(
                "Done",
                StringComparison.OrdinalIgnoreCase));

        var todo = tasks.Count(x =>
            x.Status.Equals(
                "Todo",
                StringComparison.OrdinalIgnoreCase));

        var inProgress = tasks.Count(x =>
            x.Status.Equals(
                "InProgress",
                StringComparison.OrdinalIgnoreCase));

        var testing = tasks.Count(x =>
            x.Status.Equals(
                "Review",
                StringComparison.OrdinalIgnoreCase));

        var done = completedTasks;

        var progressPercentage = totalTasks == 0
            ? 0
            : Math.Round(
                (double)completedTasks /
                totalTasks *
                100,
                2);

        return Ok(new
        {
            sprintId,

            totalTasks,

            completedTasks,

            todo,

            inProgress,

            testing,

            done,

            progressPercentage
        });
    }
}