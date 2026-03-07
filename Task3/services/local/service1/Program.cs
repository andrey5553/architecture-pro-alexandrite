using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

var serviceName = "Service1";
var serviceVersion = "1.0.0";

// Настройка OpenTelemetry
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .SetResourceBuilder(ResourceBuilder.CreateDefault()
            .AddService(serviceName: serviceName, serviceVersion: serviceVersion))
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddJaegerExporter(options =>
        {
            // Используем HTTP протокол вместо UDP
            options.Endpoint = new Uri("http://localhost:14268/api/traces");
            options.Protocol = OpenTelemetry.Exporter.JaegerExportProtocol.HttpBinaryThrift;
            options.ExportProcessorType = OpenTelemetry.ExportProcessorType.Batch;
            options.BatchExportProcessorOptions = new OpenTelemetry.BatchExportProcessorOptions<Activity>
            {
                MaxQueueSize = 100,
                ScheduledDelayMilliseconds = 1000, // Отправка каждую секунду
                ExporterTimeoutMilliseconds = 30000,
                MaxExportBatchSize = 100
            };
        })
        .AddConsoleExporter()) // Для отладки
    .WithMetrics(metrics => metrics
        .SetResourceBuilder(ResourceBuilder.CreateDefault()
            .AddService(serviceName: serviceName, serviceVersion: serviceVersion))
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddPrometheusExporter());

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Регистрируем HttpClient для Service2
builder.Services.AddHttpClient("Service2", client =>
{
    client.BaseAddress = new Uri("http://localhost:5002");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseOpenTelemetryPrometheusScrapingEndpoint();

// Эндпоинт для получения данных от Service2
app.MapGet("/api/get-random-from-service2", async (IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient("Service2");

    try
    {
        var response = await client.GetAsync("/api/random");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();

        return Results.Ok(new
        {
            message = "Successfully fetched from Service2",
            data = content,
            timestamp = DateTime.UtcNow,
            traceId = Activity.Current?.TraceId.ToString(),
            spanId = Activity.Current?.SpanId.ToString()
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(
            title: "Error fetching from Service2",
            detail: ex.Message,
            statusCode: 500
        );
    }
});

// Эндпоинт для проверки подключения к Jaeger
app.MapGet("/api/test-jaeger", async () =>
{
    var results = new List<object>();

    // Создаем несколько активностей для проверки
    for (int i = 0; i < 3; i++)
    {
        using var activity = new ActivitySource("ManualTest").StartActivity($"TestOperation-{i}");
        activity?.SetTag("test.iteration", i);
        activity?.SetTag("test.timestamp", DateTime.UtcNow.ToString("o"));

        results.Add(new
        {
            iteration = i,
            traceId = Activity.Current?.TraceId.ToString(),
            spanId = activity?.SpanId.ToString()
        });

        await Task.Delay(100);
    }

    return Results.Ok(new
    {
        message = "Jaeger test completed",
        results = results,
        currentActivity = Activity.Current != null ? new
        {
            traceId = Activity.Current.TraceId.ToString(),
            spanId = Activity.Current.SpanId.ToString()
        } : null
    });
});

app.Run();
