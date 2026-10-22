using TaskManagement.Comment.Models;

namespace TaskManagement.Comment.Repositories;

public interface ICompletionFeedbackRepository
{
    Task<TaskCompletionFeedback?> GetByTaskIdAsync(Guid taskId);

    Task<TaskCompletionFeedback> UpsertAsync(TaskCompletionFeedback feedback);

    Task DeleteByTaskIdAsync(Guid taskId);
}
