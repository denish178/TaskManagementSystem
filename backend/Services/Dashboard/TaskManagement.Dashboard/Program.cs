using Microsoft.AspNetCore.Authentication.JwtBearer;
using TaskManagement.Dashboard.Interfaces;
using TaskManagement.Dashboard.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<IDashboardService, DashboardService>();

builder.Services.AddHttpClient();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "http://localhost:8080/realms/dotnet-training";
        options.Audience = "dotnet-api";
        options.RequireHttpsMetadata = false;
    });

builder.Services.AddAuthorization();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();