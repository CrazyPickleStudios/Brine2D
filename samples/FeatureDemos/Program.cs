using Brine2D.Core;
using Brine2D.Collision;
using Brine2D.Performance;
using Brine2D.Tilemap;
using Brine2D.ECS;
using Brine2D.Engine;
using Brine2D.Hosting;
using Brine2D.Input;
using Brine2D.Rendering;
using Brine2D.Rendering.SDL;
using Brine2D.Rendering.SDL.PostProcessing;
using Brine2D.Rendering.SDL.PostProcessing.Effects;
using Brine2D.SDL.Common;
using Brine2D.Systems.Performance;
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
using Microsoft.Extensions.Logging;
using Brine2D.Systems.Audio;
using Brine2D.Systems.Rendering;
using Brine2D.SDL.Audio;
using Brine2D.SDL.Input;
using Brine2D.Systems.Input;
using Brine2D.Systems.AI;
using Brine2D.Systems.Physics;

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

builder.Services.AddPostProcessing(options =>
{
    options.Enabled = true; // Toggle on/off
    // options.RenderTargetFormat can be customized if needed
});

// ECS services
// ECS should have parallelization enabled (default):
builder.Services.AddObjectECS(options =>
{
    options.EnableParallelExecution = true; // Default
    options.ParallelEntityThreshold = 100;  // Default
    options.MaxDegreeOfParallelism = -1;    // Use all cores
});
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
    // ONLY global systems
    pipelines.AddSystem<PlayerControllerSystem>();
    pipelines.AddSystem<AISystem>();
    pipelines.AddSystem<VelocitySystem>();
    pipelines.AddSystem<PhysicsSystem>();
    pipelines.AddSystem<AudioSystem>();
    pipelines.AddSystem<CameraSystem>();
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
builder.Services.AddTransient<CustomLoadingScreen>();

// Add performance monitoring
builder.Services.AddPerformanceMonitoring(); // Core tracking

// Add performance overlay (optional, for debugging)
builder.Services.AddPerformanceOverlay(); // Rendering overlay

// If using ECS rendering, add stats collector
builder.Services.AddSingleton<ISceneLifecycleHook, RenderingStatsCollector>();

// Replace grayscale with blur, or chain both:
builder.Services.AddGrayscaleEffect(1280, 720, intensity: 1.0f);
builder.Services.AddBlurEffect(1280, 720, blurRadius: 3.0f); // Strong blur

// Build and run
var game = builder.Build();

// Register post-processing effect factories (created lazily after GPU init)
//var pipeline = game.Services.GetService<SDL3PostProcessPipeline>();
//var loggerFactory = game.Services.GetRequiredService<ILoggerFactory>();

//if (pipeline != null)
//{
//    // Blur effect factory
//    pipeline.AddEffectFactory(() =>
//    {
//        var renderer = game.Services.GetRequiredService<IRenderer>();
//        if (renderer is SDL3GPURenderer gpuRenderer)
//        {
//            return new BlurEffect(
//                gpuRenderer.Device,
//                1280,
//                720,
//                loggerFactory,
//                loggerFactory.CreateLogger<BlurEffect>())
//            {
//                BlurRadius = 1.0f // Adjust strength here
//            };
//        }
//        throw new InvalidOperationException("Blur effect requires SDL3GPURenderer");
//    });
//}

// Start with main menu!
await game.RunAsync<MainMenuScene>();