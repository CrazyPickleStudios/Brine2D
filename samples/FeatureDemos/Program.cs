using Brine2D.Collision;
using Brine2D.ECS;
using Brine2D.Engine;
using Brine2D.Hosting;
using Brine2D.Performance;
using Brine2D.Rendering;
using Brine2D.Rendering.SDL;
using Brine2D.SDL;
using Brine2D.Systems.AI;
using Brine2D.Systems.Audio;
using Brine2D.Systems.Collision;
using Brine2D.Systems.Input;
using Brine2D.Systems.Performance;
using Brine2D.Systems.Physics;
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
using Microsoft.Extensions.DependencyInjection;

// Create the game application builder
var builder = GameApplication.CreateBuilder(args);

// Configure and register Brine2D with SDL backend
builder.Services
    .AddBrine2D(options =>
    {
        options.Window.Title = "Brine2D - Feature Demos";
        options.Window.Width = 1280;
        options.Window.Height = 720;
        options.Rendering.Backend = GraphicsBackend.GPU;
        options.Rendering.PreferredGPUDriver = "vulkan";
        options.Rendering.VSync = true;
        options.ECS.EnableMultiThreading = true;
        options.ECS.ParallelEntityThreshold = 100;
        options.ECS.WorkerThreadCount = null;
    })
    .UseSystems() // Auto-adds ALL built-in systems to pipelines!
    .UseSDL();

// Optional: ONLY needed for CUSTOM systems
builder.Services.ConfigureSystemPipelines(pipelines =>
{
    pipelines.AddSystem<AISystem>(); // Custom AI system
    pipelines.AddSystem<BenchmarkSystem>(); // Custom benchmark system
});

// Post-processing (optional)
builder.Services.AddPostProcessing(options => { options.Enabled = true; });

// Texture atlasing (optional)
builder.Services.AddTextureAtlasing(options =>
{
    options.MaxAtlasWidth = 2048;
    options.MaxAtlasHeight = 2048;
    options.Padding = 2;
    options.UsePowerOfTwo = true;
    options.DefaultScaleMode = TextureScaleMode.Nearest;
});

// Optional: Configure global system pipelines if needed
// (Systems are already registered by .UseSystems(), but you can customize order)
builder.Services.ConfigureSystemPipelines(pipelines =>
{
    // Rendering systems
    pipelines.AddSystem<SpriteRenderingSystem>();
    pipelines.AddSystem<ParticleSystem>();
    pipelines.AddSystem<CameraSystem>();
    pipelines.AddSystem<DebugRenderer>();

    // Physics systems
    pipelines.AddSystem<VelocitySystem>();
    pipelines.AddSystem<CollisionDetectionSystem>();

    // Input systems
    pipelines.AddSystem<PlayerControllerSystem>();

    // AI systems
    pipelines.AddSystem<AISystem>();

    // Audio systems
    pipelines.AddSystem<AudioSystem>();
});

// Other optional services
builder.Services.AddTilemapServices();
builder.Services.AddTilemapRenderer();
builder.Services.AddCollisionSystem();
builder.Services.AddUICanvas();

// Register demo scenes
builder.Services.AddScene<QueryDemoScene>();
builder.Services.AddScene<ParticleDemoScene>();
builder.Services.AddScene<CollisionDemoScene>();
builder.Services.AddScene<TransitionDemoScene>();
builder.Services.AddScene<SceneA>();
builder.Services.AddScene<SceneB>();
builder.Services.AddScene<SceneC>();
builder.Services.AddScene<UIDemoScene>();
builder.Services.AddScene<ManualControlScene>();
builder.Services.AddScene<SpriteBenchmarkScene>();
builder.Services.AddScene<TextureAtlasDemoScene>();
builder.Services.AddScene<SpatialAudioDemoScene>();
builder.Services.AddScene<MainMenuScene>();

// Custom loading screen
builder.Services.AddTransient<CustomLoadingScreen>();

// Performance monitoring (optional)
builder.Services.AddPerformanceMonitoring();
builder.Services.AddPerformanceOverlay();
builder.Services.AddSingleton<ISceneLifecycleHook, RenderingStatsCollector>();

// Post-processing effects (optional)
builder.Services.AddGrayscaleEffect();
builder.Services.AddBlurEffect(1280, 720, 3.0f);

// Build and run
var game = builder.Build();
await game.RunAsync<MainMenuScene>();