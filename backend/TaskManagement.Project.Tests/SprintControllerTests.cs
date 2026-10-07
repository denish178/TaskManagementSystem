using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Project.Controllers;
using TaskManagement.Project.Data;
using TaskManagement.Project.DTOs;
using TaskManagement.Project.Models;
using ProjectModel = TaskManagement.Project.Models.Project;

namespace TaskManagement.Project.Tests;

public class SprintControllerTests
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
    // CREATE SPRINT
    // =========================================================

    [Fact]
    public async Task CreateSprint_ValidRequest_ReturnsCreated()
    {
        // Arrange
        await using var context = CreateContext();

        var projectId = Guid.NewGuid();

        var project = new ProjectModel
        {
            Id = projectId,
            TeamId = Guid.NewGuid(),
            Name = "Task Management Project",
            Description = "Project Description",
            CreatedBy = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow
        };

        context.Projects.Add(project);
        await context.SaveChangesAsync();

        var controller = new SprintsController(context);

        var startDate = DateTime.UtcNow.Date;
        var endDate = startDate.AddDays(14);

        var request = new CreateSprintRequest(
            projectId,
            "Sprint 1",
            "Complete initial development",
            startDate,
            endDate);

        // Act
        var result = await controller.CreateSprint(request);

        // Assert
        var createdResult =
            Assert.IsType<CreatedAtActionResult>(result);

        Assert.Equal(
            nameof(SprintsController.GetSprint),
            createdResult.ActionName);

        var sprint =
            Assert.IsType<Sprint>(createdResult.Value);

        Assert.Equal(projectId, sprint.ProjectId);
        Assert.Equal("Sprint 1", sprint.Name);
        Assert.Equal(
            "Complete initial development",
            sprint.Goal);
    }

    [Fact]
    public async Task CreateSprint_EmptyProjectId_ReturnsBadRequest()
    {
        // Arrange
        await using var context = CreateContext();

        var controller = new SprintsController(context);

        var startDate = DateTime.UtcNow.Date;
        var endDate = startDate.AddDays(14);

        var request = new CreateSprintRequest(
            Guid.Empty,
            "Sprint 1",
            "Goal",
            startDate,
            endDate);

        // Act
        var result =
            await controller.CreateSprint(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CreateSprint_EmptyName_ReturnsBadRequest()
    {
        // Arrange
        await using var context = CreateContext();

        var controller = new SprintsController(context);

        var startDate = DateTime.UtcNow.Date;
        var endDate = startDate.AddDays(14);

        var request = new CreateSprintRequest(
            Guid.NewGuid(),
            "",
            "Goal",
            startDate,
            endDate);

        // Act
        var result =
            await controller.CreateSprint(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CreateSprint_InvalidDates_ReturnsBadRequest()
    {
        // Arrange
        await using var context = CreateContext();

        var controller = new SprintsController(context);

        var startDate = DateTime.UtcNow.Date.AddDays(10);
        var endDate = DateTime.UtcNow.Date;

        var request = new CreateSprintRequest(
            Guid.NewGuid(),
            "Sprint 1",
            "Goal",
            startDate,
            endDate);

        // Act
        var result =
            await controller.CreateSprint(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CreateSprint_ProjectDoesNotExist_ReturnsBadRequest()
    {
        // Arrange
        await using var context = CreateContext();

        var controller = new SprintsController(context);

        var startDate = DateTime.UtcNow.Date;
        var endDate = startDate.AddDays(14);

        var request = new CreateSprintRequest(
            Guid.NewGuid(),
            "Sprint 1",
            "Goal",
            startDate,
            endDate);

        // Act
        var result =
            await controller.CreateSprint(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    // =========================================================
    // GET PROJECT SPRINTS
    // =========================================================

    [Fact]
    public async Task GetProjectSprints_ExistingProject_ReturnsOk()
    {
        // Arrange
        await using var context = CreateContext();

        var projectId = Guid.NewGuid();

        var project = new ProjectModel
        {
            Id = projectId,
            TeamId = Guid.NewGuid(),
            Name = "Project One",
            Description = "Description",
            CreatedBy = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow
        };

        var startDate = DateTime.UtcNow.Date;

        context.Projects.Add(project);

        context.Sprints.Add(new Sprint
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Name = "Sprint 1",
            Goal = "First sprint",
            StartDate = startDate,
            EndDate = startDate.AddDays(14),
            CreatedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        var controller = new SprintsController(context);

        // Act
        var result =
            await controller.GetProjectSprints(projectId);

        // Assert
        var okResult =
            Assert.IsType<OkObjectResult>(result);

        Assert.NotNull(okResult.Value);

        var sprints =
            Assert.IsAssignableFrom<IEnumerable<Sprint>>(
                okResult.Value);

        Assert.Single(sprints);
    }

    [Fact]
    public async Task GetProjectSprints_NonExistingProject_ReturnsNotFound()
    {
        // Arrange
        await using var context = CreateContext();

        var controller = new SprintsController(context);

        // Act
        var result =
            await controller.GetProjectSprints(Guid.NewGuid());

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    // =========================================================
    // GET SPRINT BY ID
    // =========================================================

    [Fact]
    public async Task GetSprint_ExistingId_ReturnsOk()
    {
        // Arrange
        await using var context = CreateContext();

        var projectId = Guid.NewGuid();
        var sprintId = Guid.NewGuid();

        var project = new ProjectModel
        {
            Id = projectId,
            TeamId = Guid.NewGuid(),
            Name = "Project One",
            Description = "Description",
            CreatedBy = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow
        };

        var startDate = DateTime.UtcNow.Date;

        var sprint = new Sprint
        {
            Id = sprintId,
            ProjectId = projectId,
            Name = "Sprint 1",
            Goal = "First sprint",
            StartDate = startDate,
            EndDate = startDate.AddDays(14),
            CreatedAt = DateTime.UtcNow
        };

        context.Projects.Add(project);
        context.Sprints.Add(sprint);

        await context.SaveChangesAsync();

        var controller = new SprintsController(context);

        // Act
        var result =
            await controller.GetSprint(sprintId);

        // Assert
        var okResult =
            Assert.IsType<OkObjectResult>(result);

        var returnedSprint =
            Assert.IsType<Sprint>(okResult.Value);

        Assert.Equal(sprintId, returnedSprint.Id);
        Assert.Equal(projectId, returnedSprint.ProjectId);
        Assert.Equal("Sprint 1", returnedSprint.Name);
    }

    [Fact]
    public async Task GetSprint_NonExistingId_ReturnsNotFound()
    {
        // Arrange
        await using var context = CreateContext();

        var controller = new SprintsController(context);

        // Act
        var result =
            await controller.GetSprint(Guid.NewGuid());

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    // =========================================================
    // UPDATE SPRINT
    // =========================================================

    [Fact]
    public async Task UpdateSprint_ValidRequest_ReturnsOk()
    {
        // Arrange
        await using var context = CreateContext();

        var sprintId = Guid.NewGuid();

        var startDate = DateTime.UtcNow.Date;

        var sprint = new Sprint
        {
            Id = sprintId,
            ProjectId = Guid.NewGuid(),
            Name = "Old Sprint",
            Goal = "Old Goal",
            StartDate = startDate,
            EndDate = startDate.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };

        context.Sprints.Add(sprint);
        await context.SaveChangesAsync();

        var controller = new SprintsController(context);

        var request = new UpdateSprintRequest(
            "Updated Sprint",
            "Updated Goal",
            startDate,
            startDate.AddDays(14));

        // Act
        var result =
            await controller.UpdateSprint(
                sprintId,
                request);

        // Assert
        var okResult =
            Assert.IsType<OkObjectResult>(result);

        var updatedSprint =
            Assert.IsType<Sprint>(okResult.Value);

        Assert.Equal(
            "Updated Sprint",
            updatedSprint.Name);

        Assert.Equal(
            "Updated Goal",
            updatedSprint.Goal);

        Assert.Equal(
            startDate.AddDays(14),
            updatedSprint.EndDate);
    }

    [Fact]
    public async Task UpdateSprint_EmptyName_ReturnsBadRequest()
    {
        // Arrange
        await using var context = CreateContext();

        var controller = new SprintsController(context);

        var startDate = DateTime.UtcNow.Date;

        var request = new UpdateSprintRequest(
            "",
            "Goal",
            startDate,
            startDate.AddDays(14));

        // Act
        var result =
            await controller.UpdateSprint(
                Guid.NewGuid(),
                request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UpdateSprint_InvalidDates_ReturnsBadRequest()
    {
        // Arrange
        await using var context = CreateContext();

        var controller = new SprintsController(context);

        var startDate = DateTime.UtcNow.Date.AddDays(10);
        var endDate = DateTime.UtcNow.Date;

        var request = new UpdateSprintRequest(
            "Updated Sprint",
            "Goal",
            startDate,
            endDate);

        // Act
        var result =
            await controller.UpdateSprint(
                Guid.NewGuid(),
                request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UpdateSprint_NonExistingId_ReturnsNotFound()
    {
        // Arrange
        await using var context = CreateContext();

        var controller = new SprintsController(context);

        var startDate = DateTime.UtcNow.Date;

        var request = new UpdateSprintRequest(
            "Updated Sprint",
            "Goal",
            startDate,
            startDate.AddDays(14));

        // Act
        var result =
            await controller.UpdateSprint(
                Guid.NewGuid(),
                request);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    // =========================================================
    // DELETE SPRINT
    // =========================================================

    [Fact]
    public async Task DeleteSprint_ExistingId_ReturnsNoContent()
    {
        // Arrange
        await using var context = CreateContext();

        var sprintId = Guid.NewGuid();

        var startDate = DateTime.UtcNow.Date;

        var sprint = new Sprint
        {
            Id = sprintId,
            ProjectId = Guid.NewGuid(),
            Name = "Sprint To Delete",
            Goal = "Delete test",
            StartDate = startDate,
            EndDate = startDate.AddDays(14),
            CreatedAt = DateTime.UtcNow
        };

        context.Sprints.Add(sprint);
        await context.SaveChangesAsync();

        var controller = new SprintsController(context);

        // Act
        var result =
            await controller.DeleteSprint(sprintId);

        // Assert
        Assert.IsType<NoContentResult>(result);

        var deletedSprint =
            await context.Sprints.FindAsync(sprintId);

        Assert.Null(deletedSprint);
    }

    [Fact]
    public async Task DeleteSprint_NonExistingId_ReturnsNotFound()
    {
        // Arrange
        await using var context = CreateContext();

        var controller = new SprintsController(context);

        // Act
        var result =
            await controller.DeleteSprint(Guid.NewGuid());

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }
}