namespace TaskManagement.File.Models;

public class Attachment
{
    public int Id { get; set; }

    public Guid TaskId { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string BlobName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long FileSize { get; set; }

    public Guid UploadedBy { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
