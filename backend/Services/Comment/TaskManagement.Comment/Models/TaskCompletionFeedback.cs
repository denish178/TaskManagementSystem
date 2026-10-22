using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TaskManagement.Comment.Models;

public class TaskCompletionFeedback
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }

    [BsonRepresentation(BsonType.String)]
    public Guid TaskId { get; set; }

    [BsonRepresentation(BsonType.String)]
    public Guid SubmittedByUserId { get; set; }

    public string Content { get; set; } = string.Empty;

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
}
