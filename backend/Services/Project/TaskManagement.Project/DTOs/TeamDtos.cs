namespace TaskManagement.Project.DTOs;

public record CreateTeamRequest(
    string Name,
    string? Description,
    Guid CreatedBy
);

public record UpdateTeamRequest(
    string Name,
    string? Description
);

public record AddTeamMemberRequest(
    Guid UserId,
    string? Role
);