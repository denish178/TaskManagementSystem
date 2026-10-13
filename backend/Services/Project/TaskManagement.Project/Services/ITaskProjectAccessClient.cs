namespace TaskManagement.Project.Services;

public interface ITaskProjectAccessClient
{
    Task<HashSet<Guid>> GetAccessibleProjectIdsAsync(
        CancellationToken cancellationToken = default);
}
