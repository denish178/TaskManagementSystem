using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Project.Controllers;
using TaskManagement.Project.Data;
using TaskManagement.Project.DTOs;
using TaskManagement.Project.Models;
using ProjectModel = TaskManagement.Project.Models.Project;

namespace TaskManagement.Project.Tests;

public class ProjectControllerTests
{
    // =========================================================
    // CREATE TEST DATABASE
    // =========================================================

    private static ProjectDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ProjectDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ProjectDbContext(options);
    }

    // =========================================================
    // CREATE PROJECT
    // =========================================================

    [Fact]
    public async Task CreateProject_ValidRequest_ReturnsCreated()
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

        var controller = new ProjectsController(context);

        var request = new CreateProjectRequest(
            teamId,
            "Task Management Project",
            "Project Description",
            Guid.NewGuid());

        // Act
        var result = await controller.CreateProject(request);

        // Assert
        var createdResult =
            Assert.IsType<CreatedAtActionResult>(result);

        Assert.Equal(
            nameof(ProjectsController.GetProject),
            createdResult.ActionName);

        var project =
            Assert.IsType<ProjectModel>(createdResult.Value);

        Assert.Equal(
            "Task Management Project",
            project.Name);

        Assert.Equal(
            teamId,
            project.TeamId);
    }

    [Fact]
    public async Task CreateProject_EmptyTeamId_ReturnsBadRequest()
    {
        // Arrange
        await using var context = CreateContext();

        var controller = new ProjectsController(context);

        var request = new CreateProjectRequest(
            Guid.Empty,
            "Project",
            "Description",
            Guid.NewGuid());

        // Act
        var result =
            await controller.CreateProject(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CreateProject_EmptyName_ReturnsBadRequest()
    {
        // Arrange
        await using var context = CreateContext();

        var controller = new ProjectsController(context);

        var request = new CreateProjectRequest(
            Guid.NewGuid(),
            "",
            "Description",
            Guid.NewGuid());

        // Act
        var result =
            await controller.CreateProject(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CreateProject_TeamDoesNotExist_ReturnsBadRequest()
    {
        // Arrange
        await using var context = CreateContext();

        var controller = new ProjectsController(context);

        var request = new CreateProjectRequest(
            Guid.NewGuid(),
            "Project",
            "Description",
            Guid.NewGuid());

        // Act
        var result =
            await controller.CreateProject(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    // =========================================================
    // GET ALL PROJECTS
    // =========================================================

    [Fact]
    public async Task GetProjects_ReturnsOk()
    {
        // Arrange
        await using var context = CreateContext();

        var teamId = Guid.NewGuid();

        var team = new Team
        {
            Id = teamId,
            Name = "Development Team",
            Description = "Development",
            CreatedBy = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow
        };

        var project = new ProjectModel
        {
            Id = Guid.NewGuid(),
            TeamId = teamId,
            Name = "Project One",
            Description = "First Project",
            CreatedBy = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow
        };

        context.Teams.Add(team);
        context.Projects.Add(project);

        await context.SaveChangesAsync();

        var controller = new ProjectsController(context);

        // Act
        var result =
            await controller.GetProjects();

        // Assert
        var okResult =
            Assert.IsType<OkObjectResult>(result);

        Assert.NotNull(okResult.Value);
    }

    // =========================================================
    // GET PROJECT BY ID
    // =========================================================

    [Fact]
    public async Task GetProject_ExistingId_ReturnsOk()
    {
        // Arrange
        await using var context = CreateContext();

        var teamId = Guid.NewGuid();
        var projectId = Guid.NewGuid();

        var team = new Team
        {
            Id = teamId,
            Name = "Development Team",
            Description = "Development",
            CreatedBy = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow
        };

        var project = new ProjectModel
        {
            Id = projectId,
            TeamId = teamId,
            Name = "Project One",
            Description = "Description",
            CreatedBy = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow
        };

        context.Teams.Add(team);
        context.Projects.Add(project);

        await context.SaveChangesAsync();

        var controller = new ProjectsController(context);

        // Act
        var result =
            await controller.GetProject(projectId);

        // Assert
        var okResult =
            Assert.IsType<OkObjectResult>(result);

        var returnedProject =
            Assert.IsType<ProjectModel>(okResult.Value);

        Assert.Equal(
            projectId,
            returnedProject.Id);
    }

    [Fact]
    public async Task GetProject_NonExistingId_ReturnsNotFound()
    {
        // Arrange
        await using var context = CreateContext();

        var controller = new ProjectsController(context);

        var projectId = Guid.NewGuid();

        // Act
        var result =
            await controller.GetProject(projectId);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    // =========================================================
    // UPDATE PROJECT
    // =========================================================

    [Fact]
    public async Task UpdateProject_ValidRequest_ReturnsOk()
    {
        // Arrange
        await using var context = CreateContext();

        var projectId = Guid.NewGuid();

        var project = new ProjectModel
        {
            Id = projectId,
            TeamId = Guid.NewGuid(),
            Name = "Old Project Name",
            Description = "Old Description",
            CreatedBy = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow
        };

        context.Projects.Add(project);

        await context.SaveChangesAsync();

        var controller = new ProjectsController(context);

        var request = new UpdateProjectRequest(
            "Updated Project Name",
            "Updated Description");

        // Act
        var result =
            await controller.UpdateProject(
                projectId,
                request);

        // Assert
        var okResult =
            Assert.IsType<OkObjectResult>(result);

        var updatedProject =
            Assert.IsType<ProjectModel>(okResult.Value);

        Assert.Equal(
            "Updated Project Name",
            updatedProject.Name);

        Assert.Equal(
            "Updated Description",
            updatedProject.Description);
    }

    [Fact]
    public async Task UpdateProject_EmptyName_ReturnsBadRequest()
    {
        // Arrange
        await using var context = CreateContext();

        var controller = new ProjectsController(context);

        var request = new UpdateProjectRequest(
            "",
            "Description");

        // Act
        var result =
            await controller.UpdateProject(
                Guid.NewGuid(),
                request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UpdateProject_NonExistingId_ReturnsNotFound()
    {
        // Arrange
        await using var context = CreateContext();

        var controller = new ProjectsController(context);

        var request = new UpdateProjectRequest(
            "Updated Project",
            "Updated Description");

        var projectId = Guid.NewGuid();

        // Act
        var result =
            await controller.UpdateProject(
                projectId,
                request);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    // =========================================================
    // DELETE PROJECT
    // =========================================================

    [Fact]
    public async Task DeleteProject_ExistingId_ReturnsNoContent()
    {
        // Arrange
        await using var context = CreateContext();

        var projectId = Guid.NewGuid();

        var project = new ProjectModel
        {
            Id = projectId,
            TeamId = Guid.NewGuid(),
            Name = "Project To Delete",
            Description = "Description",
            CreatedBy = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow
        };

        context.Projects.Add(project);

        await context.SaveChangesAsync();

        var controller = new ProjectsController(context);

        // Act
        var result =
            await controller.DeleteProject(projectId);

        // Assert
        Assert.IsType<NoContentResult>(result);

        var deletedProject =
            await context.Projects.FindAsync(projectId);

        Assert.Null(deletedProject);
    }

    [Fact]
    public async Task DeleteProject_NonExistingId_ReturnsNotFound()
    {
        // Arrange
        await using var context = CreateContext();

        var controller = new ProjectsController(context);

        var projectId = Guid.NewGuid();

        // Act
        var result =
            await controller.DeleteProject(projectId);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }
}