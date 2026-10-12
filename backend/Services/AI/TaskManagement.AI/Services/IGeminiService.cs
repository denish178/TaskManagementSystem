namespace TaskManagement.AI.Services;

public interface IGeminiService
{
    Task<string> AskAsync(string question, string databaseContext);
}
