using System.Net.Http.Headers;
using System.Net.Http.Json;
using TaskManagement.Dashboard.DTOs;
using TaskManagement.Dashboard.Interfaces;

namespace TaskManagement.Dashboard.Services;

public class DashboardService : IDashboardService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DashboardService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        IHttpContextAccessor httpContextAccessor)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;
    }

    private HttpClient CreateTaskServiceClient()
    {
        var client = _httpClientFactory.CreateClient();

        var authorizationHeader =
            _httpContextAccessor.HttpContext?
                .Request.Headers.Authorization
                .FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(authorizationHeader))
        {
            client.DefaultRequestHeaders.Authorization =
                AuthenticationHeaderValue.Parse(authorizationHeader);
        }

        return client;
    }

    private string GetTaskServiceUrl()
    {
        return _configuration["ServiceUrls:TaskService"]
            ?? throw new InvalidOperationException(
                "Task Service URL is not configured.");
    }

    private async Task<TaskDashboardDataDto?>
        GetProjectDashboardDataAsync(Guid projectId)
    {
        var client = CreateTaskServiceClient();

        var response = await client.GetAsync(
            $"{GetTaskServiceUrl()}/api/tasks/project/{projectId}/dashboard-data");

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<TaskDashboardDataDto>();
    }

    public async Task<ProjectDashboardDto?>
        GetProjectDashboardAsync(Guid projectId)
    {
        var data =
            await GetProjectDashboardDataAsync(projectId);

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
        var data =
            await GetProjectDashboardDataAsync(projectId);

        return data?.StatusCounts
            ?? new Dictionary<string, int>();
    }

    public async Task<int>
        GetOverdueTasksAsync(Guid projectId)
    {
        var data =
            await GetProjectDashboardDataAsync(projectId);

        return data?.OverdueTasks ?? 0;
    }

    public async Task<SprintDashboardDto?>
        GetSprintProgressAsync(Guid sprintId)
    {
        var client = CreateTaskServiceClient();

        var response = await client.GetAsync(
            $"{GetTaskServiceUrl()}/api/tasks/sprint/{sprintId}/dashboard-data");

        response.EnsureSuccessStatusCode();

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