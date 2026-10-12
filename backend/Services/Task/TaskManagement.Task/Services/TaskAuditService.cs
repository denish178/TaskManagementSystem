using TaskManagement.Task.Data;
using TaskManagement.Task.Models;

namespace TaskManagement.Task.Services;

public class TaskAuditService : ITaskAuditService
{
    private readonly TaskDbContext _context;

    public TaskAuditService(TaskDbContext context)
    {
        _context = context;
    }

    public async System.Threading.Tasks.Task RecordAsync(
        Guid taskId,
        Guid actorUserId,
        string action,
        string? oldValue,
        string? newValue,
        CancellationToken cancellationToken = default)
    {
        _context.TaskAuditEntries.Add(new TaskAuditEntry
        {
            Id = Guid.NewGuid(),
            TaskId = taskId,
            ActorUserId = actorUserId,
            Action = action,
            OldValue = oldValue,
            NewValue = newValue,
            CreatedAtUtc = DateTime.UtcNow,
        });

        await _context.SaveChangesAsync(cancellationToken);
    }
}
