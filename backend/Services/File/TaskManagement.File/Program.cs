using Microsoft.EntityFrameworkCore;
using TaskManagement.File;
using TaskManagement.File.Data;
using TaskManagement.File.Interfaces;
using TaskManagement.File.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
builder.Services.AddDbContext<FileDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("FileDb")));

// Azure Blob
builder.Services.Configure<AzureBlobSettings>(
    builder.Configuration.GetSection("AzureBlobStorage"));

builder.Services.AddSingleton<IBlobStorageService, BlobStorageService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.MapControllers();

app.Run();