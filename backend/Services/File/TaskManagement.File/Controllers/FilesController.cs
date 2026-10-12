using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManagement.File.Data;
using TaskManagement.File.Interfaces;
using TaskManagement.File.Models;
using TaskManagement.File.Services;

namespace TaskManagement.File.Controllers;

[ApiController]
[Route("api/files")]
[Authorize]
public class FilesController : ControllerBase
{
    private const long MaxFileSize = 10 * 1024 * 1024;

    private static readonly string[] AllowedExtensions =
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".pdf",
        ".doc",
        ".docx",
        ".xls",
        ".xlsx",
        ".txt"
    };

    private readonly IBlobStorageService _blobStorageService;
    private readonly FileDbContext _dbContext;
    private readonly ITaskAccessClient _taskAccess;

    public FilesController(
        IBlobStorageService blobStorageService,
        FileDbContext dbContext,
        ITaskAccessClient taskAccess)
    {
        _blobStorageService = blobStorageService;
        _dbContext = dbContext;
        _taskAccess = taskAccess;
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var attachment = await _dbContext.Attachments.FindAsync(id);

        if (attachment == null)
        {
            return NotFound(new { message = "Attachment not found." });
        }

        if (!await _taskAccess.CanAccessTaskAsync(attachment.TaskId))
        {
            return Forbid();
        }

        return Ok(attachment);
    }

    [HttpPost("upload")]
    public async Task<IActionResult> Upload(
        IFormFile file,
        [FromForm] Guid taskId,
        [FromForm] Guid? uploadedBy)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "File is required." });
        }

        if (taskId == Guid.Empty)
        {
            return BadRequest(new { message = "TaskId is required." });
        }

        if (!await _taskAccess.CanAccessTaskAsync(taskId))
        {
            return Forbid();
        }

        if (file.Length > MaxFileSize)
        {
            return BadRequest(new { message = "File size cannot exceed 10 MB." });
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (!AllowedExtensions.Contains(extension))
        {
            return BadRequest(new
            {
                message = $"File type '{extension}' is not allowed."
            });
        }

        var userId = GetCurrentUserId();
        string? blobName = null;

        try
        {
            blobName = await _blobStorageService.UploadAsync(
                file.OpenReadStream(),
                file.FileName,
                file.ContentType);

            var attachment = new Attachment
            {
                TaskId = taskId,
                FileName = file.FileName,
                BlobName = blobName,
                ContentType = file.ContentType,
                FileSize = file.Length,
                UploadedBy = userId,
                UploadedAt = DateTime.UtcNow
            };

            _dbContext.Attachments.Add(attachment);
            await _dbContext.SaveChangesAsync();

            return Ok(attachment);
        }
        catch (Exception)
        {
            if (!string.IsNullOrEmpty(blobName))
            {
                await _blobStorageService.DeleteAsync(blobName);
            }

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while uploading the file." });
        }
    }

    [HttpGet("task/{taskId:guid}")]
    public async Task<IActionResult> GetByTask(Guid taskId)
    {
        if (!await _taskAccess.CanAccessTaskAsync(taskId))
        {
            return Forbid();
        }

        var attachments = await _dbContext.Attachments
            .Where(a => a.TaskId == taskId)
            .OrderByDescending(a => a.UploadedAt)
            .ToListAsync();

        return Ok(attachments);
    }

    [HttpGet("{id:int}/download")]
    public async Task<IActionResult> Download(int id)
    {
        var attachment = await _dbContext.Attachments.FindAsync(id);

        if (attachment == null)
        {
            return NotFound(new { message = "Attachment not found." });
        }

        if (!await _taskAccess.CanAccessTaskAsync(attachment.TaskId))
        {
            return Forbid();
        }

        var stream = await _blobStorageService
            .DownloadAsync(attachment.BlobName);

        return File(
            stream,
            attachment.ContentType,
            attachment.FileName);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var attachment = await _dbContext.Attachments.FindAsync(id);

        if (attachment == null)
        {
            return NotFound(new { message = "Attachment not found." });
        }

        if (!await _taskAccess.CanAccessTaskAsync(attachment.TaskId))
        {
            return Forbid();
        }

        var userId = GetCurrentUserId();
        if (!User.IsInRole("Admin") && attachment.UploadedBy != userId)
        {
            return Forbid();
        }

        await _blobStorageService.DeleteAsync(attachment.BlobName);

        _dbContext.Attachments.Remove(attachment);
        await _dbContext.SaveChangesAsync();

        return NoContent();
    }

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
}
