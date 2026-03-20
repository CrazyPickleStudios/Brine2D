using Brine2D.Assets;
using Brine2D.Audio;
using Brine2D.ECS;
using Brine2D.Engine;
using Brine2D.Events;
using Brine2D.Input;
using Brine2D.Rendering;
using Brine2D.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Frozen;
using System.Reflection;

namespace Brine2D.Hosting;

/// <summary>
/// Builder for configuring and creating a game application.
/// </summary>
/// <remarks>
/// Default logging is Console at <see cref="LogLevel.Information"/>; all providers added by
/// <see cref="Host.CreateApplicationBuilder"/> are cleared so the engine starts from a known baseline.
/// Override via <see cref="Logging"/> before calling <see cref="Build"/>.
/// </remarks>
public sealed class GameApplicationBuilder
{
    // Container-synthesized generic collection types; always resolvable without explicit registration.
    private static readonly FrozenSet<Type> _synthesizedCollectionTypes = new HashSet<Type>
    {
        typeof(IEnumerable<>),
        typeof(IReadOnlyList<>),
        typeof(IReadOnlyCollection<>),
        typeof(IList<>),
        typeof(ICollection<>),
    }.ToFrozenSet();

    private readonly HostApplicationBuilder _hostBuilder;
    private readonly Brine2DOptions _options = new();
    private readonly HashSet<Type> _registeredScenes = new();
    private readonly HashSet<Type> _excludedDefaultSystems = new();
    private readonly List<Action<IEntityWorld>> _sceneConfigurations = [];
    private readonly List<Action<Brine2DBuilder>> _brineConfigurations = [];
    private bool _built;
    private Type _fallbackSceneType = typeof(DefaultFallbackScene);
    private readonly List<(Type Type, Action<IEntityWorld> Register)> _additionalDefaultSystems = [];

    internal GameApplicationBuilder(string[] args)
    {
        _hostBuilder = Host.CreateApplicationBuilder(args);
        Logging.ClearProviders();
        Logging.AddConsole();
        Logging.SetMinimumLevel(LogLevel.Information);
    }

    /// <summary>Gets the service collection.</summary>
    public IServiceCollection Services => _hostBuilder.Services;

    /// <summary>Gets the logging builder.</summary>
    public ILoggingBuilder Logging => _hostBuilder.Logging;

    /// <summary>
    /// Gets the host environment. Use this to branch behaviour between
    /// <c>Development</c> and <c>Production</c> at startup.
    /// </summary>
    public IHostEnvironment HostEnvironment => _hostBuilder.Environment;

    /// <summary>
    /// Configures Brine2D options.
    /// </summary>
    /// <remarks>
    /// Sub-option properties are <see langword="init"/>-only; mutate existing instances rather than replacing them.
    /// <code>
    /// builder.Configure(o => o.Rendering.TargetFps = 60);
    /// </code>
    /// </remarks>
    public GameApplicationBuilder Configure(Action<Brine2DOptions> configure)
    {
        EnsureNotBuilt();
        ArgumentNullException.ThrowIfNull(configure);
        configure(_options);
        return this;
    }

    /// <summary>
    /// Registers a scene and defers dependency validation to <see cref="Build"/>.
    /// Registration is optional; unregistered scenes can still be loaded via ActivatorUtilities.
    /// </summary>
    public GameApplicationBuilder AddScene<T>() where T : Scene
        => AddScene(typeof(T));

    /// <summary>
    /// Registers a scene by runtime type. Used internally by <see cref="SceneBuilder.AddRange"/>.
    /// Prefer <see cref="AddScene{T}"/> for compile-time type safety.
    /// </summary>
    /// <param name="sceneType">The scene type to register.</param>
    /// <param name="skipTypeCheck">
    /// When <see langword="true"/>, skips the <see cref="Scene"/> assignability check.
    /// Used by <see cref="SceneBuilder.AddRange"/> which pre-validates all types atomically.
    /// </param>
    internal GameApplicationBuilder AddScene(Type sceneType, bool skipTypeCheck = false)
    {
        ArgumentNullException.ThrowIfNull(sceneType);
        EnsureNotBuilt();

        if (!skipTypeCheck && !typeof(Scene).IsAssignableFrom(sceneType))
            throw new ArgumentException(
                $"Type '{sceneType.Name}' does not inherit from Scene.", nameof(sceneType));

        if (_registeredScenes.Add(sceneType))
            Services.AddTransient(sceneType, sceneType);

        return this;
    }

