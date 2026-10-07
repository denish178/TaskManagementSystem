using Microsoft.AspNetCore.Mvc;
using TaskManagement.File.Data;
using TaskManagement.File.Interfaces;
using TaskManagement.File.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace TaskManagement.File.Controllers;

[ApiController]
[Route("api/files")]
[Authorize]
public class FilesController : ControllerBase
{
    // Maximum file size allowed by the File Service.
    private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB
    // Only allow file types required by the application.
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

    public FilesController(
        IBlobStorageService blobStorageService,
        FileDbContext dbContext)
    {
        _blobStorageService = blobStorageService;
        _dbContext = dbContext;
    }
[HttpGet("{id:int}")]
public async Task<IActionResult> GetById(int id)
{
    var attachment = await _dbContext.Attachments.FindAsync(id);

    if (attachment == null)
    {
        return NotFound("Attachment not found.");
    }

    return Ok(attachment);
}



    [HttpPost("upload")]
    public async Task<IActionResult> Upload(
        IFormFile file,
        [FromForm] int taskId,
        [FromForm] int uploadedBy)
    {
        // Validate that a file was provided.
        if (file == null || file.Length == 0)
        {
            return BadRequest("File is required.");
        }
        // Reject files larger than the configured limit.
        if (file.Length > MaxFileSize)
{
    return BadRequest("File size cannot exceed 10 MB.");
}
// Validate the file extension before sending it to storage.
var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

if (!AllowedExtensions.Contains(extension))
{
    return BadRequest(
        $"File type '{extension}' is not allowed.");
}

        string? blobName = null;

try
{
    // 1. Upload file to Blob Storage
    blobName = await _blobStorageService.UploadAsync(
        file.OpenReadStream(),
        file.FileName,
        file.ContentType);

    // 2. Create metadata
    var attachment = new Attachment
    {
        TaskId = taskId,
        FileName = file.FileName,
        BlobName = blobName,
        ContentType = file.ContentType,
        FileSize = file.Length,
        UploadedBy = uploadedBy,
        UploadedAt = DateTime.UtcNow
    };

    // 3. Save metadata to SQL Server
    _dbContext.Attachments.Add(attachment);

    await _dbContext.SaveChangesAsync();

    return Ok(attachment);
}
catch (Exception)
{
    // If Blob upload succeeded but SQL failed,
    // remove the Blob to prevent orphaned files.
    if (!string.IsNullOrEmpty(blobName))
    {
        await _blobStorageService.DeleteAsync(blobName);
    }

    return StatusCode(
        StatusCodes.Status500InternalServerError,
        "An error occurred while uploading the file.");
}
    }
[HttpGet("task/{taskId:int}")]
public async Task<IActionResult> GetByTask(int taskId)
{
    // Return all attachments belonging to the specified task.
    var attachments = await _dbContext.Attachments
        .Where(a => a.TaskId == taskId)
        .OrderByDescending(a => a.UploadedAt)
        .ToListAsync();

    return Ok(attachments);
}
[HttpGet("{id:int}/download")]
public async Task<IActionResult> Download(int id)
{
    // First read the metadata to find the corresponding blob name.
    var attachment = await _dbContext.Attachments.FindAsync(id);

    if (attachment == null)
    {
        return NotFound("Attachment not found.");
    }
    // Stream the actual file from Azure Blob Storage to the client.
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
    // Find the metadata record before deleting the stored file.
    var attachment = await _dbContext.Attachments.FindAsync(id);

    if (attachment == null)
    {
        return NotFound("Attachment not found.");
    }
    // Delete the physical file from Azure Blob Storage first.

    await _blobStorageService.DeleteAsync(
        attachment.BlobName);

// Remove the corresponding metadata from SQL Server.
    _dbContext.Attachments.Remove(attachment);

    await _dbContext.SaveChangesAsync();

    return NoContent();
}
}