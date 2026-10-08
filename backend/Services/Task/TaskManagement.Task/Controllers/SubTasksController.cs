using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Task.Data;
using TaskManagement.Task.DTOs;
using TaskManagement.Task.Models;

namespace TaskManagement.Task.Controllers;

[ApiController]
[Route("api/tasks/{taskId:guid}/subtasks")]
[Authorize]
public class SubTasksController : ControllerBase
{
    private readonly TaskDbContext _context;

    public SubTasksController(TaskDbContext context)
    {
        _context = context;
    }

    // =========================================================
    // CREATE SUBTASK
    // POST /api/tasks/{taskId}/subtasks
    // =========================================================

    [HttpPost]
    public async Task<IActionResult> CreateSubTask(
        Guid taskId,
        CreateSubTaskRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest(new
            {
                message = "Subtask title is required."
            });
        }

        var title = request.Title.Trim();

        if (title.Length > 250)
        {
            return BadRequest(new
            {
                message = "Subtask title cannot exceed 250 characters."
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

        var subTask = new SubTask
        {
            Id = Guid.NewGuid(),
            TaskId = taskId,
            Title = title,
            IsCompleted = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.SubTasks.Add(subTask);

        task.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return CreatedAtAction(
    nameof(GetSubTasks),
    new { taskId },
    new
    {
        subTask.Id,
        subTask.TaskId,
        subTask.Title,
        subTask.IsCompleted,
        subTask.CreatedAt
    });
    }

    // =========================================================
    // GET SUBTASKS
    // GET /api/tasks/{taskId}/subtasks
    // =========================================================

    [HttpGet]
    public async Task<IActionResult> GetSubTasks(
        Guid taskId)
    {
        var taskExists = await _context.Tasks
            .AsNoTracking()
            .AnyAsync(x => x.Id == taskId);

        if (!taskExists)
        {
            return NotFound(new
            {
                message = "Task not found."
            });
        }

        var subTasks = await _context.SubTasks
            .AsNoTracking()
            .Where(x => x.TaskId == taskId)
            .OrderBy(x => x.CreatedAt)
            .Select(x => new
            {
                x.Id,
                x.TaskId,
                x.Title,
                x.IsCompleted,
                x.CreatedAt
            })
            .ToListAsync();

        return Ok(subTasks);
    }

    // =========================================================
    // UPDATE SUBTASK
    // PUT /api/tasks/{taskId}/subtasks/{subtaskId}
    // =========================================================

    [HttpPut("{subtaskId:guid}")]
    public async Task<IActionResult> UpdateSubTask(
        Guid taskId,
        Guid subtaskId,
        UpdateSubTaskRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest(new
            {
                message = "Subtask title is required."
            });
        }

        var title = request.Title.Trim();

        if (title.Length > 250)
        {
            return BadRequest(new
            {
                message = "Subtask title cannot exceed 250 characters."
            });
        }

        var subTask = await _context.SubTasks
            .FirstOrDefaultAsync(x =>
                x.Id == subtaskId &&
                x.TaskId == taskId);

        if (subTask == null)
        {
            return NotFound(new
            {
                message = "Subtask not found."
            });
        }

        subTask.Title = title;
        subTask.IsCompleted = request.IsCompleted;

        var task = await _context.Tasks
            .FirstOrDefaultAsync(x => x.Id == taskId);

        if (task != null)
        {
            task.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            subTask.Id,
            subTask.TaskId,
            subTask.Title,
            subTask.IsCompleted,
            subTask.CreatedAt
        });
    }

    // =========================================================
    // DELETE SUBTASK
    // DELETE /api/tasks/{taskId}/subtasks/{subtaskId}
    // =========================================================

    [HttpDelete("{subtaskId:guid}")]
    public async Task<IActionResult> DeleteSubTask(
        Guid taskId,
        Guid subtaskId)
    {
        var subTask = await _context.SubTasks
            .FirstOrDefaultAsync(x =>
                x.Id == subtaskId &&
                x.TaskId == taskId);

        if (subTask == null)
        {
            return NotFound(new
            {
                message = "Subtask not found."
            });
        }

        _context.SubTasks.Remove(subTask);

        var task = await _context.Tasks
            .FirstOrDefaultAsync(x => x.Id == taskId);

        if (task != null)
        {
            task.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return NoContent();
    }
}