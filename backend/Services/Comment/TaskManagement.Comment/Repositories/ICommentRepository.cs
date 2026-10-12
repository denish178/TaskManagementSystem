using CommentModel = TaskManagement.Comment.Models.Comment;

namespace TaskManagement.Comment.Repositories;

public interface ICommentRepository
{
    Task<CommentModel> CreateAsync(CommentModel comment);
    Task<List<CommentModel>> GetByTaskIdAsync(Guid taskId);
    Task<CommentModel?> GetByIdAsync(Guid id);
    Task<bool> UpdateAsync(Guid id, string content);
    Task<bool> DeleteAsync(Guid id);
}
