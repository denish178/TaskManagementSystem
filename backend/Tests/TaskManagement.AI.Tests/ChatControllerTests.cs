using Microsoft.AspNetCore.Mvc;
using Moq;
using TaskManagement.AI.Controllers;
using TaskManagement.AI.DTOs;
using TaskManagement.AI.Services;

namespace TaskManagement.AI.Tests;

public class ChatControllerTests
{
    [Fact]
    public async Task Chat_EmptyMessage_ReturnsBadRequest()
    {
        var gemini = new Mock<IGeminiService>();
        var db = new Mock<IDatabaseContextService>();
        var controller = new ChatController(gemini.Object, db.Object);

        var result = await controller.Chat(new ChatRequest("   "));

        Assert.IsType<BadRequestObjectResult>(result.Result);
        gemini.Verify(
            g => g.AskAsync(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task Chat_ValidMessage_ReturnsOkWithAnswer()
    {
        var gemini = new Mock<IGeminiService>();
        gemini
            .Setup(g => g.AskAsync("How many tasks?", It.IsAny<string>()))
            .ReturnsAsync("You have 3 tasks.");

        var db = new Mock<IDatabaseContextService>();
        db.Setup(d => d.GetDatabaseContextAsync()).ReturnsAsync("{\"tasks\":[]}");

        var controller = new ChatController(gemini.Object, db.Object);

        var result = await controller.Chat(new ChatRequest("How many tasks?"));

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ChatResponse>(ok.Value);
        Assert.Equal("You have 3 tasks.", response.Answer);
    }

    [Fact]
    public async Task Chat_WhenGeminiThrows_Returns500()
    {
        var gemini = new Mock<IGeminiService>();
        gemini
            .Setup(g => g.AskAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("API key missing"));

        var db = new Mock<IDatabaseContextService>();
        db.Setup(d => d.GetDatabaseContextAsync()).ReturnsAsync("{}");

        var controller = new ChatController(gemini.Object, db.Object);

        var result = await controller.Chat(new ChatRequest("Hi"));

        var status = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, status.StatusCode);
    }
}
