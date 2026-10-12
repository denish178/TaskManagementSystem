namespace TaskManagement.AI.Services;

public interface IDatabaseContextService
{
    Task<string> GetDatabaseContextAsync();
}
