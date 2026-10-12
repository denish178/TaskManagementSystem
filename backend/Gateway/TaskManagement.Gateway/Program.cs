
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddOcelot(builder.Configuration);

var app = builder.Build();

app.Use(async (context, next) =>
{
    var path = context.Request.Path;
    if (path.Equals("/health", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.StatusCode = StatusCodes.Status200OK;
        await context.Response.WriteAsJsonAsync(new { status = "healthy", service = "gateway" });
        return;
    }

    if (path.Equals("/", StringComparison.Ordinal) || path.Value is "/" or "")
    {
        context.Response.StatusCode = StatusCodes.Status200OK;
        await context.Response.WriteAsJsonAsync(new
        {
            service = "gateway",
            status = "running",
            health = "/health",
            api = "/api/* (proxied to microservices)",
            frontend = "http://localhost:5173"
        });
        return;
    }

    await next();
});

app.UseCors("FrontendPolicy");
app.UseWebSockets();

await app.UseOcelot();

app.Run();
