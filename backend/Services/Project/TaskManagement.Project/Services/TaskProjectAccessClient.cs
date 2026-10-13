namespace TaskManagement.Project.Services;

public class TaskProjectAccessClient : ITaskProjectAccessClient
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TaskProjectAccessClient(
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
            "api/tasks/accessible-project-ids");

        var header = _httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrWhiteSpace(header))
        {
            request.Headers.TryAddWithoutValidation("Authorization", header);
        }

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
}
