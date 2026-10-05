namespace TaskManagement.Project.Models;

public class Sprint
{
    public Guid Id { get; set; }

    public Guid ProjectId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Goal { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public DateTime CreatedAt { get; set; }

    public Project? Project { get; set; }
}