namespace TaskManagement.Comment.Services;

public interface ITaskCommentPolicyClient
{
    Task<bool> CanUserCommentOnTaskAsync(
        Guid taskId,
        CancellationToken cancellationToken = default);
}
