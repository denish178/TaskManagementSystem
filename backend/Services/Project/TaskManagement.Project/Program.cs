using Microsoft.EntityFrameworkCore;
using TaskManagement.Project.Data;
using System.Text.Json.Serialization;

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
        });
});

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

app.MapControllers();

app.Run();