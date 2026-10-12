using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TaskManagement.Shared.RateLimiting;
using TaskManagement.AI.DTOs;
using TaskManagement.AI.Services;

namespace TaskManagement.AI.Controllers;

[ApiController]
[Route("api/chat")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly GeminiService _geminiService;
    private readonly DatabaseContextService _databaseContextService;

    public ChatController(
        GeminiService geminiService,
        DatabaseContextService databaseContextService)
    {
        _geminiService = geminiService;
        _databaseContextService = databaseContextService;
    }

    [HttpPost]
    [EnableRateLimiting(RateLimitingExtensions.ChatPolicy)]
    public async Task<ActionResult<ChatResponse>> Chat(
        [FromBody] ChatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new
            {
                message = "Message is required."
            });
        }

        try
        {
            // Get real data from Azure SQL
            var databaseContext =
                await _databaseContextService
                    .GetDatabaseContextAsync();

            // Send the user's question + database data to Gemini
            var answer =
                await _geminiService.AskAsync(
                    request.Message,
                    databaseContext);

            return Ok(
                new ChatResponse(answer)
            );
        }
        catch (Exception ex)
        {
            var detail = ex.Message;
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    message = string.IsNullOrWhiteSpace(detail)
                        ? "An error occurred while processing the chat request."
                        : detail,
                    error = detail
                }
            );
        }
    }
}