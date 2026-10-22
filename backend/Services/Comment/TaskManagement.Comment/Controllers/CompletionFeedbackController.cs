using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.Comment.DTOs;
using TaskManagement.Comment.Models;
using TaskManagement.Comment.Repositories;
using TaskManagement.Comment.Services;

namespace TaskManagement.Comment.Controllers;

[ApiController]
[Route("api/comments/task/{taskId:guid}/completion-feedback")]
[Authorize]
public class CompletionFeedbackController : ControllerBase
{
    private readonly ICompletionFeedbackRepository _repository;
    private readonly ITaskFeedbackPolicyClient _taskFeedbackPolicy;

    public CompletionFeedbackController(
        ICompletionFeedbackRepository repository,
        ITaskFeedbackPolicyClient taskFeedbackPolicy)
    {
        _repository = repository;
        _taskFeedbackPolicy = taskFeedbackPolicy;
    }

    [HttpGet]
    public async Task<IActionResult> Get(Guid taskId)
    {
        var userId = GetUserId();
        var isAdmin = User.IsInRole("Admin");
        var task = await _taskFeedbackPolicy.GetTaskContextAsync(taskId);
        if (task == null)
        {
            return NotFound(new { message = "Task not found." });
        }

        if (!task.CanAccess)
        {
            return Forbid();
        }

        if (!string.Equals(task.Status, "Done", StringComparison.Ordinal))
        {
            return Ok(new
            {
                content = (string?)null,
                submittedAt = (DateTime?)null,
                submittedByUserId = (Guid?)null,
            });
        }

        if (!isAdmin
            && task.CompletedByUserId != userId)
        {
            return Forbid();
        }

        var stored = await _repository.GetByTaskIdAsync(taskId);
        if (stored == null)
        {
            return Ok(new
            {
                content = (string?)null,
                submittedAt = (DateTime?)null,
                submittedByUserId = (Guid?)null,
            });
        }

        return Ok(new
        {
            content = stored.Content,
            submittedAt = stored.SubmittedAt,
            submittedByUserId = stored.SubmittedByUserId,
        });
    }

    [HttpPut]
    public async Task<IActionResult> Upsert(
        Guid taskId,
        SubmitCompletionFeedbackRequest request)
    {
        if (User.IsInRole("Admin"))
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return BadRequest(new { message = "Feedback content is required." });
        }

        var content = request.Content.Trim();
        if (content.Length > 4000)
        {
            return BadRequest(new { message = "Feedback cannot exceed 4000 characters." });
        }

        var userId = GetUserId();
        var task = await _taskFeedbackPolicy.GetTaskContextAsync(taskId);
        if (task == null)
        {
            return NotFound(new { message = "Task not found." });
        }

        if (!task.CanAccess)
        {
            return Forbid();
        }

        if (!string.Equals(task.Status, "Done", StringComparison.Ordinal))
        {
            return BadRequest(new
            {
                message = "Completion feedback is only available when the task is Done.",
            });
        }

        if (task.CompletedByUserId != userId)
        {
            return Forbid();
        }

        var saved = await _repository.UpsertAsync(new TaskCompletionFeedback
        {
            TaskId = taskId,
            SubmittedByUserId = userId,
            Content = content,
            SubmittedAt = DateTime.UtcNow,
        });

        return Ok(new
        {
            content = saved.Content,
            submittedAt = saved.SubmittedAt,
            submittedByUserId = saved.SubmittedByUserId,
        });
    }

    private Guid GetUserId()
    {
        var userIdClaim =
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("User ID claim is missing or invalid.");
        }

        return userId;
    }
}
