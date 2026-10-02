using Microsoft.EntityFrameworkCore;
using TaskManagement.File;
using TaskManagement.File.Data;
using TaskManagement.File.Interfaces;
using TaskManagement.File.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddControllers();

builder.Services.AddDbContext<FileDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("FileDb")));

builder.Services.Configure<AzureBlobSettings>(
    builder.Configuration.GetSection("AzureBlobStorage"));

builder.Services.AddSingleton<IBlobStorageService, BlobStorageService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();