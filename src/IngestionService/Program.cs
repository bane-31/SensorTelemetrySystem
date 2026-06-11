using IngestionService.Middleware;
using IngestionService.Persistence;
using IngestionService.Services;
using IngestionService.Services.Interfaces;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRateLimiter(options =>
{
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        var cache = context.HttpContext.RequestServices.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>();
        var sensorIdHeader = context.HttpContext.Request.Headers["X-Sensor-Id"].ToString();

        if (Guid.TryParse(sensorIdHeader, out Guid sensorId))
        {
            var dbContext = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();

            var sensor = await dbContext.Sensors.FindAsync(new object[] { sensorId }, cancellationToken);

            if (sensor != null && !sensor.IsBlocked)
            {
                sensor.IsBlocked = true;
                sensor.BlockedUntil = DateTime.UtcNow.AddSeconds(30);
                await dbContext.SaveChangesAsync(cancellationToken);

                cache.Set($"BlockedSensor_{sensorId}", true, TimeSpan.FromSeconds(30));
                
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine($"[SECURITY ALARM] Sensor {sensorId} detected as malicious (DoS)! Automatically blocked for 30s.");
                Console.ResetColor();
            }
        }
    };

    options.AddPolicy("sensor_limiter", httpContext =>
    {
        var sensorIdHeader = httpContext.Request.Headers["X-Sensor-Id"].ToString();

        var partitionKey = string.IsNullOrEmpty(sensorIdHeader)
            ? httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"
            : sensorIdHeader;

        return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ =>
            new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromSeconds(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
    });
});
builder.Services.AddControllers();
builder.Services.AddMemoryCache();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<ISensorService, SensorService>();
builder.Services.AddScoped<IMetricService, MetricService>();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();



var app = builder.Build();

app.UseExceptionHandler();

app.Use(async (context, next) =>
{
    if (context.Request.Headers.TryGetValue("X-Sensor-Id", out var sensorIdValue) &&
        Guid.TryParse(sensorIdValue, out Guid sensorId))
    {
        var cache = context.RequestServices.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>();

        if (cache.TryGetValue($"BlockedSensor_{sensorId}", out _))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { Error = "Senzor je u karantinu." });
            return;
        }
    }

    await next();
});
app.UseRateLimiter();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
