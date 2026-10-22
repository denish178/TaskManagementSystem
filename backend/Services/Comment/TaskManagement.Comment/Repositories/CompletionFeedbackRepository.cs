using MongoDB.Driver;
using TaskManagement.Comment.Models;

namespace TaskManagement.Comment.Repositories;

public class CompletionFeedbackRepository : ICompletionFeedbackRepository
{
    private readonly IMongoCollection<TaskCompletionFeedback> _feedback;

    public CompletionFeedbackRepository(IMongoDatabase database)
    {
        _feedback = database.GetCollection<TaskCompletionFeedback>("completionFeedback");
    }

    public async Task<TaskCompletionFeedback?> GetByTaskIdAsync(Guid taskId)
    {
        return await _feedback
            .Find(x => x.TaskId == taskId)
            .FirstOrDefaultAsync();
    }

    public async Task<TaskCompletionFeedback> UpsertAsync(TaskCompletionFeedback feedback)
    {
        var existing = await GetByTaskIdAsync(feedback.TaskId);
        if (existing == null)
        {
            feedback.Id = Guid.NewGuid();
            await _feedback.InsertOneAsync(feedback);
            return feedback;
        }

        feedback.Id = existing.Id;
        await _feedback.ReplaceOneAsync(x => x.TaskId == feedback.TaskId, feedback);
        return feedback;
    }

    public async Task DeleteByTaskIdAsync(Guid taskId)
    {
        await _feedback.DeleteOneAsync(x => x.TaskId == taskId);
    }
}
