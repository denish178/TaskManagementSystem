using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Task.Data;
using TaskManagement.Task.DTOs;
using TaskManagement.Task.Models;

namespace TaskManagement.Task.Controllers;

[ApiController]
[Route("api/tasks/{taskId:guid}/subtasks")]
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

        var taskExists = await _context.Tasks
            .AnyAsync(x => x.Id == taskId);

        if (!taskExists)
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

            Title = request.Title.Trim(),

            IsCompleted = false,

            CreatedAt = DateTime.UtcNow
        };

        _context.SubTasks.Add(subTask);

        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetSubTasks),
            new { taskId },
            subTask);
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

        subTask.Title = request.Title.Trim();

        subTask.IsCompleted = request.IsCompleted;

        await _context.SaveChangesAsync();

        return Ok(subTask);
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

        await _context.SaveChangesAsync();

        return NoContent();
    }
}