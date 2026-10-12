using TaskManagement.AI.Services;
using TaskManagement.Shared.Auth;
using TaskManagement.Shared.Health;
using TaskManagement.Shared.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpContextAccessor();
builder.Services.AddTaskManagementJwtAuthentication(builder.Configuration);
builder.Services.AddTaskManagementRateLimiting();
builder.Services.AddScoped<IDatabaseContextService, DatabaseContextService>();
builder.Services.AddHttpClient<IGeminiService, GeminiService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHttpsRedirection();
}

app.UseTaskManagementRateLimiting();
app.UseAuthentication();
app.UseAuthorization();

app.MapTaskManagementHealth("ai");
app.MapControllers();

app.Run();
