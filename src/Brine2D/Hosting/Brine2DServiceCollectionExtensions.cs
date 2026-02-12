using Brine2D.Assets;
using Brine2D.Core;
using Brine2D.ECS;
using Brine2D.ECS.Systems;
using Brine2D.Engine;
using Brine2D.Events;
using Brine2D.Input;
using Brine2D.Performance;
using Brine2D.Pooling;
using Brine2D.Rendering;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Brine2D.Systems.Audio;
using Brine2D.Systems.Input;
using Brine2D.Systems.Physics;
using Brine2D.Systems.Rendering;
using Brine2D.Threading;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.ObjectPool;

namespace Brine2D.Hosting;

/// <summary>
/// Extensions for registering Brine2D core services.
/// </summary>
public static class Brine2DServiceCollectionExtensions
{
    /// <summary>
    /// Adds Brine2D core engine services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action for Brine2D options.</param>
    /// <returns>A <see cref="Brine2DBuilder"/> for chaining backend configuration.</returns>
    /// <remarks>
    /// <para>
    /// This method registers the core engine services (game loop, scenes, ECS) without
    /// any platform-specific services (rendering, input, audio).
    /// </para>
    /// <para>
    /// <strong>For a complete game with rendering, input, and audio:</strong>
    /// </para>
    /// <code>
    /// builder.Services.AddBrine2D(options => { /* ... */ }).UseSDL();
    /// </code>
    /// <para>
    /// <strong>For headless mode (testing, dedicated servers):</strong>
    /// </para>
    /// <code>
    /// builder.Services.AddBrine2D(options => { /* ... */ });
    /// </code>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Complete game with SDL backend
    /// builder.Services
    ///     .AddBrine2D(options =>
    ///     {
    ///         options.Window.Title = "My Game";
    ///         options.Window.Width = 1280;
    ///         options.Window.Height = 720;
    ///         options.Rendering.Backend = GraphicsBackend.GPU;
    ///         options.ECS.EnableMultiThreading = true;
    ///     })
    ///     .UseSDL();
    ///     
    /// // Headless mode
    /// builder.Services.AddBrine2D(options =>
    /// {
    ///     options.ECS.EnableMultiThreading = true;
    /// });
    /// </code>
    /// </example>
    public static Brine2DBuilder AddBrine2D(
        this IServiceCollection services,
        Action<Brine2DOptions>? configure = null)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        // Configure root options
        if (configure != null)
        {
            services.Configure(configure);
        }
        
        // Bind options from configuration (gamesettings.json, etc.) with validation
        services.AddOptions<Brine2DOptions>()
            .BindConfiguration(Brine2DOptions.SectionName)
            .ValidateDataAnnotations()     
            .ValidateOnStart();            
    
        // Configure ECS options (core engine) with validation
        services.AddOptions<ECSOptions>()
            .BindConfiguration($"{Brine2DOptions.SectionName}:{ECSOptions.SectionName}")
            .ValidateDataAnnotations()      
            .ValidateOnStart()             
            .Configure<IOptions<Brine2DOptions>>((ecsOpts, brineOpts) =>
            {
                if (configure != null)
                {
                    var ecs = brineOpts.Value.ECS;
                    ecsOpts.EnableMultiThreading = ecs.EnableMultiThreading;
                    ecsOpts.InitialEntityCapacity = ecs.InitialEntityCapacity;
                    ecsOpts.EnableQueryCaching = ecs.EnableQueryCaching;
                    ecsOpts.WorkerThreadCount = ecs.WorkerThreadCount;
                    ecsOpts.ParallelEntityThreshold = ecs.ParallelEntityThreshold;
                }
            });

        // Register core services
        AddBrineCore(services);
        AddECSCore(services);
        AddBrineEngine(services);

