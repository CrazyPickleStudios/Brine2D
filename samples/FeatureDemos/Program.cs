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
using FeatureDemos.Scenes;
using FeatureDemos.Scenes.ECS;
using FeatureDemos.Scenes.Transitions;
using FeatureDemos.Scenes.Advanced;
using FeatureDemos.Scenes.Collision;
using FeatureDemos.Scenes.UI;

// Create the game application builder
var builder = GameApplication.CreateBuilder(args);

// Core services
builder.Services.AddInputLayerManager().AddSDL3Input();
builder.Services.AddSDL3Audio();

builder.Services.AddSDL3Rendering(options =>
{
    builder.Configuration.GetSection("Rendering").Bind(options);
    options.WindowTitle = "Brine2D - Feature Demos";
    options.WindowWidth = 1280;
    options.WindowHeight = 720;
    options.VSync = true;
});

// ECS services
builder.Services.AddObjectECS();
builder.Services.AddECSRendering(); 
builder.Services.AddECSInput();
builder.Services.AddECSAudio();

// Configure System Pipelines
builder.Services.ConfigureSystemPipelines(pipelines =>
{
    // Update systems
    pipelines.AddSystem<PlayerControllerSystem>();
    pipelines.AddSystem<AISystem>();
    pipelines.AddSystem<VelocitySystem>();
    pipelines.AddSystem<PhysicsSystem>();
    pipelines.AddSystem<AudioSystem>();
    pipelines.AddSystem<CameraSystem>();

    // Render systems
    pipelines.AddSystem<SpriteRenderingSystem>();
    pipelines.AddSystem<DebugRenderer>();
    pipelines.AddSystem<ParticleSystem>();
});

// Other services
builder.Services.AddTilemapServices();
builder.Services.AddTilemapRenderer();
builder.Services.AddCollisionSystem();
builder.Services.AddUICanvas();

// Register demo scenes
// ECS Demos
builder.Services.AddScene<QueryDemoScene>();
builder.Services.AddScene<ParticleDemoScene>();

// Collision Demos
builder.Services.AddScene<CollisionDemoScene>();

// Transition Demos
builder.Services.AddScene<TransitionDemoScene>();
builder.Services.AddScene<SceneA>();
builder.Services.AddScene<SceneB>();
builder.Services.AddScene<SceneC>();

// UI Demos
builder.Services.AddScene<UIDemoScene>();

// Advanced Demos
builder.Services.AddScene<ManualControlScene>();

builder.Services.AddScene<MainMenuScene>();

// Build and run
var game = builder.Build();

// Start with main menu!
await game.RunAsync<MainMenuScene>();
