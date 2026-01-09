using Brine2D.Core;
using Brine2D.ECS;
using Brine2D.ECS.Components;
using Brine2D.Engine;
using Brine2D.Input;
using Brine2D.Rendering;
using Brine2D.Rendering.ECS;
using Brine2D.Rendering.Performance;
using Microsoft.Extensions.Logging;
using System.Numerics;

namespace FeatureDemos.Scenes.ECS;

/// <summary>
/// Demo scene showcasing the particle system with various effects.
/// Notice: NO manual pipeline calls - they execute automatically!
/// Notice: NO frame management - SceneManager handles it!
/// </summary>
public class ParticleDemoScene : DemoSceneBase
{
    private readonly IEntityWorld _world;
    private readonly IRenderer _renderer;
    private readonly IInputService _input;
    private readonly IGameContext _gameContext;

    private Entity? _currentEmitter;
    private EffectType _currentEffect = EffectType.Fire;
    private bool _continuousEmission = true;

    private enum EffectType
    {
        Fire,
        Explosion,
        Smoke,
        Sparkles,
        Trail
    }

    public ParticleDemoScene(
        IEntityWorld world,
        IRenderer renderer,
        IInputService input,
        ISceneManager sceneManager,
        IGameContext gameContext,
        ILogger<ParticleDemoScene> logger,
        PerformanceOverlay? perfOverlay = null) 
        : base(input, sceneManager, gameContext, logger, renderer, world, perfOverlay)
    {
        _world = world;
        _renderer = renderer;
        _input = input;
        _gameContext = gameContext;
    }

    protected override void OnInitialize()
    {
        Logger.LogInformation("=== Particle Demo Scene ===");
        Logger.LogInformation("Controls:");
        Logger.LogInformation("  1-5 - Switch particle effects");
        Logger.LogInformation("  Left Click - Spawn particle burst at mouse");
        Logger.LogInformation("  SPACE - Toggle continuous emission");
        Logger.LogInformation("  ESC - Exit");
        Logger.LogInformation("");

        // Set clear color for this scene (SceneManager will use it)
        _renderer.ClearColor = new Color(15, 15, 25);

        CreateEffect(_currentEffect, new Vector2(640, 360));
    }

    protected override void OnUpdate(GameTime gameTime)
    {
        HandlePerformanceHotkeys();
        if (CheckReturnToMenu()) return;

        // Switch effects
        if (_input.IsKeyPressed(Keys.D1)) SwitchEffect(EffectType.Fire);
        if (_input.IsKeyPressed(Keys.D2)) SwitchEffect(EffectType.Explosion);
        if (_input.IsKeyPressed(Keys.D3)) SwitchEffect(EffectType.Smoke);
        if (_input.IsKeyPressed(Keys.D4)) SwitchEffect(EffectType.Sparkles);
        if (_input.IsKeyPressed(Keys.D5)) SwitchEffect(EffectType.Trail);

        // Toggle continuous emission
        if (_input.IsKeyPressed(Keys.Space))
        {
            _continuousEmission = !_continuousEmission;

            if (_currentEmitter != null)
            {
                var emitter = _currentEmitter.GetComponent<ParticleEmitterComponent>();
                if (emitter != null)
                {
                    emitter.IsEmitting = _continuousEmission;
                }
            }

            Logger.LogInformation("Continuous emission: {State}", _continuousEmission ? "ON" : "OFF");
        }

        // Spawn burst on click
        if (_input.IsMouseButtonPressed(MouseButton.Left))
        {
            var clickPos = new Vector2(_input.MousePosition.X, _input.MousePosition.Y);
            CreateBurst(_currentEffect, clickPos);
        }

        // For trail effect, update position to follow mouse
        if (_currentEffect == EffectType.Trail && _currentEmitter != null)
        {
            var transform = _currentEmitter.GetComponent<TransformComponent>();
            if (transform != null)
            {
                transform.Position = new Vector2(_input.MousePosition.X, _input.MousePosition.Y);
            }
        }

        // NO MANUAL PIPELINE CALLS NEEDED!
        // ECS systems execute automatically via lifecycle hooks!
    }

    protected override void OnRender(GameTime gameTime)
    {
        // NO FRAME MANAGEMENT NEEDED!
        // SceneManager handles Clear/BeginFrame/EndFrame automatically!
        // ECS particles are already rendered via PreRender hook!

        // Just draw scene-specific UI
        DrawUI();

        RenderPerformanceOverlay();
    }

    private void DrawUI()
    {
        _renderer.DrawText("Particle Effects Demo", 10, 10, Color.White);
        _renderer.DrawText($"Effect: {_currentEffect} (1-5 to switch)", 10, 35, Color.Yellow);
        _renderer.DrawText($"Mode: {(_continuousEmission ? "Continuous" : "Burst")} (SPACE to toggle)", 10, 60, Color.Yellow);
        _renderer.DrawText("Left Click: Spawn burst at mouse", 10, 85, Color.Gray);

        // Draw particle count
        var emitters = _world.Query().With<ParticleEmitterComponent>().Execute();
        int totalParticles = 0;
        foreach (var entity in emitters)
        {
            var emitter = entity.GetComponent<ParticleEmitterComponent>();
            if (emitter != null)
            {
                totalParticles += emitter.ParticleCount;
            }
        }
        _renderer.DrawText($"Particles: {totalParticles}", 10, 110, new Color(0, 255, 100));
    }

    private void SwitchEffect(EffectType effect)
    {
        _currentEffect = effect;
        Logger.LogInformation("Switched to: {Effect}", effect);

        if (_currentEmitter != null)
        {
            _world.DestroyEntity(_currentEmitter);
        }

        CreateEffect(effect, new Vector2(640, 360));
    }

