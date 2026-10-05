namespace TaskManagement.Project.DTOs;

public record CreateSprintRequest(
    Guid ProjectId,
    string Name,
    string? Goal,
    DateTime StartDate,
    DateTime EndDate
);

public record UpdateSprintRequest(
    string Name,
    string? Goal,
    DateTime StartDate,
    DateTime EndDate
);