using Brine2D.Collision;
using Brine2D.Hosting;
using Brine2D.Performance;
using Brine2D.Rendering;
using Brine2D.Rendering.SDL;
using Brine2D.SDL;
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

// Configure Brine2D with SDL backend
builder.Services
    .AddBrine2D(options =>
    {
        options.Window.Title = "Brine2D - Feature Demos";
        options.Window.Width = 1280;
        options.Window.Height = 720;
        options.ECS.EnableMultiThreading = false;
    })
    .UseGPURenderer(gpu => gpu          
        .WithVSync(true)
        .WithTargetFPS(0)               
        .WithDriver("vulkan"))
    .UseSystems()                       
    .UseSDL();

builder.AddScenes(scenes => scenes
    .Add<MainMenuScene>()
    .Add<QueryDemoScene>()
    .Add<ParticleDemoScene>()
    .Add<CollisionDemoScene>()
    .Add<TransitionDemoScene>()
    .Add<SceneA>()
    .Add<SceneB>()
    .Add<SceneC>()
    .Add<UIDemoScene>()
    .Add<ManualControlScene>()
    .Add<SpriteBenchmarkScene>()
    .Add<TextureAtlasDemoScene>()
    .Add<SpatialAudioDemoScene>()
    .Add<BackgroundLoadingDemoScene>()
    .Add<ScissorRectDemoScene>());  

// Other services
builder.Services.AddPostProcessing(options => { options.Enabled = true; });
builder.Services.AddTextureAtlasing();
builder.Services.AddTilemapServices();
builder.Services.AddCollisionSystem();
builder.Services.AddUICanvas();
builder.Services.AddPerformanceMonitoring();

var game = builder.Build();

await game.RunAsync<MainMenuScene>();