namespace TaskManagement.Task.DTOs;

public record CreateTaskRequest(
    Guid ProjectId,
    Guid? SprintId,
    string Title,
    string? Description,
    string? Priority,
    Guid CreatedBy,
    DateTime? DueDate
);

public record UpdateTaskRequest(
    string Title,
    string? Description,
    Guid? SprintId,
    string? Priority,
    DateTime? DueDate
);

public record UpdateTaskStatusRequest(
    string Status
);

public record AssignTaskRequest(
    Guid AssigneeId
);

public record CreateSubTaskRequest(
    string Title
);

public record UpdateSubTaskRequest(
    string Title,
    bool IsCompleted
);