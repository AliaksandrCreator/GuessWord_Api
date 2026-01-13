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

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlite($"Data Source={Configuration.DbPath}"));
builder.Services.AddScoped<GameService>();

var app = builder.Build();

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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.MapPost("/start", async (GameService service, [FromQuery] string user) =>
{
    var id = await service.StartNewGameAsync(user);
    return Results.Ok($"Новая игра начата. ID сессии: {id}");
});

app.MapPost("/guess", async (GameService service, [FromQuery] char letter, [FromQuery] long id) =>
{
    var result = await service.GuessAsync(id, letter);
    return Results.Ok(result);
});

app.MapGet("/statistics", async (GameService service, [FromQuery] string? status, [FromQuery] string? user) =>
{
    var stats = await service.GetStatisticsAsync(status, user);
    return Results.Ok(stats);
});

app.MapDelete("/user", async (GameService service, [FromQuery] string user) =>
{
    var result = await service.DeleteUserAsync(user);
    return Results.Ok(result);
});

app.Run();

// WebApplicationFactory
namespace GuessWord.Api
{
    public partial class Program { } 
}


