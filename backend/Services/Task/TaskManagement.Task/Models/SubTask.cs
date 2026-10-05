namespace TaskManagement.Task.Models;

public class SubTask
{
    public Guid Id { get; set; }

    public Guid TaskId { get; set; }

    public string Title { get; set; } = string.Empty;

    public bool IsCompleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public TaskItem? Task { get; set; }
}