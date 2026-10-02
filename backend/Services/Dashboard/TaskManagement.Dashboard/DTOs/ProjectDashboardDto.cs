namespace TaskManagement.Dashboard.DTOs;

public class ProjectDashboardDto
{
    public Guid ProjectId { get; set; }

    public int TotalTasks { get; set; }

    public Dictionary<string, int> StatusCounts { get; set; }
        = new();

    public int OverdueTasks { get; set; }
}