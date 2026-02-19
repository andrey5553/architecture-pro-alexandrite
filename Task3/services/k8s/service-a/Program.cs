using System.Text.Json;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(8080);
});

builder.Services.AddHttpClient("ServiceB", client =>
{
    var serviceBUrl = Environment.GetEnvironmentVariable("SERVICE_B_URL") ?? "http://service-b:8080";
    client.BaseAddress = new Uri(serviceBUrl);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// Добавляем явные настройки JSON
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "ServiceA" }));

app.MapGet("/api/call-random", async (IHttpClientFactory httpClientFactory, ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("Calling ServiceB...");

        var client = httpClientFactory.CreateClient("ServiceB");
        var response = await client.GetAsync("/api/random");

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            logger.LogInformation("Raw response from ServiceB: {Content}", content);

            // Явная десериализация с настройками
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var result = JsonSerializer.Deserialize<RandomNumberResponse>(content, options);

            logger.LogInformation("Deserialized: Number={Number}, Timestamp={Timestamp}",
                result?.Number, result?.Timestamp);

            return Results.Ok(new
            {
                message = "Successfully called ServiceB",
                data = result,
                trace_id = Activity.Current?.TraceId.ToString()
            });
        }

        return Results.Problem($"ServiceB returned {response.StatusCode}");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error calling ServiceB");
        return Results.Problem(ex.Message);
    }
});

app.Run();

// Используем record с правильными именами свойств
public record RandomNumberResponse(int Number, DateTime Timestamp);