using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using TaskManagement.Auth;
using TaskManagement.Auth.Data;
using TaskManagement.Shared.Auth;
using TaskManagement.Shared.Health;
using TaskManagement.Shared.RateLimiting;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var authConnectionString =
    builder.Configuration.GetConnectionString("AuthDatabase")
    ?? throw new InvalidOperationException(
        "Auth database connection string is not configured.");

var sqlConnectionStringBuilder =
    new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(authConnectionString);

if (!builder.Environment.IsDevelopment())
{
    sqlConnectionStringBuilder.MultiSubnetFailover = true;
}

builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseSqlServer(
        sqlConnectionStringBuilder.ConnectionString,
        sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure();
        }));

builder.Services.AddTaskManagementJwtAuthentication(builder.Configuration);
builder.Services.AddTaskManagementRateLimiting();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "JWT Authorization header using the Bearer scheme."
    });

    options.AddSecurityRequirement(document =>
        new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = []
        });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    await DevelopmentDataSeeder.SeedAsync(app.Services);
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseTaskManagementRateLimiting();
app.UseAuthentication();
app.UseAuthorization();

app.MapTaskManagementHealth("auth");
app.MapControllers();

app.Run();