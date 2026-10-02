namespace TaskManagement.File.Models;

public class Attachment
{
    public int Id { get; set; }

    public int TaskId { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string BlobName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long FileSize { get; set; }

    public int UploadedBy { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}