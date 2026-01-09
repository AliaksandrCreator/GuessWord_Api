using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.OpenApi.Models;
using System;
using Microsoft.AspNetCore.Mvc;

Directory.CreateDirectory(Configuration.DbDirectory);

var builder = WebApplication.CreateBuilder(args);

// Регистрация Swagger-сервисов
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "WordGame API",
        Version = "v1",
        Description = "Минимальный API для игры в угадывание слов"
    });
});

// Регистрация EF Core и сервисов
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlite($"Data Source={Configuration.DbPath}"));
builder.Services.AddScoped<GameService>();

var app = builder.Build();

// Инициализация базы данных при отсутствии файла
using (var scope = app.Services.CreateScope())
{
    var dbFile = Configuration.DbPath;

    // Проверка: если файл базы не существует — создаём структуру
    if (!File.Exists(dbFile))
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated(); // создаёт таблицы по моделям
    }
} 

// Включение Swagger UI в режиме разработки
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Инициализация базы данных
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

// Старт новой игры с указанием имени пользователя
app.MapPost("/start", async (GameService service, [FromQuery] string user) =>
{
    var id = await service.StartNewGameAsync(user);
    return Results.Ok($"Новая игра начата. ID сессии: {id}");
});

// Попытка угадать букву
app.MapPost("/guess", async (GameService service, [FromQuery] char letter, [FromQuery] long id) =>
{
    var result = await service.GuessAsync(id, letter);
    return Results.Ok(result);
});

// Статистика по статусу и имени пользователя
app.MapGet("/statistics", async (GameService service, [FromQuery] string? status, [FromQuery] string? user) =>
{
    var stats = await service.GetStatisticsAsync(status, user);
    return Results.Ok(stats);
});

// Удаление пользователя и всех его игр
app.MapDelete("/user", async (GameService service, [FromQuery] string user) =>
{
    var result = await service.DeleteUserAsync(user);
    return Results.Ok(result);
});


app.Run();

// !!!нужен для WebApplicationFactory!!!
namespace GuessWord.Api
{
    public partial class Program { } 
}


