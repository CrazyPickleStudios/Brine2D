using Brine2D.Engine;
using Brine2D.Hosting;
using DependencyInjection;
using DependencyInjection.Options;
using DependencyInjection.Services;
using Microsoft.Extensions.DependencyInjection;

var builder = GameApplication.CreateBuilder(args);

builder.Configure(options =>
{
    options.Window.Title  = "03 - Dependency Injection";
    options.Window.Width  = 1280;
    options.Window.Height = 720;
});

// Custom service
builder.Services.AddSingleton<IScoreService, ScoreService>();

// Game options; configure in code and register as a singleton
builder.Services.AddSingleton(new GameOptions
{
    PointsPerSecond = 50,
    PlayerName      = "Player"
});

builder.AddScene<GameScene>();

await using var game = builder.Build();
await game.RunAsync<GameScene>();