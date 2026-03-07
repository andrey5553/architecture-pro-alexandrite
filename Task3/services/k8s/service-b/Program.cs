using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Diagnostics;

// Настройка OpenTelemetry
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder =>
    {
        tracerProviderBuilder
            .AddSource("ServiceB")
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("ServiceB"))
            .AddAspNetCoreInstrumentation()
            .AddJaegerExporter(options =>
            {
                options.AgentHost = Environment.GetEnvironmentVariable("JAEGER_AGENT_HOST") ?? "localhost";
                options.AgentPort = 6831;
            });
    });

var app = builder.Build();

var activitySource = new ActivitySource("ServiceB");

// Эндпоинт для получения случайного числа
app.MapGet("/api/random", (ILogger<Program> logger) =>
{
    using var activity = activitySource.StartActivity("GetRandomNumber");

    var random = new Random();
    var number = random.Next(1, 1000);

    activity?.SetTag("random.number", number);
    activity?.SetTag("random.generated_at", DateTime.UtcNow.ToString("O"));

    logger.LogInformation("Generated random number: {Number}", number);

    return Results.Ok(new { number = number, timestamp = DateTime.UtcNow });
})
.WithName("GetRandomNumber")
.WithOpenApi();

// Эндпоинт для проверки здоровья
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "ServiceB" }));

app.Run();
