using Brine2D.Engine;
using Brine2D.Hosting;
using Brine2D.SDL;
using InputAndText;

// Create builder and configure Brine2D
var builder = GameApplication.CreateBuilder(args);

builder.Services.AddBrine2D(options =>
{
    options.Window.Title = "04 - Input and Text";
    options.Window.Width = 1280;
    options.Window.Height = 720;
});

// Register scene
builder.Services.AddScene<GameScene>();

// Build and run
var game = builder.Build();
await game.RunAsync<GameScene>();