using BasicGame;
using Brine2D.Audio.ECS;
using Brine2D.Audio.SDL;
using Brine2D.Core.Collision;
using Brine2D.Core.Tilemap;
using Brine2D.ECS;
using Brine2D.ECS.Systems;
using Brine2D.Input.ECS;
using Brine2D.Engine;
using Brine2D.Hosting;
using Brine2D.Input;
using Brine2D.Input.SDL;
using Brine2D.Rendering;
using Brine2D.Rendering.ECS;
using Brine2D.Rendering.SDL;
using Brine2D.UI;
using Microsoft.Extensions.Configuration;

// Create the game application builder
var builder = GameApplication.CreateBuilder(args);

// Core services
builder.Services.AddInputLayerManager().AddSDL3Input();
builder.Services.AddSDL3Audio();

builder.Services.AddSDL3Rendering(options =>
{
    builder.Configuration.GetSection("Rendering").Bind(options);
    options.WindowTitle = "Brine2D - ECS Demo";
    options.WindowWidth = 1280;
    options.WindowHeight = 720;
    options.VSync = true;
});

// ECS services
builder.Services.AddObjectECS();
builder.Services.AddECSRendering(); 
builder.Services.AddECSInput();
builder.Services.AddECSAudio();

// Configure System Pipelines (ASP.NET-like)
builder.Services.ConfigureSystemPipelines(pipelines =>
{
    // Update-only systems (IUpdateSystem)
    pipelines.AddSystem<PlayerControllerSystem>();
    pipelines.AddSystem<AISystem>();
    pipelines.AddSystem<VelocitySystem>();
    pipelines.AddSystem<PhysicsSystem>();
    pipelines.AddSystem<AudioSystem>();
    pipelines.AddSystem<CameraSystem>();

    // Render-only systems (IRenderSystem)
    pipelines.AddSystem<SpriteRenderingSystem>();
    pipelines.AddSystem<DebugRenderer>();

    // Both update + render (automatically added to both)
    pipelines.AddSystem<ParticleSystem>();
});

// Other services
builder.Services.AddTilemapServices();
builder.Services.AddTilemapRenderer();
builder.Services.AddCollisionSystem();
builder.Services.AddUICanvas();

// Register scene
builder.Services.AddScene<ECSQuickStartScene>();

// Build and run
var game = builder.Build();

await game.RunAsync<ECSQuickStartScene>();