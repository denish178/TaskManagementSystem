using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Project.Controllers;
using TaskManagement.Project.Data;
using TaskManagement.Project.DTOs;
using TaskManagement.Project.Models;

namespace TaskManagement.Project.Tests;

public class TeamControllerTests
{
    // =========================================================
    // CREATE IN-MEMORY DATABASE
    // =========================================================

    private static ProjectDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ProjectDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ProjectDbContext(options);
    }

    // =========================================================
    // CREATE TEAM
    // =========================================================

    [Fact]
    public async Task CreateTeam_ValidRequest_ReturnsCreated()
    {
        // Arrange
        await using var context = CreateContext();

        var controller = new TeamsController(context);

        var request = new CreateTeamRequest(
            "Development Team",
            "Backend development team",
            Guid.NewGuid());

        // Act
        var result = await controller.CreateTeam(request);

        // Assert
        var createdResult =
            Assert.IsType<CreatedAtActionResult>(result);

        Assert.Equal(
            nameof(TeamsController.GetTeam),
            createdResult.ActionName);

        var team =
            Assert.IsType<Team>(createdResult.Value);

        Assert.NotEqual(Guid.Empty, team.Id);
        Assert.Equal("Development Team", team.Name);
        Assert.Equal(
            "Backend development team",
            team.Description);
    }

    [Fact]
    public async Task CreateTeam_EmptyName_ReturnsBadRequest()
    {
        // Arrange
        await using var context = CreateContext();

        var controller = new TeamsController(context);

        var request = new CreateTeamRequest(
            "",
            "Description",
            Guid.NewGuid());

        // Act
        var result =
            await controller.CreateTeam(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CreateTeam_WhitespaceName_ReturnsBadRequest()
    {
        // Arrange
        await using var context = CreateContext();

        var controller = new TeamsController(context);

        var request = new CreateTeamRequest(
            "   ",
            "Description",
            Guid.NewGuid());

        // Act
        var result =
            await controller.CreateTeam(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    // =========================================================
    // GET ALL TEAMS
    // =========================================================

    [Fact]
    public async Task GetTeams_ReturnsOk()
    {
        // Arrange
        await using var context = CreateContext();

        context.Teams.AddRange(
            new Team
            {
                Id = Guid.NewGuid(),
                Name = "Team One",
                Description = "First team",
                CreatedBy = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow
            },
            new Team
            {
                Id = Guid.NewGuid(),
                Name = "Team Two",
                Description = "Second team",
                CreatedBy = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow
            });

        await context.SaveChangesAsync();

        var controller = new TeamsController(context);

        // Act
        var result = await controller.GetTeams();

        // Assert
        var okResult =
            Assert.IsType<OkObjectResult>(result);

        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetTeams_EmptyDatabase_ReturnsOk()
    {
        // Arrange
        await using var context = CreateContext();

        var controller = new TeamsController(context);

        // Act
        var result = await controller.GetTeams();

        // Assert
        var okResult =
            Assert.IsType<OkObjectResult>(result);

        Assert.NotNull(okResult.Value);
    }

    // =========================================================
    // GET TEAM BY ID
    // =========================================================

    [Fact]
    public async Task GetTeam_ExistingId_ReturnsOk()
    {
        // Arrange
        await using var context = CreateContext();

        var teamId = Guid.NewGuid();

        var team = new Team
        {
            Id = teamId,
            Name = "Development Team",
            Description = "Development team",
            CreatedBy = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow
        };

        context.Teams.Add(team);
        await context.SaveChangesAsync();

        var controller = new TeamsController(context);

        // Act
        var result =
            await controller.GetTeam(teamId);

        // Assert
        var okResult =
            Assert.IsType<OkObjectResult>(result);

        var returnedTeam =
            Assert.IsType<Team>(okResult.Value);

        Assert.Equal(teamId, returnedTeam.Id);
        Assert.Equal(
            "Development Team",
            returnedTeam.Name);
    }

    [Fact]
    public async Task GetTeam_NonExistingId_ReturnsNotFound()
    {
        // Arrange
        await using var context = CreateContext();

        var controller = new TeamsController(context);

        // Act
        var result =
            await controller.GetTeam(Guid.NewGuid());

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    // =========================================================
    // UPDATE TEAM
    // =========================================================

    [Fact]
    public async Task UpdateTeam_ValidRequest_ReturnsOk()
    {
        // Arrange
        await using var context = CreateContext();

        var teamId = Guid.NewGuid();

        var team = new Team
        {
            Id = teamId,
            Name = "Old Team Name",
            Description = "Old description",
            CreatedBy = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow
        };

        context.Teams.Add(team);
        await context.SaveChangesAsync();

        var controller = new TeamsController(context);

        var request = new UpdateTeamRequest(
            "Updated Team Name",
            "Updated description");

        // Act
        var result =
            await controller.UpdateTeam(
                teamId,
                request);

        // Assert
        var okResult =
            Assert.IsType<OkObjectResult>(result);

        var updatedTeam =
            Assert.IsType<Team>(okResult.Value);

        Assert.Equal(
            "Updated Team Name",
            updatedTeam.Name);

        Assert.Equal(
            "Updated description",
            updatedTeam.Description);
    }

    [Fact]
    public async Task UpdateTeam_EmptyName_ReturnsBadRequest()
    {
        // Arrange
        await using var context = CreateContext();

        var controller = new TeamsController(context);

        var request = new UpdateTeamRequest(
            "",
            "Description");

        // Act
        var result =
            await controller.UpdateTeam(
                Guid.NewGuid(),
                request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UpdateTeam_WhitespaceName_ReturnsBadRequest()
    {
        // Arrange
        await using var context = CreateContext();

        var controller = new TeamsController(context);

        var request = new UpdateTeamRequest(
            "   ",
            "Description");

        // Act
        var result =
            await controller.UpdateTeam(
                Guid.NewGuid(),
                request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UpdateTeam_NonExistingId_ReturnsNotFound()
    {
        // Arrange
        await using var context = CreateContext();

        var controller = new TeamsController(context);

        var request = new UpdateTeamRequest(
            "Updated Team",
            "Updated description");

        // Act
        var result =
            await controller.UpdateTeam(
                Guid.NewGuid(),
                request);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    // =========================================================
    // DELETE TEAM
    // =========================================================

    [Fact]
    public async Task DeleteTeam_ExistingId_ReturnsNoContent()
    {
        // Arrange
        await using var context = CreateContext();

        var teamId = Guid.NewGuid();

        var team = new Team
        {
            Id = teamId,
            Name = "Team To Delete",
            Description = "Delete test",
            CreatedBy = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow
        };

        context.Teams.Add(team);
        await context.SaveChangesAsync();

        var controller = new TeamsController(context);

        // Act
        var result =
            await controller.DeleteTeam(teamId);

        // Assert
        Assert.IsType<NoContentResult>(result);

        var deletedTeam =
            await context.Teams.FindAsync(teamId);

        Assert.Null(deletedTeam);
    }

    [Fact]
    public async Task DeleteTeam_NonExistingId_ReturnsNotFound()
    {
        // Arrange
        await using var context = CreateContext();

        var controller = new TeamsController(context);

        // Act
        var result =
            await controller.DeleteTeam(Guid.NewGuid());

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    // =========================================================
    // ADD TEAM MEMBER
    // =========================================================

    [Fact]
    public async Task AddMember_ValidRequest_ReturnsOk()
    {
        // Arrange
        await using var context = CreateContext();

        var teamId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var team = new Team
        {
            Id = teamId,
            Name = "Development Team",
            Description = "Development team",
            CreatedBy = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow
        };

        context.Teams.Add(team);
        await context.SaveChangesAsync();

        var controller = new TeamsController(context);

        var request = new AddTeamMemberRequest(
            userId,
            "Developer");

        // Act
        var result =
            await controller.AddMember(
                teamId,
                request);

        // Assert
        var okResult =
            Assert.IsType<OkObjectResult>(result);

        var member =
            Assert.IsType<TeamMember>(okResult.Value);

        Assert.Equal(teamId, member.TeamId);
        Assert.Equal(userId, member.UserId);
        Assert.Equal("Developer", member.Role);
    }

    [Fact]
    public async Task AddMember_EmptyUserId_ReturnsBadRequest()
    {
        // Arrange
        await using var context = CreateContext();

        var teamId = Guid.NewGuid();

        var team = new Team
        {
            Id = teamId,
            Name = "Development Team",
            Description = "Development team",
            CreatedBy = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow
        };

        context.Teams.Add(team);
        await context.SaveChangesAsync();

        var controller = new TeamsController(context);

        var request = new AddTeamMemberRequest(
            Guid.Empty,
            "Developer");

        // Act
        var result =
            await controller.AddMember(
                teamId,
                request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task AddMember_TeamDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        await using var context = CreateContext();

        var controller = new TeamsController(context);

        var request = new AddTeamMemberRequest(
            Guid.NewGuid(),
            "Developer");

        // Act
        var result =
            await controller.AddMember(
                Guid.NewGuid(),
                request);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task AddMember_UserAlreadyMember_ReturnsConflict()
    {
        // Arrange
        await using var context = CreateContext();

        var teamId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var team = new Team
        {
            Id = teamId,
            Name = "Development Team",
            Description = "Development team",
            CreatedBy = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow
        };

        var existingMember = new TeamMember
        {
            Id = Guid.NewGuid(),
            TeamId = teamId,
            UserId = userId,
            Role = "Developer",
            JoinedAt = DateTime.UtcNow
        };

        context.Teams.Add(team);
        context.TeamMembers.Add(existingMember);

        await context.SaveChangesAsync();

        var controller = new TeamsController(context);

        var request = new AddTeamMemberRequest(
            userId,
            "Developer");

        // Act
        var result =
            await controller.AddMember(
                teamId,
                request);

        // Assert
        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task AddMember_EmptyRole_DefaultsToMember()
    {
        // Arrange
        await using var context = CreateContext();

        var teamId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var team = new Team
        {
            Id = teamId,
            Name = "Development Team",
            Description = "Development team",
            CreatedBy = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow
        };

        context.Teams.Add(team);
        await context.SaveChangesAsync();

        var controller = new TeamsController(context);

        var request = new AddTeamMemberRequest(
            userId,
            "");

        // Act
        var result =
            await controller.AddMember(
                teamId,
                request);

        // Assert
        var okResult =
            Assert.IsType<OkObjectResult>(result);

        var member =
            Assert.IsType<TeamMember>(okResult.Value);

        Assert.Equal("Member", member.Role);
    }

    // =========================================================
    // GET TEAM MEMBERS
    // =========================================================

    [Fact]
    public async Task GetMembers_ExistingTeam_ReturnsOk()
    {
        // Arrange
        await using var context = CreateContext();

        var teamId = Guid.NewGuid();

        var team = new Team
        {
            Id = teamId,
            Name = "Development Team",
            Description = "Development team",
            CreatedBy = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow
        };

        context.Teams.Add(team);

        context.TeamMembers.AddRange(
            new TeamMember
            {
                Id = Guid.NewGuid(),
                TeamId = teamId,
                UserId = Guid.NewGuid(),
                Role = "Developer",
                JoinedAt = DateTime.UtcNow
            },
            new TeamMember
            {
                Id = Guid.NewGuid(),
                TeamId = teamId,
                UserId = Guid.NewGuid(),
                Role = "Tester",
                JoinedAt = DateTime.UtcNow
            });

        await context.SaveChangesAsync();

        var controller = new TeamsController(context);

        // Act
        var result =
            await controller.GetMembers(teamId);

        // Assert
        var okResult =
            Assert.IsType<OkObjectResult>(result);

        var members =
            Assert.IsAssignableFrom<IEnumerable<TeamMember>>(
                okResult.Value);

        Assert.Equal(2, members.Count());
    }

    [Fact]
    public async Task GetMembers_NonExistingTeam_ReturnsNotFound()
    {
        // Arrange
        await using var context = CreateContext();

        var controller = new TeamsController(context);

        // Act
        var result =
            await controller.GetMembers(Guid.NewGuid());

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    // =========================================================
    // REMOVE TEAM MEMBER
    // =========================================================

    [Fact]
    public async Task RemoveMember_ExistingMember_ReturnsNoContent()
    {
        // Arrange
        await using var context = CreateContext();

        var teamId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var team = new Team
        {
            Id = teamId,
            Name = "Development Team",
            Description = "Development team",
            CreatedBy = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow
        };

        var member = new TeamMember
        {
            Id = Guid.NewGuid(),
            TeamId = teamId,
            UserId = userId,
            Role = "Developer",
            JoinedAt = DateTime.UtcNow
        };

        context.Teams.Add(team);
        context.TeamMembers.Add(member);

        await context.SaveChangesAsync();

        var controller = new TeamsController(context);

        // Act
        var result =
            await controller.RemoveMember(
                teamId,
                userId);

        // Assert
        Assert.IsType<NoContentResult>(result);

        var deletedMember =
            await context.TeamMembers
                .FirstOrDefaultAsync(x =>
                    x.TeamId == teamId &&
                    x.UserId == userId);

        Assert.Null(deletedMember);
    }

    [Fact]
    public async Task RemoveMember_NonExistingMember_ReturnsNotFound()
    {
        // Arrange
        await using var context = CreateContext();

        var controller = new TeamsController(context);

        // Act
        var result =
            await controller.RemoveMember(
                Guid.NewGuid(),
                Guid.NewGuid());

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }
}