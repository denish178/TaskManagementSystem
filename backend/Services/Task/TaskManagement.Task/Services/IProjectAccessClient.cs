namespace TaskManagement.Task.Services;

public interface IProjectAccessClient
{
    Task<HashSet<Guid>> GetAccessibleProjectIdsAsync(
        CancellationToken cancellationToken = default);

    Task<bool> CanAccessProjectAsync(
        Guid projectId,
        CancellationToken cancellationToken = default);
}