    /// <summary>
    /// Configures scenes using a fluent builder.
    /// </summary>
    /// <example>
    /// <code>
    /// builder.AddScenes(scenes => scenes
    ///     .Add&lt;MenuScene&gt;()
    ///     .Add&lt;GameScene&gt;()
    ///     .Add&lt;SettingsScene&gt;());
    /// </code>
    /// </example>
    public GameApplicationBuilder AddScenes(Action<SceneBuilder> configure)
    {
        EnsureNotBuilt();
        ArgumentNullException.ThrowIfNull(configure);
        configure(new SceneBuilder(this));
        return this;
    }

    /// <summary>
    /// Configures the entity world for every scene loaded during this game's lifetime.
    /// Called after default systems are added, so you can disable, replace, or extend them.
    /// Can be called multiple times; delegates are additive.
    /// </summary>
    /// <remarks>
    /// All registered delegates are always invoked, even if an earlier one throws.
    /// Exceptions are collected and re-thrown as a <see cref="GameConfigurationException"/>.
    /// </remarks>
    /// <example>
    /// <code>
    /// builder.ConfigureScene(world =>
    ///     world.GetSystem&lt;ParticleSystem&gt;()!.IsEnabled = false);
    ///
    /// builder.ConfigureScene(world =>
    ///     world.AddSystem&lt;MyDebugOverlaySystem&gt;());
    /// </code>
    /// </example>
    public GameApplicationBuilder ConfigureScene(Action<IEntityWorld> configure)
    {
        EnsureNotBuilt();
        ArgumentNullException.ThrowIfNull(configure);
        _sceneConfigurations.Add(configure);
        return this;
    }

    /// <summary>
    /// Prevents a default engine system from being registered in every scene's world.
    /// Use this when a system is never needed project-wide and you want to avoid its
    /// construction cost entirely. To conditionally disable a system at runtime instead,
    /// use <see cref="ConfigureScene"/> with <c>world.GetSystem&lt;T&gt;()!.IsEnabled = false</c>.
    /// Can be called multiple times; exclusions are additive.
    /// </summary>
    /// <example>
    /// <code>
    /// builder.ExcludeDefaultSystem&lt;ParticleSystem&gt;();
    /// builder.ExcludeDefaultSystem&lt;CollisionDetectionSystem&gt;();
    /// </code>
    /// </example>
    public GameApplicationBuilder ExcludeDefaultSystem<T>() where T : class
    {
        EnsureNotBuilt();
        _excludedDefaultSystems.Add(typeof(T));
        return this;
    }

    /// <summary>
    /// Configures optional Brine2D subsystems via <see cref="Brine2DBuilder"/>.
    /// Can be called multiple times; delegates are additive.
    /// </summary>
    /// <example>
    /// <code>
    /// builder.ConfigureBrine2D(b => b
    ///     .UseNetworking()
    ///     .UseDebugOverlay());
    /// </code>
    /// </example>
    public GameApplicationBuilder ConfigureBrine2D(Action<Brine2DBuilder> configure)
    {
        EnsureNotBuilt();
        ArgumentNullException.ThrowIfNull(configure);
        _brineConfigurations.Add(configure);
        return this;
    }

    /// <summary>
    /// Replaces the built-in <see cref="DefaultFallbackScene"/> with a custom scene shown when
    /// a scene load fails and <see cref="ISceneManager.SceneLoadFailed"/> has no handler that
    /// queues a recovery transition.
    /// The fallback scene can inject <see cref="ISceneLoadErrorInfo"/> to display the failure details.
    /// </summary>
    /// <example>
    /// <code>
    /// builder.UseFallbackScene&lt;MyErrorScene&gt;();
    /// </code>
    /// </example>
    public GameApplicationBuilder UseFallbackScene<T>() where T : Scene
    {
        EnsureNotBuilt();
        _fallbackSceneType = typeof(T);
        if (_registeredScenes.Add(typeof(T)))
            Services.AddTransient(typeof(T), typeof(T));
        return this;
    }

