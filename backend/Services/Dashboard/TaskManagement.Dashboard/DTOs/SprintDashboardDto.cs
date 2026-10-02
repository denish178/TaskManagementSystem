namespace TaskManagement.Dashboard.DTOs;

public class SprintDashboardDto
{
    public Guid SprintId { get; set; }

    public int TotalTasks { get; set; }

    public int CompletedTasks { get; set; }

    public int Todo { get; set; }

    public int InProgress { get; set; }

    public int Testing { get; set; }

    public int Done { get; set; }

    public double ProgressPercentage { get; set; }
}