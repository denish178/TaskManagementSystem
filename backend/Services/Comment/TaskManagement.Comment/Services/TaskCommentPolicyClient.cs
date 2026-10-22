using System.Net.Http.Json;

namespace TaskManagement.Comment.Services;

public class TaskCommentPolicyClient : ITaskCommentPolicyClient
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TaskCommentPolicyClient(
        HttpClient httpClient,
        IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClient;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<bool> CanUserCommentOnTaskAsync(
        Guid taskId,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"api/tasks/{taskId}/comment-eligibility");

        var header = _httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrWhiteSpace(header))
        {
            request.Headers.TryAddWithoutValidation("Authorization", header);
        }

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return false;
        }

        var payload = await response.Content.ReadFromJsonAsync<CommentEligibilityResponse>(
            cancellationToken: cancellationToken);

        return payload?.Allowed == true;
    }

    private sealed class CommentEligibilityResponse
    {
        public bool Allowed { get; set; }
    }
}