    /// <summary>
    /// Adds a system to every scene's entity world, equivalent to calling
    /// <c>world.AddSystem&lt;T&gt;()</c> in <see cref="ConfigureScene"/>.
    /// Can be excluded project-wide with <see cref="ExcludeDefaultSystem{T}"/>.
    /// Can be called multiple times; additions are additive.
    /// </summary>
    /// <example>
    /// <code>
    /// builder.AddDefaultSystem&lt;FogOfWarSystem&gt;();
    /// </code>
    /// </example>
    public GameApplicationBuilder AddDefaultSystem<T>() where T : class, ISystem
    {
        EnsureNotBuilt();
        _additionalDefaultSystems.Add((typeof(T), w => w.AddSystem<T>()));
        return this;
    }

    /// <summary>
    /// Adds a system to every scene's entity world with an initial configuration delegate.
    /// Can be excluded project-wide with <see cref="ExcludeDefaultSystem{T}"/>.
    /// </summary>
    /// <example>
    /// <code>
    /// builder.AddDefaultSystem&lt;FogOfWarSystem&gt;(s => s.Radius = 200f);
    /// </code>
    /// </example>
    public GameApplicationBuilder AddDefaultSystem<T>(Action<T> configure) where T : class, ISystem
    {
        EnsureNotBuilt();
        ArgumentNullException.ThrowIfNull(configure);
        _additionalDefaultSystems.Add((typeof(T), w => w.AddSystem(configure)));
        return this;
    }

    /// <summary>
    /// Builds the game application. Can only be called once per builder instance.
    /// </summary>
    public GameApplication Build()
    {
        EnsureNotBuilt();

        RegisterCoreServices();

        var brine2DBuilder = Services.AddBrine2D();
        ApplyBrine2DConfigurations(brine2DBuilder);
        RegisterBackendServices();
        ValidateAll();

        _built = true;
        var host = _hostBuilder.Build();

        LogConfigurationWarnings(host);

        return new GameApplication(host);
    }

    private void RegisterCoreServices()
    {
        Services.AddSingleton(new RegisteredSceneRegistry(new HashSet<Type>(_registeredScenes)));

        Services.AddSingleton(_options);
        Services.AddSingleton(_options.Window);
        Services.AddSingleton(_options.Rendering);
        Services.AddSingleton(_options.ECS);
        Services.AddSingleton(_options.Audio);

        Services.AddSingleton(BuildSceneWorldConfiguration());
        Services.AddSingleton(new FallbackSceneConfiguration(_fallbackSceneType));
    }

    private SceneWorldConfiguration BuildSceneWorldConfiguration()
    {
        var configurations = _sceneConfigurations.ToArray();
        Action<IEntityWorld>? compositeConfig = configurations.Length > 0
            ? world => RunAllOrThrow(configurations, world, "ConfigureScene")
            : null;

        return new SceneWorldConfiguration(
            compositeConfig,
            _excludedDefaultSystems.ToFrozenSet(),
            _additionalDefaultSystems.Count > 0 ? _additionalDefaultSystems.ToArray() : null);
    }

    private void ApplyBrine2DConfigurations(Brine2DBuilder brine2DBuilder)
    {
        if (_brineConfigurations.Count == 0) return;
        RunAllOrThrow(_brineConfigurations, brine2DBuilder, "ConfigureBrine2D");
    }

    private void RegisterBackendServices()
    {
        if (!_options.Headless)
        {
            Services.AddSDL3EventPump();
            Services.AddSDL3Rendering();
            Services.AddSDL3Input();
            Services.AddSDL3Audio();
        }
        else
        {
            Services.TryAddSingleton<IRenderer, HeadlessRenderer>();
            Services.TryAddSingleton<ITextureLoader, HeadlessTextureLoader>();
            Services.TryAddSingleton<IInputContext, HeadlessInputContext>();
            Services.TryAddSingleton<IAudioService, HeadlessAudioService>();
            Services.TryAddSingleton<IEventPump, HeadlessEventPump>();
        }
    }

    private void ValidateAll()
    {
        var registeredExact = new HashSet<Type>();
        var registeredOpenGeneric = new HashSet<Type>();
        foreach (var descriptor in Services)
        {
            registeredExact.Add(descriptor.ServiceType);
            if (descriptor.ServiceType.IsGenericTypeDefinition)
                registeredOpenGeneric.Add(descriptor.ServiceType);
        }

        _options.Validate();
        ValidateServiceRegistrations(registeredExact);

        foreach (var sceneType in _registeredScenes)
            ValidateSceneDependencies(sceneType, registeredExact, registeredOpenGeneric);

        if (!_registeredScenes.Contains(_fallbackSceneType))
            ValidateSceneDependencies(_fallbackSceneType, registeredExact, registeredOpenGeneric);
    }

