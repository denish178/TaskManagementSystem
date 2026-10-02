using TaskManagement.Dashboard.DTOs;

namespace TaskManagement.Dashboard.Interfaces;

public interface IDashboardService
{
    Task<ProjectDashboardDto?> GetProjectDashboardAsync(
        Guid projectId);

    Task<Dictionary<string, int>> GetProjectStatusCountsAsync(
        Guid projectId);

    Task<int> GetOverdueTasksAsync(
        Guid projectId);

    Task<SprintDashboardDto?> GetSprintProgressAsync(
        Guid sprintId);
}