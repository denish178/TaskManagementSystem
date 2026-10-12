using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using TaskManagement.Task.Data;
using TaskManagement.Task.Hubs;
using TaskManagement.Task.Services;
using TaskManagement.Shared.Auth;
using TaskManagement.Shared.Health;
using TaskManagement.Shared.SignalR;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddTaskManagementSignalR(builder.Configuration);

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter JWT token: Bearer {your JWT token}"
    });

    options.AddSecurityRequirement(document =>
        new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] =
                new List<string>()
        });
});

var connectionString =
    builder.Configuration.GetConnectionString("AzureSql");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Azure SQL connection string is not configured.");
}

var sqlConnectionStringBuilder =
    new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString);

if (!builder.Environment.IsDevelopment())
{
    sqlConnectionStringBuilder.MultiSubnetFailover = true;
}

builder.Services.AddDbContext<TaskDbContext>(options =>
{
    options.UseSqlServer(
        sqlConnectionStringBuilder.ConnectionString,
        sqlOptions =>
        {
            sqlOptions.MigrationsHistoryTable("__TaskMigrationsHistory");
            sqlOptions.EnableRetryOnFailure();
        });
});

builder.Services.AddTaskManagementJwtAuthentication(builder.Configuration);
builder.Services.AddHttpContextAccessor();

var projectServiceUrl =
    builder.Configuration["ServiceUrls:Project"]
    ?? "http://localhost:5012";

builder.Services.AddHttpClient<IProjectAccessClient, ProjectAccessClient>(client =>
{
    client.BaseAddress = new Uri(projectServiceUrl);
});
builder.Services.AddScoped<TaskAuthorizationService>();
builder.Services.AddScoped<ITaskAuditService, TaskAuditService>();
builder.Services.AddScoped<ITaskRealtimeNotifier, TaskRealtimeNotifier>();

var notificationServiceUrl =
    builder.Configuration["ServiceUrls:Notification"]
    ?? "http://localhost:5290";

builder.Services.AddHttpClient<INotificationClient, NotificationClient>(client =>
{
    client.BaseAddress = new Uri(notificationServiceUrl);
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5173",
                "http://localhost:5204")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<TaskDbContext>();
    await db.Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("FrontendPolicy");
app.UseAuthentication();
app.UseAuthorization();

app.MapTaskManagementHealth("task");
app.MapControllers();
app.MapHub<TaskHub>("/hubs/tasks");

app.Run();
