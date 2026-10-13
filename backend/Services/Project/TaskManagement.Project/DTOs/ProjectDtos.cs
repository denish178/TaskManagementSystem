namespace TaskManagement.Project.DTOs;

public record CreateProjectRequest(
    Guid TeamId,
    string Name,
    string? Description,
    Guid CreatedBy
);

public record UpdateProjectRequest(
    string Name,
    string? Description
);

public record EnsureProjectMemberRequest(Guid UserId);