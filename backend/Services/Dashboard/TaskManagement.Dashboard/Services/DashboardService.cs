using System.Net.Http.Json;
using TaskManagement.Dashboard.DTOs;
using TaskManagement.Dashboard.Interfaces;

namespace TaskManagement.Dashboard.Services;

public class DashboardService : IDashboardService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public DashboardService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    public async Task<ProjectDashboardDto?> GetProjectDashboardAsync(
        Guid projectId)
    {
        var client = _httpClientFactory.CreateClient();

        var taskServiceUrl =
            _configuration["ServiceUrls:TaskService"];

        var response =
            await client.GetAsync(
                $"{taskServiceUrl}/api/tasks/project/{projectId}/dashboard-data");

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var data =
            await response.Content
                .ReadFromJsonAsync<TaskDashboardDataDto>();

        if (data == null)
        {
            return null;
        }

        return new ProjectDashboardDto
        {
            ProjectId = data.ProjectId,
            TotalTasks = data.TotalTasks,
            StatusCounts = data.StatusCounts,
            OverdueTasks = data.OverdueTasks
        };
    }

    public async Task<Dictionary<string, int>>
        GetProjectStatusCountsAsync(Guid projectId)
    {
        var client = _httpClientFactory.CreateClient();

        var taskServiceUrl =
            _configuration["ServiceUrls:TaskService"];

        var response =
            await client.GetAsync(
                $"{taskServiceUrl}/api/tasks/project/{projectId}/dashboard-data");

        if (!response.IsSuccessStatusCode)
        {
            return new Dictionary<string, int>();
        }

        var data =
            await response.Content
                .ReadFromJsonAsync<TaskDashboardDataDto>();

        return data?.StatusCounts
            ?? new Dictionary<string, int>();
    }

    public async Task<int> GetOverdueTasksAsync(Guid projectId)
    {
        var client = _httpClientFactory.CreateClient();

        var taskServiceUrl =
            _configuration["ServiceUrls:TaskService"];

        var response =
            await client.GetAsync(
                $"{taskServiceUrl}/api/tasks/project/{projectId}/dashboard-data");

        if (!response.IsSuccessStatusCode)
        {
            return 0;
        }

        var data =
            await response.Content
                .ReadFromJsonAsync<TaskDashboardDataDto>();

        return data?.OverdueTasks ?? 0;
    }

    public async Task<SprintDashboardDto?>
        GetSprintProgressAsync(Guid sprintId)
    {
        var client = _httpClientFactory.CreateClient();

        var taskServiceUrl =
            _configuration["ServiceUrls:TaskService"];

        var response =
            await client.GetAsync(
                $"{taskServiceUrl}/api/tasks/sprint/{sprintId}/dashboard-data");

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var data =
            await response.Content
                .ReadFromJsonAsync<SprintDashboardDataDto>();

        if (data == null)
        {
            return null;
        }

        return new SprintDashboardDto
        {
            SprintId = data.SprintId,
            TotalTasks = data.TotalTasks,
            CompletedTasks = data.CompletedTasks,
            Todo = data.Todo,
            InProgress = data.InProgress,
            Testing = data.Testing,
            Done = data.Done,
            ProgressPercentage = data.ProgressPercentage
        };
    }
}