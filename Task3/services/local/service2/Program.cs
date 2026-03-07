var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Эндпоинт для получения случайного числа
app.MapGet("/api/random", () =>
{
    var random = new Random();
    var number = random.Next(1, 1000);
    return Results.Ok(new { number = number, timestamp = DateTime.UtcNow });
})
.WithName("GetRandomNumber")
.WithOpenApi();

// Эндпоинт для проверки здоровья
app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));

app.Run();
