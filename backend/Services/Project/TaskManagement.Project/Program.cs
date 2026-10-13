using Microsoft.EntityFrameworkCore;
using TaskManagement.Project.Data;
using TaskManagement.Project.Services;
using System.Text.Json.Serialization;
using TaskManagement.Shared.Auth;
using TaskManagement.Shared.Health;

var builder = WebApplication.CreateBuilder(args);

// ----------------------------------------------------
// Controllers + JSON settings
// ----------------------------------------------------

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Prevent circular reference errors when
        // Project -> Team -> Projects -> Project...
        // relationships are serialized.
        options.JsonSerializerOptions.ReferenceHandler =
            ReferenceHandler.IgnoreCycles;
    });

// ----------------------------------------------------
// Swagger
// ----------------------------------------------------

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ----------------------------------------------------
// Azure SQL
// ----------------------------------------------------

var connectionString =
    builder.Configuration.GetConnectionString("AzureSql");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Azure SQL connection string is not configured.");
}

builder.Services.AddDbContext<ProjectDbContext>(options =>
{
    options.UseSqlServer(
        connectionString,
        sqlOptions =>
        {
            sqlOptions.MigrationsHistoryTable(
                "__ProjectMigrationsHistory");

            sqlOptions.EnableRetryOnFailure();
        });
});
builder.Services.AddTaskManagementJwtAuthentication(builder.Configuration);
builder.Services.AddHttpContextAccessor();

var taskServiceUrl =
    builder.Configuration["ServiceUrls:Task"]
    ?? "http://localhost:5266";

builder.Services.AddHttpClient<ITaskProjectAccessClient, TaskProjectAccessClient>(client =>
{
    client.BaseAddress = new Uri(taskServiceUrl);
});
builder.Services.AddScoped<ProjectAccessService>();

// ----------------------------------------------------
// CORS
// ----------------------------------------------------

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// ----------------------------------------------------
// Build application
// ----------------------------------------------------

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();
    await db.Database.MigrateAsync();
}

// ----------------------------------------------------
// Swagger
// ----------------------------------------------------

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ----------------------------------------------------
// Middleware
// ----------------------------------------------------

app.UseHttpsRedirection();

app.UseCors("FrontendPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapTaskManagementHealth("project");
app.MapControllers();

app.Run();