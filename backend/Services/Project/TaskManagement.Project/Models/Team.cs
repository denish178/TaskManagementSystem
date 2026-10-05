namespace TaskManagement.Project.Models;

public class Team
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public Guid CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public ICollection<TeamMember> Members { get; set; }
        = new List<TeamMember>();

    public ICollection<Project> Projects { get; set; }
        = new List<Project>();
}