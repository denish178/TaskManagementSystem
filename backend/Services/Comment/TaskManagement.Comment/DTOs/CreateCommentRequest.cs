using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Comment.DTOs;

public class CreateCommentRequest
{
    [Required]
    public Guid TaskId { get; set; }

    [Required]
    [StringLength(2000, MinimumLength = 1)]
    public string Content { get; set; } = string.Empty;
}