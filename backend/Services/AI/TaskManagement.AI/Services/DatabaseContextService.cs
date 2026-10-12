using System.Data;
using Microsoft.Data.SqlClient;
using System.Text.Json;
using TaskManagement.Shared.Auth;

namespace TaskManagement.AI.Services;

public class DatabaseContextService : IDatabaseContextService
{
    private const int MaxRowsPerSection = 100;

    private readonly string _projectConnectionString;
    private readonly string _taskConnectionString;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DatabaseContextService(
        IConfiguration configuration,
        IHttpContextAccessor httpContextAccessor)
    {
        var legacy = configuration.GetConnectionString("AzureSql");

        _projectConnectionString =
            configuration.GetConnectionString("ProjectDatabase")
            ?? legacy
            ?? throw new InvalidOperationException(
                "Project database connection string is not configured.");

        _taskConnectionString =
            configuration.GetConnectionString("TaskDatabase")
            ?? legacy
            ?? _projectConnectionString;

        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<string> GetDatabaseContextAsync()
    {
        var user = _httpContextAccessor.HttpContext?.User
            ?? throw new UnauthorizedAccessException("User is not authenticated.");

        var userId = CurrentUser.GetUserId(user);
        var isAdmin = CurrentUser.IsAdmin(user);
        var currentUser = new
        {
            UserId = userId,
            Email = CurrentUser.GetEmail(user),
            Name = CurrentUser.GetName(user),
            Role = CurrentUser.GetRole(user),
            IsAdmin = isAdmin,
        };

        await using var projectConnection =
            new SqlConnection(_projectConnectionString);
        await projectConnection.OpenAsync();

        var teams = await GetTeamsAsync(projectConnection, userId, isAdmin);
        var teamMembers = await GetTeamMembersAsync(projectConnection, userId, isAdmin);
        var projects = await GetProjectsAsync(projectConnection, userId, isAdmin);
        var sprints = await GetSprintsAsync(projectConnection, userId, isAdmin);

        await using var taskConnection =
            new SqlConnection(_taskConnectionString);
        await taskConnection.OpenAsync();

        var scopedProjectIds = ExtractGuidIds(projects, "Id");
        var tasks = await GetTasksAsync(
            taskConnection,
            userId,
            isAdmin,
            scopedProjectIds);
        var subTasks = await GetSubTasksAsync(
            taskConnection,
            userId,
            isAdmin,
            scopedProjectIds);

        var database = new
        {
            CurrentUser = currentUser,
            ScopeNote =
                isAdmin
                    ? "Admin view: all accessible organization data (capped per section)."
                    : "Scoped to teams you belong to, plus tasks you created or are assigned to.",
            Teams = teams,
            TeamMembers = teamMembers,
            Projects = projects,
            Sprints = sprints,
            Tasks = tasks,
            SubTasks = subTasks,
        };

        return JsonSerializer.Serialize(
            database,
            new JsonSerializerOptions { WriteIndented = true });
    }

    private static async Task<List<Dictionary<string, object?>>> GetTeamsAsync(
        SqlConnection connection,
        Guid userId,
        bool isAdmin)
    {
        const string sql = """
            SELECT TOP (@maxRows)
                Id, Name, Description, CreatedBy, CreatedAt
            FROM Teams
            WHERE (@isAdmin = 1 OR Id IN (
                SELECT TeamId FROM TeamMembers WHERE UserId = @userId))
            ORDER BY CreatedAt DESC;
            """;

        return await ExecuteQueryAsync(connection, sql, userId, isAdmin);
    }

    private static async Task<List<Dictionary<string, object?>>> GetTeamMembersAsync(
        SqlConnection connection,
        Guid userId,
        bool isAdmin)
    {
        const string sql = """
            SELECT TOP (@maxRows)
                tm.Id, tm.TeamId, tm.UserId, tm.Role, tm.JoinedAt
            FROM TeamMembers tm
            WHERE (@isAdmin = 1 OR tm.TeamId IN (
                SELECT TeamId FROM TeamMembers WHERE UserId = @userId))
            ORDER BY tm.JoinedAt DESC;
            """;

        return await ExecuteQueryAsync(connection, sql, userId, isAdmin);
    }

    private static async Task<List<Dictionary<string, object?>>> GetProjectsAsync(
        SqlConnection connection,
        Guid userId,
        bool isAdmin)
    {
        const string sql = """
            SELECT TOP (@maxRows)
                p.Id, p.TeamId, p.Name, p.Description, p.CreatedBy, p.CreatedAt
            FROM Projects p
            WHERE (@isAdmin = 1 OR p.TeamId IN (
                SELECT TeamId FROM TeamMembers WHERE UserId = @userId))
            ORDER BY p.CreatedAt DESC;
            """;

        return await ExecuteQueryAsync(connection, sql, userId, isAdmin);
    }

    private static async Task<List<Dictionary<string, object?>>> GetSprintsAsync(
        SqlConnection connection,
        Guid userId,
        bool isAdmin)
    {
        const string sql = """
            SELECT TOP (@maxRows)
                s.Id, s.ProjectId, s.Name, s.Goal, s.StartDate, s.EndDate, s.CreatedAt
            FROM Sprints s
            WHERE (@isAdmin = 1 OR s.ProjectId IN (
                SELECT p.Id FROM Projects p
                INNER JOIN TeamMembers tm ON tm.TeamId = p.TeamId
                WHERE tm.UserId = @userId))
            ORDER BY s.CreatedAt DESC;
            """;

        return await ExecuteQueryAsync(connection, sql, userId, isAdmin);
    }

    private static async Task<List<Dictionary<string, object?>>> GetTasksAsync(
        SqlConnection connection,
        Guid userId,
        bool isAdmin,
        IReadOnlyList<Guid> scopedProjectIds)
    {
        var sql = """
            SELECT TOP (@maxRows)
                t.Id, t.ProjectId, t.SprintId, t.Title, t.Description, t.Status,
                t.Priority, t.AssigneeId, t.CreatedBy, t.DueDate, t.CreatedAt, t.UpdatedAt
            FROM Tasks t
            WHERE (@isAdmin = 1
                OR t.AssigneeId = @userId
                OR t.CreatedBy = @userId
            """;

        if (scopedProjectIds.Count > 0)
        {
            sql += " OR t.ProjectId IN (" + BuildGuidInList(scopedProjectIds) + ")";
        }

        sql += """
            )
            ORDER BY t.CreatedAt DESC;
            """;

        return await ExecuteQueryAsync(connection, sql, userId, isAdmin);
    }

    private static async Task<List<Dictionary<string, object?>>> GetSubTasksAsync(
        SqlConnection connection,
        Guid userId,
        bool isAdmin,
        IReadOnlyList<Guid> scopedProjectIds)
    {
        var taskFilter = """
            SELECT t.Id FROM Tasks t
            WHERE (@isAdmin = 1
                OR t.AssigneeId = @userId
                OR t.CreatedBy = @userId
            """;

        if (scopedProjectIds.Count > 0)
        {
            taskFilter += " OR t.ProjectId IN (" + BuildGuidInList(scopedProjectIds) + ")";
        }

        taskFilter += ")";

        var sql = $"""
            SELECT TOP (@maxRows)
                st.Id, st.TaskId, st.Title, st.IsCompleted, st.CreatedAt
            FROM SubTasks st
            WHERE st.TaskId IN ({taskFilter})
            ORDER BY st.CreatedAt DESC;
            """;

        return await ExecuteQueryAsync(connection, sql, userId, isAdmin);
    }

    private static List<Guid> ExtractGuidIds(
        List<Dictionary<string, object?>> rows,
        string key)
    {
        var ids = new List<Guid>();
        foreach (var row in rows)
        {
            if (!row.TryGetValue(key, out var value) || value is null)
            {
                continue;
            }

            if (value is Guid guid)
            {
                ids.Add(guid);
            }
            else if (Guid.TryParse(value.ToString(), out var parsed))
            {
                ids.Add(parsed);
            }
        }

        return ids;
    }

    private static string BuildGuidInList(IReadOnlyList<Guid> ids)
    {
        return string.Join(", ", ids.Select(id => $"'{id:D}'"));
    }

    private static async Task<List<Dictionary<string, object?>>> ExecuteQueryAsync(
        SqlConnection connection,
        string sql,
        Guid userId,
        bool isAdmin)
    {
        var results = new List<Dictionary<string, object?>>();

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@userId", SqlDbType.UniqueIdentifier).Value = userId;
        command.Parameters.Add("@isAdmin", SqlDbType.Bit).Value = isAdmin;
        command.Parameters.Add("@maxRows", SqlDbType.Int).Value = MaxRowsPerSection;

        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var row = new Dictionary<string, object?>();

            for (int i = 0; i < reader.FieldCount; i++)
            {
                row[reader.GetName(i)] =
                    reader.IsDBNull(i) ? null : reader.GetValue(i);
            }

            results.Add(row);
        }

        return results;
    }
}
