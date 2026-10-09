using Microsoft.Data.SqlClient;
using System.Text;
using System.Text.Json;

namespace TaskManagement.AI.Services;

public class DatabaseContextService
{
    private readonly string _connectionString;

    public DatabaseContextService(IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("AzureSql")
            ?? throw new InvalidOperationException(
                "Azure SQL connection string is not configured.");
    }

    public async Task<string> GetDatabaseContextAsync()
    {
        await using var connection =
            new SqlConnection(_connectionString);

        await connection.OpenAsync();

        var teams =
            await GetTeamsAsync(connection);

        var teamMembers =
            await GetTeamMembersAsync(connection);

        var projects =
            await GetProjectsAsync(connection);

        var sprints =
            await GetSprintsAsync(connection);

        var tasks =
            await GetTasksAsync(connection);

        var subTasks =
            await GetSubTasksAsync(connection);

        var database = new
        {
            Teams = teams,
            TeamMembers = teamMembers,
            Projects = projects,
            Sprints = sprints,
            Tasks = tasks,
            SubTasks = subTasks
        };

        return JsonSerializer.Serialize(
            database,
            new JsonSerializerOptions
            {
                WriteIndented = true
            });
    }

    private static async Task<List<Dictionary<string, object?>>>
        GetTeamsAsync(SqlConnection connection)
    {
        const string sql = """
            SELECT
                Id,
                Name,
                Description,
                CreatedBy,
                CreatedAt
            FROM Teams
            ORDER BY CreatedAt DESC;
            """;

        return await ExecuteQueryAsync(connection, sql);
    }

    private static async Task<List<Dictionary<string, object?>>>
        GetTeamMembersAsync(SqlConnection connection)
    {
        const string sql = """
            SELECT
                Id,
                TeamId,
                UserId,
                Role,
                JoinedAt
            FROM TeamMembers
            ORDER BY JoinedAt DESC;
            """;

        return await ExecuteQueryAsync(connection, sql);
    }

    private static async Task<List<Dictionary<string, object?>>>
        GetProjectsAsync(SqlConnection connection)
    {
        const string sql = """
            SELECT
                Id,
                TeamId,
                Name,
                Description,
                CreatedBy,
                CreatedAt
            FROM Projects
            ORDER BY CreatedAt DESC;
            """;

        return await ExecuteQueryAsync(connection, sql);
    }

    private static async Task<List<Dictionary<string, object?>>>
        GetSprintsAsync(SqlConnection connection)
    {
        const string sql = """
            SELECT
                Id,
                ProjectId,
                Name,
                Goal,
                StartDate,
                EndDate,
                CreatedAt
            FROM Sprints
            ORDER BY CreatedAt DESC;
            """;

        return await ExecuteQueryAsync(connection, sql);
    }

    private static async Task<List<Dictionary<string, object?>>>
        GetTasksAsync(SqlConnection connection)
    {
        const string sql = """
            SELECT
                Id,
                ProjectId,
                SprintId,
                Title,
                Description,
                Status,
                Priority,
                AssigneeId,
                CreatedBy,
                DueDate,
                CreatedAt,
                UpdatedAt
            FROM Tasks
            ORDER BY CreatedAt DESC;
            """;

        return await ExecuteQueryAsync(connection, sql);
    }

    private static async Task<List<Dictionary<string, object?>>>
        GetSubTasksAsync(SqlConnection connection)
    {
        const string sql = """
            SELECT
                Id,
                TaskId,
                Title,
                IsCompleted,
                CreatedAt
            FROM SubTasks
            ORDER BY CreatedAt DESC;
            """;

        return await ExecuteQueryAsync(connection, sql);
    }

    private static async Task<List<Dictionary<string, object?>>>
        ExecuteQueryAsync(
            SqlConnection connection,
            string sql)
    {
        var results =
            new List<Dictionary<string, object?>>();

        await using var command =
            new SqlCommand(sql, connection);

        await using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var row =
                new Dictionary<string, object?>();

            for (int i = 0;
                 i < reader.FieldCount;
                 i++)
            {
                row[reader.GetName(i)] =
                    reader.IsDBNull(i)
                        ? null
                        : reader.GetValue(i);
            }

            results.Add(row);
        }

        return results;
    }
}