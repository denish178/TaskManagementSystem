namespace TaskManagement.File.Services;

public interface ITaskAccessClient
{
    Task<bool> CanAccessTaskAsync(
        Guid taskId,
        CancellationToken cancellationToken = default);
}
