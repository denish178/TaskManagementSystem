namespace TaskManagement.Comment.Services;

public interface ITaskFeedbackPolicyClient
{
    Task<TaskFeedbackContext?> GetTaskContextAsync(
        Guid taskId,
        CancellationToken cancellationToken = default);
}

public sealed class TaskFeedbackContext
{
    public Guid TaskId { get; init; }

    public string Status { get; init; } = string.Empty;

    public Guid? CompletedByUserId { get; init; }

    public bool CanAccess { get; init; }
}
