using Brine2D.Hosting;
using SceneBasics;

var builder = GameApplication.CreateBuilder(args);

builder.Configure(options =>
{
    options.Window.Title = "02 - Scene Basics";
    options.Window.Width = 1280;
    options.Window.Height = 720;
});

builder.AddScene<MenuScene>();
builder.AddScene<GameScene>();

await using var game = builder.Build();
await game.RunAsync<MenuScene>();