namespace TaskManagement.Task.Models;

public class TaskItem
{
    public Guid Id { get; set; }

    public Guid ProjectId { get; set; }

    public Guid? SprintId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string Status { get; set; } = "Todo";

    public string Priority { get; set; } = "Medium";

    public Guid? AssigneeId { get; set; }

    public Guid CreatedBy { get; set; }

    public DateTime? DueDate { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    /// <summary>User who moved the task to Done (for completion feedback).</summary>
    public Guid? CompletedByUserId { get; set; }

    public string? CompletionFeedback { get; set; }

    public DateTime? CompletionFeedbackSubmittedAt { get; set; }

    public ICollection<SubTask> SubTasks { get; set; }
        = new List<SubTask>();
}