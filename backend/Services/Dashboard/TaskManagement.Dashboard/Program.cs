using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi;
using TaskManagement.Dashboard.Interfaces;
using TaskManagement.Dashboard.Services;

var builder = WebApplication.CreateBuilder(args);

// Register the dashboard business logic and HTTP client factory.
builder.Services.AddScoped<IDashboardService, DashboardService>();

builder.Services.AddHttpClient();

// Configure JWT authentication using the application's Keycloak realm.
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority =
            "http://localhost:8080/realms/dotnet-training";

        options.Audience = "dotnet-api";

        options.RequireHttpsMetadata = false;
    });

builder.Services.AddAuthorization();

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();

// Configure Swagger with Bearer token support for protected endpoints.
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token."
    });

    options.AddSecurityRequirement(document =>
        new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = []
        });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();