using Microsoft.EntityFrameworkCore;
using TaskManagement.File;
using TaskManagement.File.Data;
using TaskManagement.File.Interfaces;
using TaskManagement.File.Services;
using Microsoft.OpenApi;
using TaskManagement.Shared.Auth;
using TaskManagement.Shared.Health;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddTaskManagementJwtAuthentication(builder.Configuration);

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

// Database
builder.Services.AddDbContext<FileDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("FileDb"),
        sql => sql.EnableRetryOnFailure()));

builder.Services.Configure<AzureBlobSettings>(
    builder.Configuration.GetSection("AzureBlobStorage"));
builder.Services.Configure<LocalFileStorageSettings>(
    builder.Configuration.GetSection("LocalFileStorage"));

var storageMode = builder.Configuration["FileStorage:Mode"] ?? "Azure";
var blobConnection = builder.Configuration["AzureBlobStorage:ConnectionString"];

if (builder.Environment.IsDevelopment()
    && (storageMode.Equals("Local", StringComparison.OrdinalIgnoreCase)
        || string.IsNullOrWhiteSpace(blobConnection)))
{
    builder.Services.AddSingleton<IBlobStorageService, LocalBlobStorageService>();
}
else
{
    builder.Services.AddSingleton<IBlobStorageService, BlobStorageService>();
}

builder.Services.AddHttpContextAccessor();

var taskServiceUrl =
    builder.Configuration["ServiceUrls:Task"]
    ?? "http://localhost:5266";

builder.Services.AddHttpClient<ITaskAccessClient, TaskAccessClient>(client =>
{
    client.BaseAddress = new Uri(taskServiceUrl);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<FileDbContext>();
    await db.Database.MigrateAsync();
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapTaskManagementHealth("file");
app.MapControllers();

app.Run();