using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Task.Controllers;
using TaskManagement.Task.Data;
using TaskManagement.Task.DTOs;
using TaskManagement.Task.Models;
using System.Linq;

namespace TaskManagement.Task.Tests;

public class SubTasksControllerTests
{
    private static TaskDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TaskDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new TaskDbContext(options);
    }

    private static SubTasksController CreateController(TaskDbContext context)
    {
        var identity = new ClaimsIdentity(
            new[]
            {
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
            },
            "TestAuth");

        var controller = new SubTasksController(context);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity)
            }
        };

        return controller;
    }

    private static TaskItem CreateTask(Guid? taskId = null)
    {
        return new TaskItem
        {
            Id = taskId ?? Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            Title = "Test Task",
            Description = "Test Description",
            Status = "Todo",
            Priority = "Medium",
            CreatedBy = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    // =========================================================
    // CREATE SUBTASK
    // =========================================================

    [Fact]
    public async System.Threading.Tasks.Task CreateSubTask_WithValidRequest_ReturnsCreated()
    {
        using var context = CreateContext();

        var task = CreateTask();

        context.Tasks.Add(task);

        await context.SaveChangesAsync();

        var controller = new SubTasksController(context);

        var request = new CreateSubTaskRequest(
            "Test Subtask");

        var result = await controller.CreateSubTask(
            task.Id,
            request);

        var created = Assert.IsType<CreatedAtActionResult>(result);

        Assert.Equal(201, created.StatusCode);

        var subTask = created.Value!;

        Assert.Equal(
            task.Id,
            subTask.GetType().GetProperty("TaskId")!.GetValue(subTask));

        Assert.Equal(
            "Test Subtask",
            subTask.GetType().GetProperty("Title")!.GetValue(subTask));

        Assert.False(
            (bool)subTask.GetType().GetProperty("IsCompleted")!.GetValue(subTask)!);
    }

    [Fact]
    public async System.Threading.Tasks.Task CreateSubTask_WithEmptyTitle_ReturnsBadRequest()
    {
        using var context = CreateContext();

        var controller = new SubTasksController(context);

        var request = new CreateSubTaskRequest("   ");

        var result = await controller.CreateSubTask(
            Guid.NewGuid(),
            request);

        var badRequest =
            Assert.IsType<BadRequestObjectResult>(result);

        Assert.Equal(400, badRequest.StatusCode);
    }

    [Fact]
    public async System.Threading.Tasks.Task CreateSubTask_WithNonExistingTask_ReturnsNotFound()
    {
        using var context = CreateContext();

        var controller = new SubTasksController(context);

        var request = new CreateSubTaskRequest(
            "Test Subtask");

        var result = await controller.CreateSubTask(
            Guid.NewGuid(),
            request);

        var notFound =
            Assert.IsType<NotFoundObjectResult>(result);

        Assert.Equal(404, notFound.StatusCode);
    }

    [Fact]
    public async System.Threading.Tasks.Task CreateSubTask_TrimsTitle()
    {
        using var context = CreateContext();

        var task = CreateTask();

        context.Tasks.Add(task);

        await context.SaveChangesAsync();

        var controller = new SubTasksController(context);

        var request = new CreateSubTaskRequest(
            "   Test Subtask   ");

        await controller.CreateSubTask(
            task.Id,
            request);

        var subTask = await context.SubTasks.FirstAsync();

        Assert.Equal(
            "Test Subtask",
            subTask.Title);
    }

    // =========================================================
    // GET SUBTASKS
    // =========================================================

    [Fact]
    public async System.Threading.Tasks.Task GetSubTasks_WithExistingTask_ReturnsOk()
    {
        using var context = CreateContext();

        var task = CreateTask();

        context.Tasks.Add(task);

        context.SubTasks.AddRange(
            new SubTask
            {
                Id = Guid.NewGuid(),
                TaskId = task.Id,
                Title = "Subtask 1",
                IsCompleted = false,
                CreatedAt = DateTime.UtcNow
            },
            new SubTask
            {
                Id = Guid.NewGuid(),
                TaskId = task.Id,
                Title = "Subtask 2",
                IsCompleted = true,
                CreatedAt = DateTime.UtcNow.AddMinutes(1)
            });

        await context.SaveChangesAsync();

        var controller = new SubTasksController(context);

        var result = await controller.GetSubTasks(task.Id);

        var ok = Assert.IsType<OkObjectResult>(result);

        var subTasks = Assert.IsAssignableFrom<System.Collections.IEnumerable>(ok.Value);

        var subTaskList = subTasks.Cast<object>().ToList();

        Assert.Equal(2, subTaskList.Count);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetSubTasks_WithNonExistingTask_ReturnsNotFound()
    {
        using var context = CreateContext();

        var controller = new SubTasksController(context);

        var result = await controller.GetSubTasks(
            Guid.NewGuid());

        var notFound =
            Assert.IsType<NotFoundObjectResult>(result);

        Assert.Equal(404, notFound.StatusCode);
    }

    // =========================================================
    // UPDATE SUBTASK
    // =========================================================

    [Fact]
    public async System.Threading.Tasks.Task UpdateSubTask_WithValidRequest_ReturnsOk()
    {
        using var context = CreateContext();

        var task = CreateTask();

        var subTask = new SubTask
        {
            Id = Guid.NewGuid(),
            TaskId = task.Id,
            Title = "Old Subtask",
            IsCompleted = false,
            CreatedAt = DateTime.UtcNow
        };

        context.Tasks.Add(task);

        context.SubTasks.Add(subTask);

        await context.SaveChangesAsync();

        var controller = new SubTasksController(context);

        var request = new UpdateSubTaskRequest(
            "Updated Subtask",
            true);

        var result = await controller.UpdateSubTask(
            task.Id,
            subTask.Id,
            request);

        var ok = Assert.IsType<OkObjectResult>(result);

        var updated = ok.Value!;

        Assert.Equal(
            "Updated Subtask",
            updated.GetType().GetProperty("Title")!.GetValue(updated));

        Assert.True(
            (bool)updated.GetType().GetProperty("IsCompleted")!.GetValue(updated)!);
    }

    [Fact]
    public async System.Threading.Tasks.Task UpdateSubTask_WithEmptyTitle_ReturnsBadRequest()
    {
        using var context = CreateContext();

        var controller = new SubTasksController(context);

        var request = new UpdateSubTaskRequest(
            "   ",
            false);

        var result = await controller.UpdateSubTask(
            Guid.NewGuid(),
            Guid.NewGuid(),
            request);

        var badRequest =
            Assert.IsType<BadRequestObjectResult>(result);

        Assert.Equal(400, badRequest.StatusCode);
    }

    [Fact]
    public async System.Threading.Tasks.Task UpdateSubTask_WithNonExistingSubTask_ReturnsNotFound()
    {
        using var context = CreateContext();

        var controller = new SubTasksController(context);

        var request = new UpdateSubTaskRequest(
            "Updated",
            true);

        var result = await controller.UpdateSubTask(
            Guid.NewGuid(),
            Guid.NewGuid(),
            request);

        var notFound =
            Assert.IsType<NotFoundObjectResult>(result);

        Assert.Equal(404, notFound.StatusCode);
    }

    // =========================================================
    // DELETE SUBTASK
    // =========================================================

    [Fact]
    public async System.Threading.Tasks.Task DeleteSubTask_WithExistingSubTask_ReturnsNoContent()
    {
        using var context = CreateContext();

        var task = CreateTask();

        var subTask = new SubTask
        {
            Id = Guid.NewGuid(),
            TaskId = task.Id,
            Title = "Subtask To Delete",
            IsCompleted = false,
            CreatedAt = DateTime.UtcNow
        };

        context.Tasks.Add(task);

        context.SubTasks.Add(subTask);

        await context.SaveChangesAsync();

        var controller = new SubTasksController(context);

        var result = await controller.DeleteSubTask(
            task.Id,
            subTask.Id);

        Assert.IsType<NoContentResult>(result);

        Assert.Null(
            await context.SubTasks.FindAsync(
                subTask.Id));
    }

    [Fact]
    public async System.Threading.Tasks.Task DeleteSubTask_WithNonExistingSubTask_ReturnsNotFound()
    {
        using var context = CreateContext();

        var controller = new SubTasksController(context);

        var result = await controller.DeleteSubTask(
            Guid.NewGuid(),
            Guid.NewGuid());

        var notFound =
            Assert.IsType<NotFoundObjectResult>(result);

        Assert.Equal(404, notFound.StatusCode);
    }
}



