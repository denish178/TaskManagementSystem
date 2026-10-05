namespace TaskManagement.Project.Models;

public class Project
{
    public Guid Id { get; set; }

    public Guid TeamId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public Guid CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public Team? Team { get; set; }

    public ICollection<Sprint> Sprints { get; set; }
        = new List<Sprint>();
}