using System.Net;
using System.Net.Http;
using System.Text;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using TaskManagement.Dashboard.Services;

namespace TaskManagement.Dashboard.Tests;

public class DashboardServiceTests
{
    private static DashboardService CreateService(
        HttpStatusCode statusCode,
        string responseJson)
    {
        var handler = new Mock<HttpMessageHandler>();

        handler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(
                    responseJson,
                    Encoding.UTF8,
                    "application/json")
            });

        var httpClient = new HttpClient(handler.Object)
        {
            BaseAddress = new Uri("http://localhost")
        };

        var httpClientFactory =
            new Mock<IHttpClientFactory>();

        httpClientFactory
            .Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);

        var configuration =
            new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["ServiceUrls:TaskService"] =
                            "http://localhost:5266"
                    })
                .Build();

        return new DashboardService(
            httpClientFactory.Object,
            configuration);
    }

    [Fact]
    public async Task GetProjectDashboard_WhenTaskServiceReturnsData_ReturnsDashboard()
    {
        // Arrange
        var projectId = Guid.NewGuid();

        var json = $$"""
        {
            "projectId": "{{projectId}}",
            "totalTasks": 10,
            "statusCounts": {
                "Todo": 4,
                "InProgress": 3,
                "Testing": 1,
                "Done": 2
            },
            "overdueTasks": 2
        }
        """;

        var service = CreateService(
            HttpStatusCode.OK,
            json);

        // Act
        var result =
            await service.GetProjectDashboardAsync(projectId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(projectId, result.ProjectId);
        Assert.Equal(10, result.TotalTasks);
        Assert.Equal(2, result.OverdueTasks);
        Assert.Equal(4, result.StatusCounts["Todo"]);
        Assert.Equal(3, result.StatusCounts["InProgress"]);
        Assert.Equal(2, result.StatusCounts["Done"]);
    }

    [Fact]
    public async Task GetProjectDashboard_WhenTaskServiceFails_ReturnsNull()
    {
        // Arrange
        var service = CreateService(
            HttpStatusCode.InternalServerError,
            "{}");

        // Act
        var result =
            await service.GetProjectDashboardAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetProjectDashboard_WhenResponseIsInvalid_ReturnsNull()
    {
        // Arrange
        var service = CreateService(
            HttpStatusCode.OK,
            "null");

        // Act
        var result =
            await service.GetProjectDashboardAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetProjectStatusCounts_WhenTaskServiceReturnsData_ReturnsStatusCounts()
    {
        // Arrange
        var json = """
        {
            "projectId": "11111111-1111-1111-1111-111111111111",
            "totalTasks": 8,
            "statusCounts": {
                "Todo": 3,
                "InProgress": 2,
                "Testing": 1,
                "Done": 2
            },
            "overdueTasks": 1
        }
        """;

        var service = CreateService(
            HttpStatusCode.OK,
            json);

        // Act
        var result =
            await service.GetProjectStatusCountsAsync(Guid.NewGuid());

        // Assert
        Assert.Equal(4, result.Count);
        Assert.Equal(3, result["Todo"]);
        Assert.Equal(2, result["InProgress"]);
        Assert.Equal(1, result["Testing"]);
        Assert.Equal(2, result["Done"]);
    }

    [Fact]
    public async Task GetProjectStatusCounts_WhenTaskServiceFails_ReturnsEmptyDictionary()
    {
        // Arrange
        var service = CreateService(
            HttpStatusCode.InternalServerError,
            "{}");

        // Act
        var result =
            await service.GetProjectStatusCountsAsync(Guid.NewGuid());

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetOverdueTasks_WhenTaskServiceReturnsData_ReturnsOverdueCount()
    {
        // Arrange
        var json = """
        {
            "projectId": "11111111-1111-1111-1111-111111111111",
            "totalTasks": 10,
            "statusCounts": {},
            "overdueTasks": 4
        }
        """;

        var service = CreateService(
            HttpStatusCode.OK,
            json);

        // Act
        var result =
            await service.GetOverdueTasksAsync(Guid.NewGuid());

        // Assert
        Assert.Equal(4, result);
    }

    [Fact]
    public async Task GetOverdueTasks_WhenTaskServiceFails_ReturnsZero()
    {
        // Arrange
        var service = CreateService(
            HttpStatusCode.InternalServerError,
            "{}");

        // Act
        var result =
            await service.GetOverdueTasksAsync(Guid.NewGuid());

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task GetSprintProgress_WhenTaskServiceReturnsData_ReturnsSprintDashboard()
    {
        // Arrange
        var sprintId = Guid.NewGuid();

        var json = $$"""
        {
            "sprintId": "{{sprintId}}",
            "totalTasks": 20,
            "completedTasks": 12,
            "todo": 3,
            "inProgress": 4,
            "testing": 1,
            "done": 12,
            "progressPercentage": 60.0
        }
        """;

        var service = CreateService(
            HttpStatusCode.OK,
            json);

        // Act
        var result =
            await service.GetSprintProgressAsync(sprintId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(sprintId, result.SprintId);
        Assert.Equal(20, result.TotalTasks);
        Assert.Equal(12, result.CompletedTasks);
        Assert.Equal(3, result.Todo);
        Assert.Equal(4, result.InProgress);
        Assert.Equal(1, result.Testing);
        Assert.Equal(12, result.Done);
        Assert.Equal(60.0, result.ProgressPercentage);
    }

    [Fact]
    public async Task GetSprintProgress_WhenTaskServiceFails_ReturnsNull()
    {
        // Arrange
        var service = CreateService(
            HttpStatusCode.InternalServerError,
            "{}");

        // Act
        var result =
            await service.GetSprintProgressAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }
}