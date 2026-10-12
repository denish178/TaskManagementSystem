using System.Text;
using System.Text.Json;

namespace TaskManagement.AI.Services;

public class GeminiService : IGeminiService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model;
    private readonly string _fallbackModel;

    public GeminiService(
        HttpClient httpClient,
        IConfiguration configuration)
    {
        _httpClient = httpClient;

        _apiKey = configuration["Gemini:ApiKey"] ?? "";

        _model =
            configuration["Gemini:Model"]
            ?? "gemini-3.6-flash";

        _fallbackModel =
            configuration["Gemini:FallbackModel"]
            ?? "gemini-3.1-flash-lite";
    }

    public async Task<string> AskAsync(
        string question,
        string databaseContext)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            throw new ArgumentException(
                "Question cannot be empty.",
                nameof(question));
        }

        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            throw new InvalidOperationException(
                "Gemini API key is not configured. Set Gemini:ApiKey in user secrets or the Gemini__ApiKey environment variable.");
        }

        var prompt = $"""
            You are an AI assistant for a Task Management System.

            Your job is to answer the user's questions using ONLY
            the database information provided below.

            DATABASE INFORMATION:
            {databaseContext}

            USER QUESTION:
            {question}

            RULES:

            1. Use only the supplied database information.
            2. Do not invent users, tasks, projects, teams or numbers.
            3. If the requested information is not available,
               clearly say that the information is not available.
            4. You are a read-only assistant.
            5. Do not claim that you created, deleted or modified anything.
            6. You may calculate simple statistics from the supplied data.
            7. Keep answers clear and concise.
            8. Use bullet points when listing multiple items.
            9. The JSON includes CurrentUser (UserId, Email, Name, Role).
               When the user says "I", "me", "my", or "mine", interpret that as
               CurrentUser.UserId: match Tasks where AssigneeId or CreatedBy equals
               that id unless they clearly mean something else.
            10. For the current user you may use CurrentUser.Name or Email in answers.
                For other people, only use AssigneeId/CreatedBy ids from the data;
                do not invent names.
            11. The task statuses used by this system are:
                Todo, InProgress, Testing, Done.
                "Completed" usually means Status is Done.
            12. Do not use information from outside the supplied database context.
            """;

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new
                        {
                            text = prompt
                        }
                    }
                }
            },
            generationConfig = new
            {
                temperature = 0.2
            }
        };

        var json = JsonSerializer.Serialize(requestBody);

        // First try the configured model, then fallback (overload or retired id).
        try
        {
            return await SendToGeminiAsync(_model, json);
        }
        catch (Exception ex) when (ShouldTryFallbackModel(ex))
        {
            if (string.Equals(_model, _fallbackModel, StringComparison.OrdinalIgnoreCase))
            {
                throw;
            }

            return await SendToGeminiAsync(_fallbackModel, json);
        }
    }

    private static bool ShouldTryFallbackModel(Exception ex)
    {
        if (ex is GeminiTemporaryException)
        {
            return true;
        }

        var text = ex.Message;
        return text.Contains("404", StringComparison.Ordinal)
            || text.Contains("NOT_FOUND", StringComparison.OrdinalIgnoreCase)
            || text.Contains("is not found", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<string> SendToGeminiAsync(
        string model,
        string json)
    {
        const int maxAttempts = 3;

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent");

            request.Headers.Add(
                "x-goog-api-key",
                _apiKey);

            request.Content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json");

            HttpResponseMessage response;

            try
            {
                response = await _httpClient.SendAsync(request);
            }
            catch (HttpRequestException ex)
            {
                if (attempt < maxAttempts)
                {
                    await WaitBeforeRetryAsync(attempt);
                    continue;
                }

                throw new InvalidOperationException(
                    "Unable to connect to Gemini API.",
                    ex);
            }

            var responseBody =
                await response.Content.ReadAsStringAsync();

            // Successful response
            if (response.IsSuccessStatusCode)
            {
                return ExtractAnswer(responseBody);
            }

            int statusCode = (int)response.StatusCode;

            // Temporary errors:
            // 408 = Request Timeout
            // 429 = Too Many Requests
            // 500 = Internal Server Error
            // 502 = Bad Gateway
            // 503 = Service Unavailable
            // 504 = Gateway Timeout
            bool temporaryError =
                statusCode == 408 ||
                statusCode == 429 ||
                statusCode == 500 ||
                statusCode == 502 ||
                statusCode == 503 ||
                statusCode == 504;

            if (temporaryError && attempt < maxAttempts)
            {
                await WaitBeforeRetryAsync(attempt);
                continue;
            }

            // 503 after all retries means the model is probably
            // temporarily overloaded.
            if (statusCode == 503)
            {
                throw new GeminiTemporaryException(
                    $"Gemini model '{model}' is temporarily unavailable. " +
                    $"Response: {responseBody}");
            }

            // Other Gemini errors
            throw new InvalidOperationException(
                $"Gemini API error: {response.StatusCode}. " +
                $"Response: {responseBody}");
        }

        throw new InvalidOperationException(
            "Gemini request failed after all retry attempts.");
    }

    private static async Task WaitBeforeRetryAsync(
        int attempt)
    {
        // Exponential backoff:
        //
        // Attempt 1 -> 2 seconds
        // Attempt 2 -> 4 seconds
        //
        // This prevents sending requests repeatedly
        // while Gemini is temporarily overloaded.

        int delaySeconds =
            (int)Math.Pow(2, attempt);

        await Task.Delay(
            TimeSpan.FromSeconds(delaySeconds));
    }

    private static string ExtractAnswer(
        string responseBody)
    {
        try
        {
            using var document =
                JsonDocument.Parse(responseBody);

            var root =
                document.RootElement;

            var candidates =
                root.GetProperty("candidates");

            if (candidates.GetArrayLength() == 0)
            {
                throw new InvalidOperationException(
                    "Gemini returned no candidates.");
            }

            var answer =
                candidates[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

            if (string.IsNullOrWhiteSpace(answer))
            {
                throw new InvalidOperationException(
                    "Gemini returned an empty response.");
            }

            return answer;
        }
        catch (KeyNotFoundException ex)
        {
            throw new InvalidOperationException(
                "Unexpected Gemini response format.",
                ex);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                "Could not parse Gemini response.",
                ex);
        }
    }

    private class GeminiTemporaryException
        : Exception
    {
        public GeminiTemporaryException(
            string message)
            : base(message)
        {
        }
    }
}