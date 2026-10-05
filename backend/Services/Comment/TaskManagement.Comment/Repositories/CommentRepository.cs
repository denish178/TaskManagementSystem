using MongoDB.Driver;
using CommentModel = TaskManagement.Comment.Models.Comment;

namespace TaskManagement.Comment.Repositories;

public class CommentRepository
{
    private readonly IMongoCollection<CommentModel> _comments;

    public CommentRepository(IMongoDatabase database)
    {
        _comments = database.GetCollection<CommentModel>("comments");
    }

    public async Task<CommentModel> CreateAsync(CommentModel comment)
    {
        await _comments.InsertOneAsync(comment);
        return comment;
    }

    public async Task<List<CommentModel>> GetByTaskIdAsync(Guid taskId)
    {
        return await _comments
            .Find(x => x.TaskId == taskId)
            .SortBy(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<CommentModel?> GetByIdAsync(Guid id)
    {
        return await _comments
            .Find(x => x.Id == id)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> UpdateAsync(Guid id, string content)
    {
        var update = Builders<CommentModel>.Update
            .Set(x => x.Content, content)
            .Set(x => x.UpdatedAt, DateTime.UtcNow);

        var result = await _comments.UpdateOneAsync(
            x => x.Id == id,
            update);

        return result.ModifiedCount > 0;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var result = await _comments.DeleteOneAsync(x => x.Id == id);

        return result.DeletedCount > 0;
    }
}