    private void LogConfigurationWarnings(IHost host)
    {
        var warnings = GetConfigurationWarnings();
        if (warnings.Count > 0)
        {
            var logger = host.Services.GetRequiredService<ILogger<GameApplicationBuilder>>();
            foreach (var warning in warnings)
                logger.LogWarning("{ConfigWarning}", warning);
        }
    }

    private List<string> GetConfigurationWarnings()
    {
        var warnings = new List<string>();

        if (_options.Headless)
        {
            if (_options.Window.Fullscreen)
                warnings.Add("Window.Fullscreen is true but Headless mode is enabled; window settings are ignored in headless mode.");
            if (_options.Window.Maximized)
                warnings.Add("Window.Maximized is true but Headless mode is enabled; window settings are ignored in headless mode.");
            if (_options.Window.Borderless)
                warnings.Add("Window.Borderless is true but Headless mode is enabled; window settings are ignored in headless mode.");
        }

        foreach (var sceneType in _registeredScenes)
        {
            var constructors = sceneType.GetConstructors();
            if (constructors.Length > 1 &&
                !constructors.Any(c => c.GetCustomAttribute<ActivatorUtilitiesConstructorAttribute>() != null))
            {
                warnings.Add(
                    $"Scene '{sceneType.Name}' has {constructors.Length} constructors with no " +
                    $"[ActivatorUtilitiesConstructor] attribute. Startup-time dependency validation targeted " +
                    $"the best-matching constructor (most resolvable parameters); annotate the intended " +
                    $"constructor to ensure accurate validation.");
            }
        }

        return warnings;
    }

    private static void ValidateSceneDependencies(
        Type sceneType,
        HashSet<Type> registeredExact,
        HashSet<Type> registeredOpenGeneric)
    {
        var constructors = sceneType.GetConstructors();
        if (constructors.Length == 0) return;

        var constructor =
            constructors.FirstOrDefault(c => c.GetCustomAttribute<ActivatorUtilitiesConstructorAttribute>() != null)
            ?? SelectBestConstructor(constructors, registeredExact, registeredOpenGeneric);

        var parameters = constructor.GetParameters();
        if (parameters.Length == 0) return;

        var errors = new List<string>();

        foreach (var param in parameters)
        {
            if (param.HasDefaultValue) continue;

            if (!IsResolvable(param, registeredExact, registeredOpenGeneric))
            {
                errors.Add(
                    $"  • {param.Name} ({param.ParameterType.Name}): not registered. " +
                    $"Add it via builder.Services.Add...() before calling Build().");
            }
        }

        if (errors.Count > 0)
        {
            var multiCtorNote = constructors.Length > 1
                ? Environment.NewLine +
                  $"Note: '{sceneType.Name}' has {constructors.Length} constructors; validation targeted " +
                  $"the best-matching constructor (most resolvable parameters). Annotate the intended one with " +
                  $"[ActivatorUtilitiesConstructor] to ensure accurate validation."
                : string.Empty;

            throw new GameConfigurationException(
                $"Scene '{sceneType.Name}' has unresolvable constructor dependencies:" + Environment.NewLine +
                string.Join(Environment.NewLine, errors) + Environment.NewLine + Environment.NewLine +
                $"Fix: Register missing services in Program.cs before calling Build()." +
                multiCtorNote);
        }
    }

    /// <summary>
    /// Mimics the <see cref="ActivatorUtilities"/> constructor selection algorithm:
    /// prefer the constructor with the most parameters where every parameter is resolvable,
    /// then fall back to the constructor with the most total parameters.
    /// </summary>
    private static ConstructorInfo SelectBestConstructor(
        ConstructorInfo[] constructors,
        HashSet<Type> registeredExact,
        HashSet<Type> registeredOpenGeneric)
    {
        ConstructorInfo? bestFullMatch = null;
        int bestFullMatchCount = -1;
        ConstructorInfo? bestPartial = null;
        int bestPartialCount = -1;

        foreach (var ctor in constructors)
        {
            var parameters = ctor.GetParameters();
            int resolvable = 0;

            foreach (var param in parameters)
            {
                if (param.HasDefaultValue || IsResolvable(param, registeredExact, registeredOpenGeneric))
                    resolvable++;
            }

            if (resolvable == parameters.Length && parameters.Length > bestFullMatchCount)
            {
                bestFullMatch = ctor;
                bestFullMatchCount = parameters.Length;
            }

            if (parameters.Length > bestPartialCount)
            {
                bestPartial = ctor;
                bestPartialCount = parameters.Length;
            }
        }

        return bestFullMatch ?? bestPartial!;
    }

