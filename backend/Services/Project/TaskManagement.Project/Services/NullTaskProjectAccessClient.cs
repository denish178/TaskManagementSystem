namespace TaskManagement.Project.Services;

/// <summary>
/// Used in tests when task-service project IDs are not needed.
/// </summary>
public sealed class NullTaskProjectAccessClient : ITaskProjectAccessClient
{
    public Task<HashSet<Guid>> GetAccessibleProjectIdsAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new HashSet<Guid>());
}