    private void CreateEffect(EffectType effect, Vector2 position)
    {
        var entity = _world.CreateEntity($"{effect}Emitter");

        var transform = entity.AddComponent<TransformComponent>();
        transform.Position = position;

        var emitter = entity.AddComponent<ParticleEmitterComponent>();
        emitter.IsEmitting = _continuousEmission;

        switch (effect)
        {
            case EffectType.Fire:
                ConfigureFire(emitter);
                break;
            case EffectType.Explosion:
                ConfigureExplosion(emitter);
                break;
            case EffectType.Smoke:
                ConfigureSmoke(emitter);
                break;
            case EffectType.Sparkles:
                ConfigureSparkles(emitter);
                break;
            case EffectType.Trail:
                ConfigureTrail(emitter);
                break;
        }

        _currentEmitter = entity;
    }

    private void CreateBurst(EffectType effect, Vector2 position)
    {
        var entity = _world.CreateEntity($"{effect}Burst");

        var transform = entity.AddComponent<TransformComponent>();
        transform.Position = position;

        var emitter = entity.AddComponent<ParticleEmitterComponent>();
        emitter.IsEmitting = true;
        emitter.MaxParticles = 100;

        switch (effect)
        {
            case EffectType.Fire:
                ConfigureFire(emitter);
                emitter.EmissionRate = 200f;
                break;
            case EffectType.Explosion:
                ConfigureExplosion(emitter);
                emitter.EmissionRate = 500f;
                break;
            case EffectType.Smoke:
                ConfigureSmoke(emitter);
                emitter.EmissionRate = 100f;
                break;
            case EffectType.Sparkles:
                ConfigureSparkles(emitter);
                emitter.EmissionRate = 150f;
                break;
            case EffectType.Trail:
                ConfigureTrail(emitter);
                emitter.EmissionRate = 100f;
                break;
        }

        var lifetime = entity.AddComponent<LifetimeComponent>();
        lifetime.Lifetime = 0.5f;
        lifetime.AutoDestroy = true;
    }

    private void ConfigureFire(ParticleEmitterComponent emitter)
    {
        emitter.EmissionRate = 50f;
        emitter.MaxParticles = 200;
        emitter.ParticleLifetime = 1.5f;
        emitter.LifetimeVariation = 0.3f;
        emitter.StartColor = new Color(255, 200, 0, 255);
        emitter.EndColor = new Color(255, 50, 0, 0);
        emitter.StartSize = 8f;
        emitter.EndSize = 2f;
        emitter.InitialVelocity = new Vector2(0, -50);
        emitter.VelocitySpread = 30f;
        emitter.SpeedVariation = 0.3f;
        emitter.Gravity = new Vector2(0, -20);
        emitter.SpawnRadius = 10f;
    }

    private void ConfigureExplosion(ParticleEmitterComponent emitter)
    {
        emitter.EmissionRate = 300f;
        emitter.MaxParticles = 300;
        emitter.ParticleLifetime = 1f;
        emitter.LifetimeVariation = 0.2f;
        emitter.StartColor = new Color(255, 255, 200, 255);
        emitter.EndColor = new Color(100, 50, 0, 0);
        emitter.StartSize = 10f;
        emitter.EndSize = 3f;
        emitter.InitialVelocity = new Vector2(100, 0);
        emitter.VelocitySpread = 360f;
        emitter.SpeedVariation = 0.5f;
        emitter.Gravity = new Vector2(0, 100);
        emitter.SpawnRadius = 5f;
    }

    private void ConfigureSmoke(ParticleEmitterComponent emitter)
    {
        emitter.EmissionRate = 30f;
        emitter.MaxParticles = 150;
        emitter.ParticleLifetime = 3f;
        emitter.LifetimeVariation = 0.5f;
        emitter.StartColor = new Color(150, 150, 150, 200);
        emitter.EndColor = new Color(80, 80, 80, 0);
        emitter.StartSize = 5f;
        emitter.EndSize = 15f;
        emitter.InitialVelocity = new Vector2(0, -30);
        emitter.VelocitySpread = 20f;
        emitter.SpeedVariation = 0.4f;
        emitter.Gravity = new Vector2(0, -10);
        emitter.SpawnRadius = 15f;
    }

    private void ConfigureSparkles(ParticleEmitterComponent emitter)
    {
        emitter.EmissionRate = 40f;
        emitter.MaxParticles = 200;
        emitter.ParticleLifetime = 2f;
        emitter.LifetimeVariation = 0.6f;
        emitter.StartColor = new Color(255, 255, 100, 255);
        emitter.EndColor = new Color(255, 200, 255, 0);
        emitter.StartSize = 6f;
        emitter.EndSize = 1f;
        emitter.InitialVelocity = new Vector2(0, -20);
        emitter.VelocitySpread = 360f;
        emitter.SpeedVariation = 0.7f;
        emitter.Gravity = new Vector2(0, 30);
        emitter.SpawnRadius = 20f;
    }

    private void ConfigureTrail(ParticleEmitterComponent emitter)
    {
        emitter.EmissionRate = 100f;
        emitter.MaxParticles = 300;
        emitter.ParticleLifetime = 0.8f;
        emitter.LifetimeVariation = 0.2f;
        emitter.StartColor = new Color(100, 200, 255, 255);
        emitter.EndColor = new Color(0, 100, 200, 0);
        emitter.StartSize = 6f;
        emitter.EndSize = 2f;
        emitter.InitialVelocity = new Vector2(0, 0);
        emitter.VelocitySpread = 10f;
        emitter.SpeedVariation = 0.5f;
        emitter.Gravity = new Vector2(0, 20);
        emitter.SpawnRadius = 3f;
    }
}