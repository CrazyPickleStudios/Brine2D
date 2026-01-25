using Brine2D.Engine;
using Brine2D.Hosting;
using Brine2D.SDL;
using InputAndText;

// Create builder and configure Brine2D
var builder = GameApplication.CreateBuilder(args);

builder.Services.AddBrine2D(options =>
{
    options.WindowTitle = "04 - Input and Text";
    options.WindowWidth = 1280;
    options.WindowHeight = 720;
});

// Register scene
builder.Services.AddScene<GameScene>();

// Build and run
var game = builder.Build();
await game.RunAsync<GameScene>();