        return new Brine2DBuilder(services);
    }
    
    /// <summary>
    /// Adds core Brine2D services (event bus, object pooling).
    /// </summary>
    private static void AddBrineCore(IServiceCollection services)
    {
        // Register public event bus
        services.TryAddSingleton<EventBus>(sp => 
            new EventBus(sp.GetService<ILogger<EventBus>>()));

        // Main thread dispatcher (required for GPU operations)
        services.TryAddSingleton<IMainThreadDispatcher, MainThreadDispatcher>();
        
        services.TryAddSingleton<IAssetLoader, AssetLoader>();
        
        // Register object pooling (required by ParticleSystem)
        services.AddObjectPooling(); 
    }
    
    /// <summary>
    /// Adds core Brine2D engine services (game loop, scene manager, context).
    /// </summary>
    private static void AddBrineEngine(IServiceCollection services)
    {
        services.TryAddSingleton<GameEngine>();
        
        services.TryAddSingleton<GameLoop>(sp => new GameLoop(
            sp.GetRequiredService<ILogger<GameLoop>>(),
            sp.GetRequiredService<ILoggerFactory>(),
            sp.GetRequiredService<IGameContext>(),
            sp.GetRequiredService<ISceneManager>(),
            sp.GetRequiredService<IInputContext>(),
            sp.GetRequiredService<IHostApplicationLifetime>(),
            sp.GetService<InputLayerManager>(),
            sp.GetService<IEventPump>()
        ));
        
        services.TryAddSingleton<IGameContext, GameContext>();
        services.TryAddSingleton<ISceneManager, SceneManager>();
    }

    /// <summary>
    /// Adds core ECS infrastructure (pipelines, worlds).
    /// Called automatically by AddBrine2D().
    /// </summary>
    private static void AddECSCore(IServiceCollection services)
    {
        // Register update and render pipelines
        services.TryAddSingleton<UpdatePipeline>(sp => 
            new UpdatePipeline(
                sp.GetService<ILogger<UpdatePipeline>>(),
                sp.GetService<ScopedProfiler>(),
                sp.GetService<ECSOptions>()));
        
        services.TryAddSingleton<RenderPipeline>(sp => 
            new RenderPipeline(
                sp.GetService<ILogger<RenderPipeline>>(),
                sp.GetService<ScopedProfiler>()));
        
        // Register per-scene EntityWorld (scoped lifetime)
        services.TryAddScoped<IEntityWorld>(sp =>
        {
            var loggerFactory = sp.GetService<ILoggerFactory>();
            var ecsOptions = sp.GetService<IOptions<ECSOptions>>();
            
            return new EntityWorld(
                sp,
                loggerFactory,
                ecsOptions);
        });
    }

    /// <summary>
    /// Adds data-oriented systems for automatic entity processing.
    /// Automatically registers all built-in systems to the update and render pipelines.
    /// Also sets up camera infrastructure.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use this if you're building with pure data components and want systems
    /// to automatically process entities (data-oriented ECS approach).
    /// </para>
    /// <para>
    /// If you're using object-oriented components with Update/FixedUpdate methods,
    /// you don't need these systems.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Data-oriented approach with built-in systems
    /// builder.Services
    ///     .AddBrine2D()
    ///     .UseSystems()  // Auto-adds ALL built-in systems + camera setup!
    ///     .UseSDL();
    ///     
    /// // Add custom systems
    /// builder.Services.ConfigureSystemPipelines(pipelines =>
    /// {
    ///     pipelines.AddSystem&lt;MyCustomSystem&gt;();
    /// });
    /// 
    /// // Object-oriented approach (no systems)
    /// builder.Services
    ///     .AddBrine2D()
    ///     .UseSDL(); // No systems needed
    /// </code>
    /// </example>
    public static Brine2DBuilder UseSystems(this Brine2DBuilder builder)
    {
        if (builder == null)
            throw new ArgumentNullException(nameof(builder));

        var services = builder.Services;

        // Camera infrastructure
        services.TryAddSingleton<ICameraManager, CameraManager>();

        services.TryAddScoped<ICamera>(sp =>
        {
            var renderer = sp.GetService<IRenderer>();
            
            if (renderer != null)
            {
                // Fresh camera per scene matching renderer viewport
                var camera = new Camera2D(renderer.Width, renderer.Height);
                
                // Register with camera manager (optional, for multi-camera support)
                var cameraManager = sp.GetService<ICameraManager>();
                if (cameraManager != null)
                {
                    cameraManager.RegisterCamera("main", camera);
                    cameraManager.MainCamera = camera;
                }
                
                return camera;
            }
            
            // Headless fallback - default camera
            return new Camera2D(1280, 720);
        });

        // Register lifecycle hook for update pipeline execution
        services.AddECSLifecycleHook();

        // Auto-register all built-in systems to pipelines
        services.ConfigureSystemPipelines(pipelines =>
        {
            // Rendering systems (order matters - sprites before particles before debug)
            pipelines.AddSystem<SpriteRenderingSystem>();
            pipelines.AddSystem<ParticleSystem>();
            pipelines.AddSystem<CameraSystem>();
            pipelines.AddSystem<DebugRenderer>();
            
            // Physics systems
            pipelines.AddSystem<VelocitySystem>();
            pipelines.AddSystem<PhysicsSystem>();
            
            // Input systems
            pipelines.AddSystem<PlayerControllerSystem>();
            
            // Audio systems
            pipelines.AddSystem<AudioSystem>();
        });

        // Register render lifecycle hook
        services.AddSingleton<ISceneLifecycleHook, ECSRenderHook>();

        return builder;
    }
}