    private static bool IsResolvable(
        ParameterInfo param,
        HashSet<Type> registeredExact,
        HashSet<Type> registeredOpenGeneric)
    {
        var paramType = param.ParameterType;
        return registeredExact.Contains(paramType) ||
               (paramType.IsGenericType && _synthesizedCollectionTypes.Contains(paramType.GetGenericTypeDefinition())) ||
               (paramType.IsGenericType && registeredOpenGeneric.Contains(paramType.GetGenericTypeDefinition())) ||
               param.GetCustomAttribute<FromKeyedServicesAttribute>() != null;
    }

    private static void ValidateServiceRegistrations(HashSet<Type> registered)
    {
        var errors = new List<string>();
        bool HasService<T>() => registered.Contains(typeof(T));

        // Core engine services
        if (!HasService<GameEngine>())            errors.Add("GameEngine is not registered.");
        if (!HasService<GameLoop>())              errors.Add("GameLoop is not registered.");
        if (!HasService<ISceneManager>())         errors.Add("ISceneManager is not registered.");
        if (!HasService<IEntityWorld>())          errors.Add("IEntityWorld is not registered.");
        if (!HasService<IMainThreadDispatcher>()) errors.Add("IMainThreadDispatcher is not registered.");
        if (!HasService<IEventBus>())             errors.Add("IEventBus is not registered.");
        if (!HasService<IGameContext>())          errors.Add("IGameContext is not registered.");
        if (!HasService<IAssetLoader>())          errors.Add("IAssetLoader is not registered.");

        // Backend services
        if (!HasService<IRenderer>())      errors.Add("IRenderer is not registered.");
        if (!HasService<ITextureLoader>()) errors.Add("ITextureLoader is not registered.");
        if (!HasService<IInputContext>())  errors.Add("IInputContext is not registered.");
        if (!HasService<IEventPump>())     errors.Add("IEventPump is not registered.");
        if (!HasService<IAudioService>())  errors.Add("IAudioService is not registered.");

        // Default scene system services
        if (!HasService<ICameraManager>()) errors.Add("ICameraManager is not registered.");
        if (!HasService<ICamera>())        errors.Add("ICamera is not registered.");

        if (errors.Count > 0)
        {
            throw new GameConfigurationException(
                "Game application service registration validation failed:" + Environment.NewLine +
                string.Join(Environment.NewLine, errors.Select(e => $"  • {e}")) + Environment.NewLine +
                Environment.NewLine +
                "This is likely a framework bug. Please report it with your configuration.");
        }
    }

    private static void RunAllOrThrow<T>(
        IReadOnlyList<Action<T>> delegates, T argument, string delegateName)
    {
        List<Exception>? exceptions = null;
        foreach (var action in delegates)
        {
            try { action(argument); }
            catch (Exception ex) { (exceptions ??= []).Add(ex); }
        }

        if (exceptions is not { Count: > 0 })
            return;

        var inner = exceptions.Count == 1
            ? exceptions[0]
            : new AggregateException(
                $"One or more {delegateName} delegates threw an exception.", exceptions);

        throw new GameConfigurationException(
            $"{exceptions.Count} builder.{delegateName}() delegate(s) threw an exception." +
            Environment.NewLine +
            $"Fix: Check your {delegateName} delegates in Program.cs.",
            inner);
    }

    private void EnsureNotBuilt()
    {
        if (_built)
            throw new InvalidOperationException(
                "Cannot modify GameApplicationBuilder after Build() has been called." + Environment.NewLine +
                Environment.NewLine +
                "Each builder can only be built once. If you need multiple game instances, create separate builders:" + Environment.NewLine +
                "  var game1 = GameApplication.CreateBuilder().Configure(...).Build();" + Environment.NewLine +
                "  var game2 = GameApplication.CreateBuilder().Configure(...).Build();");
    }
}