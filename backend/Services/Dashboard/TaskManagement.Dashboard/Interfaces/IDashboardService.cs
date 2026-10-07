using TaskManagement.Dashboard.DTOs;

namespace TaskManagement.Dashboard.Interfaces;

// Defines dashboard operations used by the API controller.
public interface IDashboardService
{
    // Returns the complete dashboard summary for a project.
    Task<ProjectDashboardDto?> GetProjectDashboardAsync(
        Guid projectId);

    // Returns task counts grouped by status.
    Task<Dictionary<string, int>> GetProjectStatusCountsAsync(
        Guid projectId);

    // Returns the number of overdue tasks for a project.
    Task<int> GetOverdueTasksAsync(
        Guid projectId);

    // Returns progress information for a sprint.
    Task<SprintDashboardDto?> GetSprintProgressAsync(
        Guid sprintId);
}