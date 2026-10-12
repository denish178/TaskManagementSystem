using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace TaskManagement.Task.Services;

public class NotificationClient : INotificationClient
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public NotificationClient(
        HttpClient httpClient,
        IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClient;
        _httpContextAccessor = httpContextAccessor;
    }

    public async System.Threading.Tasks.Task NotifyTaskAssignedAsync(
        Guid assigneeId,
        Guid taskId,
        string taskTitle,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "api/notifications")
        {
            Content = JsonContent.Create(new
            {
                userId = assigneeId,
                title = "Task assigned to you",
                message = $"You were assigned: {taskTitle}",
                linkUrl = $"/tasks/{taskId}",
            }),
        };

        var header = _httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrWhiteSpace(header))
        {
            request.Headers.TryAddWithoutValidation("Authorization", header);
        }

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
