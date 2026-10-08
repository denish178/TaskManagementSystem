using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Task.Controllers;
using TaskManagement.Task.Data;
using TaskManagement.Task.DTOs;
using TaskManagement.Task.Models;

namespace TaskManagement.Task.Tests;

public class TasksControllerTests
{
    private static TaskDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TaskDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new TaskDbContext(options);
    }

    // =========================================================
    // CREATE TASK
    // =========================================================

    [Fact]
    public async System.Threading.Tasks.Task CreateTask_WithValidRequest_ReturnsCreated()
    {
        using var context = CreateContext();

        var controller = new TasksController(context);

        var request = new CreateTaskRequest(
    Guid.NewGuid(),
    null,
    "Test Task",
    "Test Description",
    "High",
    null);

        var result = await controller.CreateTask(request);

        var created = Assert.IsType<CreatedAtActionResult>(result);

        Assert.Equal(201, created.StatusCode);

        var task = Assert.IsType<TaskItem>(created.Value);

        Assert.Equal("Test Task", task.Title);
        Assert.Equal("Test Description", task.Description);
        Assert.Equal("High", task.Priority);
        Assert.Equal("Todo", task.Status);
    }

    [Fact]
    public async System.Threading.Tasks.Task CreateTask_WithEmptyProjectId_ReturnsBadRequest()
    {
        using var context = CreateContext();

        var controller = new TasksController(context);

        var request = new CreateTaskRequest(
    Guid.Empty,
    null,
    "Test Task",
    null,
    null,
    null);

        var result = await controller.CreateTask(request);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);

        Assert.Equal(400, badRequest.StatusCode);
    }

    [Fact]
    public async System.Threading.Tasks.Task CreateTask_WithEmptyTitle_ReturnsBadRequest()
    {
        using var context = CreateContext();

        var controller = new TasksController(context);

        var request = new CreateTaskRequest(
    Guid.NewGuid(),
    null,
    "   ",
    null,
    null,
    null);

        var result = await controller.CreateTask(request);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);

        Assert.Equal(400, badRequest.StatusCode);
    }

    [Fact]
    public async System.Threading.Tasks.Task CreateTask_WithNoPriority_UsesMediumPriority()
    {
        using var context = CreateContext();

        var controller = new TasksController(context);

        var request = new CreateTaskRequest(
    Guid.NewGuid(),
    null,
    "Test Task",
    null,
    null,
    null);

        await controller.CreateTask(request);

        var task = await context.Tasks.FirstAsync();

        Assert.Equal("Medium", task.Priority);
    }

    [Fact]
    public async System.Threading.Tasks.Task CreateTask_TrimsTitle()
    {
        using var context = CreateContext();

        var controller = new TasksController(context);

        var request = new CreateTaskRequest(
    Guid.NewGuid(),
    null,
    "   Test Task   ",
    null,
    "High",
    null);

        await controller.CreateTask(request);

        var task = await context.Tasks.FirstAsync();

        Assert.Equal("Test Task", task.Title);
    }

    // =========================================================
    // GET ALL TASKS
    // =========================================================

    [Fact]
    public async System.Threading.Tasks.Task GetTasks_WithTasks_ReturnsOk()
    {
        using var context = CreateContext();

        context.Tasks.AddRange(
            new TaskItem
            {
                Id = Guid.NewGuid(),
                ProjectId = Guid.NewGuid(),
                Title = "Task 1",
                Status = "Todo",
                Priority = "Medium",
                CreatedBy = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow.AddMinutes(-2),
                UpdatedAt = DateTime.UtcNow
            },
            new TaskItem
            {
                Id = Guid.NewGuid(),
                ProjectId = Guid.NewGuid(),
                Title = "Task 2",
                Status = "Done",
                Priority = "High",
                CreatedBy = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

        await context.SaveChangesAsync();

        var controller = new TasksController(context);

        var result = await controller.GetTasks(
            null,
            null,
            null,
            null,
            null);

        var ok = Assert.IsType<OkObjectResult>(result);

        Assert.NotNull(ok.Value);

        var tasks = Assert.IsAssignableFrom<IEnumerable<object>>(ok.Value);

        Assert.Equal(2, tasks.Count());
    }

    [Fact]
    public async System.Threading.Tasks.Task GetTasks_WithProjectFilter_ReturnsMatchingTasks()
    {
        using var context = CreateContext();

        var projectId = Guid.NewGuid();

        context.Tasks.AddRange(
            new TaskItem
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                Title = "Project Task",
                Status = "Todo",
                Priority = "Medium",
                CreatedBy = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new TaskItem
            {
                Id = Guid.NewGuid(),
                ProjectId = Guid.NewGuid(),
                Title = "Other Task",
                Status = "Todo",
                Priority = "Medium",
                CreatedBy = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

        await context.SaveChangesAsync();

        var controller = new TasksController(context);

        var result = await controller.GetTasks(
            projectId,
            null,
            null,
            null,
            null);

        var ok = Assert.IsType<OkObjectResult>(result);

        var tasks = Assert.IsAssignableFrom<IEnumerable<object>>(ok.Value);

        Assert.Single(tasks);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetTasks_WithStatusFilter_ReturnsMatchingTasks()
    {
        using var context = CreateContext();

        context.Tasks.AddRange(
            new TaskItem
            {
                Id = Guid.NewGuid(),
                ProjectId = Guid.NewGuid(),
                Title = "Todo Task",
                Status = "Todo",
                Priority = "Medium",
                CreatedBy = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new TaskItem
            {
                Id = Guid.NewGuid(),
                ProjectId = Guid.NewGuid(),
                Title = "Done Task",
                Status = "Done",
                Priority = "Medium",
                CreatedBy = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

        await context.SaveChangesAsync();

        var controller = new TasksController(context);

        var result = await controller.GetTasks(
            null,
            null,
            "Done",
            null,
            null);

        var ok = Assert.IsType<OkObjectResult>(result);

        var tasks = Assert.IsAssignableFrom<IEnumerable<object>>(ok.Value);

        Assert.Single(tasks);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetTasks_WithPriorityFilter_ReturnsMatchingTasks()
    {
        using var context = CreateContext();

        context.Tasks.AddRange(
            new TaskItem
            {
                Id = Guid.NewGuid(),
                ProjectId = Guid.NewGuid(),
                Title = "High Task",
                Status = "Todo",
                Priority = "High",
                CreatedBy = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new TaskItem
            {
                Id = Guid.NewGuid(),
                ProjectId = Guid.NewGuid(),
                Title = "Low Task",
                Status = "Todo",
                Priority = "Low",
                CreatedBy = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

        await context.SaveChangesAsync();

        var controller = new TasksController(context);

        var result = await controller.GetTasks(
            null,
            null,
            null,
            "High",
            null);

        var ok = Assert.IsType<OkObjectResult>(result);

        var tasks = Assert.IsAssignableFrom<IEnumerable<object>>(ok.Value);

        Assert.Single(tasks);
    }

    // =========================================================
    // GET SINGLE TASK
    // =========================================================

    [Fact]
    public async System.Threading.Tasks.Task GetTask_WithExistingTask_ReturnsOk()
    {
        using var context = CreateContext();

        var taskId = Guid.NewGuid();

        context.Tasks.Add(new TaskItem
        {
            Id = taskId,
            ProjectId = Guid.NewGuid(),
            Title = "Test Task",
            Status = "Todo",
            Priority = "Medium",
            CreatedBy = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        var controller = new TasksController(context);

        var result = await controller.GetTask(taskId);

        var ok = Assert.IsType<OkObjectResult>(result);

        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetTask_WithNonExistingTask_ReturnsNotFound()
    {
        using var context = CreateContext();

        var controller = new TasksController(context);

        var result = await controller.GetTask(Guid.NewGuid());

        var notFound = Assert.IsType<NotFoundObjectResult>(result);

        Assert.Equal(404, notFound.StatusCode);
    }

    // =========================================================
    // UPDATE TASK
    // =========================================================

    [Fact]
    public async System.Threading.Tasks.Task UpdateTask_WithValidRequest_ReturnsOk()
    {
        using var context = CreateContext();

        var taskId = Guid.NewGuid();

        context.Tasks.Add(new TaskItem
        {
            Id = taskId,
            ProjectId = Guid.NewGuid(),
            Title = "Old Title",
            Description = "Old Description",
            Status = "Todo",
            Priority = "Medium",
            CreatedBy = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        var controller = new TasksController(context);

        var request = new UpdateTaskRequest(
            "New Title",
            "New Description",
            null,
            "High",
            null);

        var result = await controller.UpdateTask(
            taskId,
            request);

        var ok = Assert.IsType<OkObjectResult>(result);

        var task = Assert.IsType<TaskItem>(ok.Value);

        Assert.Equal("New Title", task.Title);
        Assert.Equal("New Description", task.Description);
        Assert.Equal("High", task.Priority);
    }

    [Fact]
    public async System.Threading.Tasks.Task UpdateTask_WithEmptyTitle_ReturnsBadRequest()
    {
        using var context = CreateContext();

        var controller = new TasksController(context);

        var request = new UpdateTaskRequest(
            "   ",
            null,
            null,
            null,
            null);

        var result = await controller.UpdateTask(
            Guid.NewGuid(),
            request);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);

        Assert.Equal(400, badRequest.StatusCode);
    }

    [Fact]
    public async System.Threading.Tasks.Task UpdateTask_WithNonExistingTask_ReturnsNotFound()
    {
        using var context = CreateContext();

        var controller = new TasksController(context);

        var request = new UpdateTaskRequest(
            "Updated Task",
            null,
            null,
            null,
            null);

        var result = await controller.UpdateTask(
            Guid.NewGuid(),
            request);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);

        Assert.Equal(404, notFound.StatusCode);
    }

    // =========================================================
    // DELETE TASK
    // =========================================================

    [Fact]
    public async System.Threading.Tasks.Task DeleteTask_WithExistingTask_ReturnsNoContent()
    {
        using var context = CreateContext();

        var taskId = Guid.NewGuid();

        context.Tasks.Add(new TaskItem
        {
            Id = taskId,
            ProjectId = Guid.NewGuid(),
            Title = "Task To Delete",
            Status = "Todo",
            Priority = "Medium",
            CreatedBy = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        var controller = new TasksController(context);

        var result = await controller.DeleteTask(taskId);

        Assert.IsType<NoContentResult>(result);

        Assert.Null(await context.Tasks.FindAsync(taskId));
    }

    [Fact]
    public async System.Threading.Tasks.Task DeleteTask_WithNonExistingTask_ReturnsNotFound()
    {
        using var context = CreateContext();

        var controller = new TasksController(context);

        var result = await controller.DeleteTask(Guid.NewGuid());

        var notFound = Assert.IsType<NotFoundObjectResult>(result);

        Assert.Equal(404, notFound.StatusCode);
    }

    // =========================================================
    // UPDATE STATUS
    // =========================================================

    [Fact]
    public async System.Threading.Tasks.Task UpdateStatus_WithValidStatus_ReturnsOk()
    {
        using var context = CreateContext();

        var taskId = Guid.NewGuid();

        context.Tasks.Add(new TaskItem
        {
            Id = taskId,
            ProjectId = Guid.NewGuid(),
            Title = "Status Task",
            Status = "Todo",
            Priority = "Medium",
            CreatedBy = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        var controller = new TasksController(context);

        var request = new UpdateTaskStatusRequest("Done");

        var result = await controller.UpdateStatus(
            taskId,
            request);

        var ok = Assert.IsType<OkObjectResult>(result);

        var task = Assert.IsType<TaskItem>(ok.Value);

        Assert.Equal("Done", task.Status);
    }

    [Fact]
    public async System.Threading.Tasks.Task UpdateStatus_WithEmptyStatus_ReturnsBadRequest()
    {
        using var context = CreateContext();

        var controller = new TasksController(context);

        var request = new UpdateTaskStatusRequest("   ");

        var result = await controller.UpdateStatus(
            Guid.NewGuid(),
            request);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);

        Assert.Equal(400, badRequest.StatusCode);
    }

    [Fact]
    public async System.Threading.Tasks.Task UpdateStatus_WithInvalidStatus_ReturnsBadRequest()
    {
        using var context = CreateContext();

        var controller = new TasksController(context);

        var request = new UpdateTaskStatusRequest(
            "InvalidStatus");

        var result = await controller.UpdateStatus(
            Guid.NewGuid(),
            request);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);

        Assert.Equal(400, badRequest.StatusCode);
    }

    [Fact]
    public async System.Threading.Tasks.Task UpdateStatus_WithNonExistingTask_ReturnsNotFound()
    {
        using var context = CreateContext();

        var controller = new TasksController(context);

        var request = new UpdateTaskStatusRequest("Done");

        var result = await controller.UpdateStatus(
            Guid.NewGuid(),
            request);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);

        Assert.Equal(404, notFound.StatusCode);
    }

    // =========================================================
    // ASSIGN TASK
    // =========================================================

    [Fact]
    public async System.Threading.Tasks.Task AssignTask_WithValidAssignee_ReturnsOk()
    {
        using var context = CreateContext();

        var taskId = Guid.NewGuid();
        var assigneeId = Guid.NewGuid();

        context.Tasks.Add(new TaskItem
        {
            Id = taskId,
            ProjectId = Guid.NewGuid(),
            Title = "Assignment Task",
            Status = "Todo",
            Priority = "Medium",
            CreatedBy = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        var controller = new TasksController(context);

        var request = new AssignTaskRequest(assigneeId);

        var result = await controller.AssignTask(
            taskId,
            request);

        var ok = Assert.IsType<OkObjectResult>(result);

        var task = Assert.IsType<TaskItem>(ok.Value);

        Assert.Equal(assigneeId, task.AssigneeId);
    }

    [Fact]
    public async System.Threading.Tasks.Task AssignTask_WithEmptyAssignee_ReturnsBadRequest()
    {
        using var context = CreateContext();

        var controller = new TasksController(context);

        var request = new AssignTaskRequest(Guid.Empty);

        var result = await controller.AssignTask(
            Guid.NewGuid(),
            request);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);

        Assert.Equal(400, badRequest.StatusCode);
    }

    [Fact]
    public async System.Threading.Tasks.Task AssignTask_WithNonExistingTask_ReturnsNotFound()
    {
        using var context = CreateContext();

        var controller = new TasksController(context);

        var request = new AssignTaskRequest(Guid.NewGuid());

        var result = await controller.AssignTask(
            Guid.NewGuid(),
            request);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);

        Assert.Equal(404, notFound.StatusCode);
    }

    // =========================================================
    // PROJECT DASHBOARD
    // =========================================================

    [Fact]
    public async System.Threading.Tasks.Task GetProjectDashboardData_ReturnsCorrectCounts()
    {
        using var context = CreateContext();

        var projectId = Guid.NewGuid();

        context.Tasks.AddRange(
            new TaskItem
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                Title = "Todo",
                Status = "Todo",
                Priority = "Medium",
                CreatedBy = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new TaskItem
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                Title = "Progress",
                Status = "InProgress",
                Priority = "Medium",
                CreatedBy = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new TaskItem
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                Title = "Review",
                Status = "Review",
                Priority = "Medium",
                CreatedBy = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new TaskItem
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                Title = "Done",
                Status = "Done",
                Priority = "Medium",
                CreatedBy = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

        await context.SaveChangesAsync();

        var controller = new TasksController(context);

        var result = await controller.GetProjectDashboardData(
            projectId);

        var ok = Assert.IsType<OkObjectResult>(result);

        var json = JsonSerializer.Serialize(ok.Value);

        using var document = JsonDocument.Parse(json);

        var root = document.RootElement;

        Assert.Equal(4, root.GetProperty("totalTasks").GetInt32());

        Assert.Equal(
            1,
            root.GetProperty("statusCounts")
                .GetProperty("todo")
                .GetInt32());

        Assert.Equal(
            1,
            root.GetProperty("statusCounts")
                .GetProperty("inProgress")
                .GetInt32());

        Assert.Equal(
            1,
            root.GetProperty("statusCounts")
                .GetProperty("testing")
                .GetInt32());

        Assert.Equal(
            1,
            root.GetProperty("statusCounts")
                .GetProperty("done")
                .GetInt32());
    }

    // =========================================================
    // SPRINT DASHBOARD
    // =========================================================

    [Fact]
    public async System.Threading.Tasks.Task GetSprintDashboardData_ReturnsCorrectProgress()
    {
        using var context = CreateContext();

        var sprintId = Guid.NewGuid();

        context.Tasks.AddRange(
            new TaskItem
            {
                Id = Guid.NewGuid(),
                ProjectId = Guid.NewGuid(),
                SprintId = sprintId,
                Title = "Done 1",
                Status = "Done",
                Priority = "Medium",
                CreatedBy = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new TaskItem
            {
                Id = Guid.NewGuid(),
                ProjectId = Guid.NewGuid(),
                SprintId = sprintId,
                Title = "Done 2",
                Status = "Done",
                Priority = "Medium",
                CreatedBy = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new TaskItem
            {
                Id = Guid.NewGuid(),
                ProjectId = Guid.NewGuid(),
                SprintId = sprintId,
                Title = "Todo",
                Status = "Todo",
                Priority = "Medium",
                CreatedBy = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new TaskItem
            {
                Id = Guid.NewGuid(),
                ProjectId = Guid.NewGuid(),
                SprintId = sprintId,
                Title = "Progress",
                Status = "InProgress",
                Priority = "Medium",
                CreatedBy = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

        await context.SaveChangesAsync();

        var controller = new TasksController(context);

        var result = await controller.GetSprintDashboardData(
            sprintId);

        var ok = Assert.IsType<OkObjectResult>(result);

        var json = JsonSerializer.Serialize(ok.Value);

        using var document = JsonDocument.Parse(json);

        var root = document.RootElement;

        Assert.Equal(4, root.GetProperty("totalTasks").GetInt32());

        Assert.Equal(2, root.GetProperty("completedTasks").GetInt32());

        Assert.Equal(2, root.GetProperty("done").GetInt32());

        Assert.Equal(
            50,
            root.GetProperty("progressPercentage").GetDouble());
    }

    [Fact]
    public async System.Threading.Tasks.Task GetSprintDashboardData_WithNoTasks_ReturnsZeroProgress()
    {
        using var context = CreateContext();

        var controller = new TasksController(context);

        var result = await controller.GetSprintDashboardData(
            Guid.NewGuid());

        var ok = Assert.IsType<OkObjectResult>(result);

        var json = JsonSerializer.Serialize(ok.Value);

        using var document = JsonDocument.Parse(json);

        var root = document.RootElement;

        Assert.Equal(
            0,
            root.GetProperty("totalTasks").GetInt32());

        Assert.Equal(
            0,
            root.GetProperty("completedTasks").GetInt32());

        Assert.Equal(
            0,
            root.GetProperty("progressPercentage").GetDouble());
    }
}