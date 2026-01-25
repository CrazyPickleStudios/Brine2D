using Brine2D.Engine;
using Brine2D.Hosting;
using Brine2D.SDL;
using SceneBasics;

// Create builder and configure Brine2D
// (See 01-HelloBrine for detailed setup explanation)
var builder = GameApplication.CreateBuilder(args);

builder.Services.AddBrine2D(options =>
{
    options.WindowTitle = "02 - Scene Basics";
    options.WindowWidth = 1280;
    options.WindowHeight = 720;
});

// Register both scenes
builder.Services.AddScene<MenuScene>();
builder.Services.AddScene<GameScene>();

// Build and run starting with MenuScene
var game = builder.Build();
await game.RunAsync<MenuScene>();