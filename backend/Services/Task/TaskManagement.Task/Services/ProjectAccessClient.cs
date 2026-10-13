using System.Net.Http.Headers;

namespace TaskManagement.Task.Services;

public class ProjectAccessClient : IProjectAccessClient
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ProjectAccessClient(
        HttpClient httpClient,
        IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClient;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<HashSet<Guid>> GetAccessibleProjectIdsAsync(
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "api/projects/accessible-ids");

        AttachBearerToken(request);

        using var response = await _httpClient.SendAsync(
            request,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return [];
        }

        var ids = await response.Content.ReadFromJsonAsync<List<Guid>>(
            cancellationToken);

        return ids?.ToHashSet() ?? [];
    }

    public async Task<bool> CanAccessProjectAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"api/projects/{projectId}/access");

        AttachBearerToken(request);

        using var response = await _httpClient.SendAsync(
            request,
            cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            return false;
        }

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }

        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<AccessResponse>(
            cancellationToken);

        return payload?.Allowed == true;
    }

    public async System.Threading.Tasks.Task EnsureProjectMemberAsync(
        Guid projectId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"api/projects/{projectId}/ensure-member")
        {
            Content = JsonContent.Create(new { userId })
        };

        AttachBearerToken(request);

        using var response = await _httpClient.SendAsync(
            request,
            cancellationToken);

        response.EnsureSuccessStatusCode();
    }

    private void AttachBearerToken(HttpRequestMessage request)
    {
        var header = _httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrWhiteSpace(header))
        {
            request.Headers.TryAddWithoutValidation("Authorization", header);
        }
    }

    private sealed class AccessResponse
    {
        public bool Allowed { get; set; }
    }
}
