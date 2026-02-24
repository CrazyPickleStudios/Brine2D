using Brine2D.Collision;
using Brine2D.Engine;
using Brine2D.Hosting;
using Brine2D.Performance;
using Brine2D.Rendering;
using Brine2D.Rendering.SDL;
using Brine2D.Systems.Rendering;
using Brine2D.Tilemap;
using Brine2D.UI;
using FeatureDemos.Scenes;
using FeatureDemos.Scenes.Advanced;
using FeatureDemos.Scenes.Audio;
using FeatureDemos.Scenes.Collision;
using FeatureDemos.Scenes.ECS;
using FeatureDemos.Scenes.Performance;
using FeatureDemos.Scenes.Rendering;
using FeatureDemos.Scenes.Transitions;
using FeatureDemos.Scenes.UI;

// Create the game application builder
var builder = GameApplication.CreateBuilder(args);

// Configure Brine2D options
builder.Configure(options =>
{
    options.Window.Title = "Brine2D - Feature Demos";
    options.Window.Width = 1280;
    options.Window.Height = 720;
    options.Rendering.VSync = true;
    options.Rendering.TargetFPS = 0;
    options.Rendering.PreferredGPUDriver = GPUDriver.Vulkan;
    options.ECS.EnableMultiThreading = false;
});

// Optional features
builder.Services.AddPostProcessing();
builder.Services.AddTextureAtlasing();
builder.Services.AddTilemapServices();
builder.Services.AddCollisionSystem();
builder.Services.AddUICanvas();
builder.Services.AddPerformanceMonitoring();

// ConfigureScene is the right place for project-wide system config
builder.ConfigureScene(world =>
    world.GetSystem<DebugRenderer>()!.IsEnabled = true); // enable debug globally

// Build and run (SDL registered here automatically!)
await using var game = builder.Build();
await game.RunAsync<MainMenuScene>();