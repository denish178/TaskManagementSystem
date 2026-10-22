using System.Net;
using System.Text.Json;

namespace TaskManagement.Comment.Services;

public class TaskFeedbackPolicyClient : ITaskFeedbackPolicyClient
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TaskFeedbackPolicyClient(
        HttpClient httpClient,
        IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClient;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<TaskFeedbackContext?> GetTaskContextAsync(
        Guid taskId,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"api/tasks/{taskId}");

        var header = _httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrWhiteSpace(header))
        {
            request.Headers.TryAddWithoutValidation("Authorization", header);
        }

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            return new TaskFeedbackContext
            {
                TaskId = taskId,
                CanAccess = false,
            };
        }

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var root = document.RootElement;

        return new TaskFeedbackContext
        {
            TaskId = taskId,
            Status = ReadString(root, "status") ?? string.Empty,
            CompletedByUserId = ReadGuid(root, "completedByUserId"),
            CanAccess = true,
        };
    }

    private static string? ReadString(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var value))
        {
            return null;
        }

        return value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    }

    private static Guid? ReadGuid(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var value))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.String
            && Guid.TryParse(value.GetString(), out var parsed))
        {
            return parsed;
        }

        return null;
    }
}
