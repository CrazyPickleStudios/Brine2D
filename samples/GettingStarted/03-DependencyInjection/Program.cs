using Brine2D.Engine;
using Brine2D.Hosting;
using Brine2D.SDL;
using DependencyInjection;
using DependencyInjection.Options;
using DependencyInjection.Services;
using Microsoft.Extensions.DependencyInjection;

// Create builder
var builder = GameApplication.CreateBuilder(args);

// Add Brine2D services (just like ASP.NET!)
builder.Services.AddBrine2D(options =>
{
    options.Window.Title = "03 - Dependency Injection";
    options.Window.Width = 1280;
    options.Window.Height = 720;
});

// Register custom services (just like ASP.NET!)
// Singleton: One instance for the entire application lifetime
builder.Services.AddSingleton<IScoreService, ScoreService>();

// Transient: New instance every time it's requested
// builder.Services.AddTransient<IScoreService, ScoreService>();

// Scoped: One instance per scene (not commonly used in games, but available)
// builder.Services.AddScoped<IScoreService, ScoreService>();

// Bind configuration to strongly-typed options (just like ASP.NET!)
builder.Services.Configure<GameOptions>(
    builder.Configuration.GetSection("Game"));

// Register scene
builder.Services.AddScene<GameScene>();

// Build and run
var game = builder.Build();
await game.RunAsync<GameScene>();