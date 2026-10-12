using Microsoft.EntityFrameworkCore;
using TaskManagement.Notification;
using TaskManagement.Notification.Data;
using TaskManagement.Notification.Hubs;
using TaskManagement.Shared.Auth;
using TaskManagement.Shared.Health;
using TaskManagement.Shared.SignalR;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddTaskManagementSignalR(builder.Configuration);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddTaskManagementJwtAuthentication(builder.Configuration);

var connectionString =
    builder.Configuration.GetConnectionString("NotificationDatabase")
    ?? throw new InvalidOperationException(
        "Notification database connection string is not configured.");

builder.Services.AddDbContext<NotificationDbContext>(options =>
    options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()));

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
    app.UseSwagger();
    app.UseSwaggerUI();
    await DevelopmentNotificationSeeder.SeedAsync(app.Services);
}

app.UseCors("FrontendPolicy");
app.UseAuthentication();
app.UseAuthorization();

app.MapTaskManagementHealth("notification");
app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");

app.Run();
