using Microsoft.AspNetCore.Mvc;
using TaskManagement.Comment.DTOs;
using CommentModel = TaskManagement.Comment.Models.Comment;
using TaskManagement.Comment.Repositories;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace TaskManagement.Comment.Controllers;

[ApiController]
[Route("api/comments")]
[Authorize]
public class CommentsController : ControllerBase
{
    private readonly CommentRepository _repository;

    public CommentsController(CommentRepository repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateCommentRequest request)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var comment = new CommentModel
        {
            Id = Guid.NewGuid(),
            TaskId = request.TaskId,
            UserId = userId,
            Content = request.Content,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var createdComment = await _repository.CreateAsync(comment);

        return CreatedAtAction(
            nameof(GetById),
            new { commentId = createdComment.Id },
            createdComment);
    }

    [HttpGet("{commentId:guid}")]
    public async Task<IActionResult> GetById(Guid commentId)
    {
        var comment = await _repository.GetByIdAsync(commentId);

        if (comment is null)
            return NotFound();

        return Ok(comment);
    }

    [HttpGet("task/{taskId:guid}")]
    public async Task<IActionResult> GetByTaskId(Guid taskId)
    {
        var comments = await _repository.GetByTaskIdAsync(taskId);

        return Ok(comments);
    }

    [HttpPut("{commentId:guid}")]
    public async Task<IActionResult> Update(
    Guid commentId,
    UpdateCommentRequest request)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var comment = await _repository.GetByIdAsync(commentId);

        if (comment is null)
            return NotFound();

        if (comment.UserId != userId)
            return Forbid();

        var updated = await _repository.UpdateAsync(
            commentId,
            request.Content);

        if (!updated)
            return NotFound();

        var updatedComment = await _repository.GetByIdAsync(commentId);

        return Ok(updatedComment);
    }

    [HttpDelete("{commentId:guid}")]
    public async Task<IActionResult> Delete(Guid commentId)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var comment = await _repository.GetByIdAsync(commentId);

        if (comment is null)
            return NotFound();

        if (comment.UserId != userId)
            return Forbid();

        await _repository.DeleteAsync(commentId);

        return NoContent();
    }
}