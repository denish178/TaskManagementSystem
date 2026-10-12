using Microsoft.OpenApi;
using MongoDB.Driver;
using TaskManagement.Comment.Hubs;
using TaskManagement.Comment.Repositories;
using TaskManagement.Shared.Auth;
using TaskManagement.Shared.Health;
using TaskManagement.Shared.SignalR;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddTaskManagementSignalR(builder.Configuration);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
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

builder.Services.AddTaskManagementJwtAuthentication(builder.Configuration);

var mongoConnectionString = builder.Configuration["MongoDb:ConnectionString"]
    ?? throw new InvalidOperationException(
        "MongoDB connection string is not configured.");

var mongoDatabaseName = builder.Configuration["MongoDb:DatabaseName"]
    ?? throw new InvalidOperationException(
        "MongoDB database name is not configured.");

var mongoClient = new MongoClient(mongoConnectionString);

builder.Services.AddSingleton<IMongoDatabase>(
    mongoClient.GetDatabase(mongoDatabaseName));

builder.Services.AddScoped<CommentRepository>();

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
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("FrontendPolicy");
app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthentication();
app.UseAuthorization();

app.MapTaskManagementHealth("comment");
app.MapControllers();
app.MapHub<CommentHub>("/hubs/comments");

app.Run();
