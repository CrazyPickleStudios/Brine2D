using BasicGame;
using Brine2D.Audio.SDL;
using Brine2D.Engine;
using Brine2D.Hosting;
using Brine2D.Input.SDL;
using Brine2D.Rendering.SDL;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

// Create the game application builder
var builder = GameApplication.CreateBuilder(args);

// Add SDL3 Input
builder.Services.AddSDL3Input();

// Add SDL3 Audio
builder.Services.AddSDL3Audio();

// Configure rendering with SDL3
builder.Services.AddSDL3Rendering(options =>
{
    builder.Configuration.GetSection("Rendering").Bind(options);
    options.WindowTitle = "Brine2D - Animation Demo";
    options.WindowWidth = 1280;
    options.WindowHeight = 720;
    options.VSync = true;
});

// Register the game scene
builder.Services.AddScene<AnimationDemoScene>();

// Build and run
var game = builder.Build();

await game.RunAsync<AnimationDemoScene>();