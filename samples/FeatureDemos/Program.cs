using Brine2D.Audio.ECS;
using Brine2D.Audio.SDL;
using Brine2D.Core;
using Brine2D.Core.Collision;
using Brine2D.Core.Performance;
using Brine2D.Core.Tilemap;
using Brine2D.ECS;
using Brine2D.ECS.Systems;
using Brine2D.Engine;
using Brine2D.Hosting;
using Brine2D.Input;
using Brine2D.Input.ECS;
using Brine2D.Input.SDL;
using Brine2D.Rendering;
using Brine2D.Rendering.ECS;
using Brine2D.Rendering.ECS.Performance;
using Brine2D.Rendering.Performance;
using Brine2D.Rendering.SDL;
using Brine2D.SDL.Common;
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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

// Create the game application builder
var builder = GameApplication.CreateBuilder(args);

builder.Services.AddBrineCore();

builder.Services.AddSDL3ApplicationLifetime();

builder.Services.AddBrineEngine();

// Core services
builder.Services.AddInputLayerManager().AddSDL3Input();
builder.Services.AddSDL3Audio();

builder.Services.AddSDL3Rendering(options =>
{
    builder.Configuration.GetSection("Rendering").Bind(options);
    options.WindowTitle = "Brine2D - Feature Demos";
    options.WindowWidth = 1280;
    options.WindowHeight = 720;

    //options.Backend = GraphicsBackend.LegacyRenderer;

    options.Backend = GraphicsBackend.GPU;
    options.PreferredGPUDriver = "vulkan";

    options.VSync = true;
});

// ECS services
builder.Services.AddObjectECS();
builder.Services.AddECSRendering(); 
builder.Services.AddECSInput();
builder.Services.AddECSAudio();

builder.Services.AddTextureAtlasing(options =>
{
    options.MaxAtlasWidth = 2048;
    options.MaxAtlasHeight = 2048;
    options.Padding = 2;
    options.UsePowerOfTwo = true;
    options.DefaultScaleMode = TextureScaleMode.Nearest;
});

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

builder.Services.AddScene<SpriteBenchmarkScene>();

builder.Services.AddScene<TextureAtlasDemoScene>();

builder.Services.AddScene<SpatialAudioDemoScene>();

builder.Services.AddScene<MainMenuScene>();

// Add performance monitoring
builder.Services.AddPerformanceMonitoring(); // Core tracking

// Add performance overlay (optional, for debugging)
builder.Services.AddPerformanceOverlay(); // Rendering overlay

// If using ECS rendering, add stats collector
builder.Services.AddSingleton<ISceneLifecycleHook, RenderingStatsCollector>();

// Build and run
var game = builder.Build();

// Start with main menu!
await game.RunAsync<MainMenuScene>();
