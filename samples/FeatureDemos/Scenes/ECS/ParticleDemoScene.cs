using System.Drawing;
using Brine2D.Core;
using Brine2D.ECS;
using Brine2D.ECS.Components;
using Brine2D.Input;
using Brine2D.Rendering;
using Brine2D.Rendering.TextureAtlas;
using Microsoft.Extensions.Logging;
using System.Numerics;
using Brine2D.Systems.Rendering;
using Brine2D.Engine;
using Brine2D.Performance;

namespace FeatureDemos.Scenes.ECS;

/// <summary>
/// Demo scene showcasing the enhanced particle system with textures, rotation, and trails.
/// Notice: NO manual pipeline calls - they execute automatically!
/// Notice: NO frame management - SceneManager handles it!
/// </summary>
public class ParticleDemoScene : DemoSceneBase
{
    private readonly IEntityWorld _world;
    private readonly IRenderer _renderer;
    private readonly IInputService _input;
    private readonly IGameContext _gameContext;
    private readonly ITextureLoader _textureLoader;
    private readonly ITextureAtlasBuilder? _atlasBuilder;

    private Entity? _currentEmitter;
    private EffectType _currentEffect = EffectType.Fire;
    private bool _continuousEmission = true;
    private ITextureAtlasCollection? _particleAtlas;
    private ITexture? _particleTexture;

    private enum EffectType
    {
        Fire,
        Explosion,
        Smoke,
        Sparkles,
        Trail,
        TexturedFire,     
        RotatingSparkles,  
        MagicTrail        
    }

    public ParticleDemoScene(
        IEntityWorld world,
        IRenderer renderer,
        IInputService input,
        ISceneManager sceneManager,
        IGameContext gameContext,
        ILogger<ParticleDemoScene> logger,
        ITextureLoader textureLoader,
        ITextureAtlasBuilder? atlasBuilder = null,
        PerformanceOverlay? perfOverlay = null) 
        : base(input, sceneManager, gameContext, logger, renderer, world, perfOverlay)
    {
        _world = world;
        _renderer = renderer;
        _input = input;
        _gameContext = gameContext;
        _textureLoader = textureLoader;
        _atlasBuilder = atlasBuilder;
    }

    protected override async Task OnInitializeAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("=== Enhanced Particle Demo Scene ===");
        Logger.LogInformation("NEW: Texture support, rotation, and trails!");
        Logger.LogInformation("Controls:");
        Logger.LogInformation("  1-8 - Switch particle effects");
        Logger.LogInformation("  Left Click - Spawn particle burst at mouse");
        Logger.LogInformation("  SPACE - Toggle continuous emission");
        Logger.LogInformation("  ESC - Exit");
        Logger.LogInformation("");

        // Set clear color for this scene
        _renderer.ClearColor = Color.FromArgb(15, 15, 25);

        // Try to load particle textures
        await LoadParticleAssetsAsync(cancellationToken);

        CreateEffect(_currentEffect, new Vector2(640, 360));
    }

    private async Task LoadParticleAssetsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var particlesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "particles");
            
            if (_atlasBuilder != null && Directory.Exists(particlesPath) && Directory.GetFiles(particlesPath, "*.png").Length > 0)
            {
                Logger.LogInformation("Loading particle textures from: {Path}", particlesPath);
                
                _particleAtlas = await _atlasBuilder
                    .WithName("ParticleAtlas")
                    .AddFolder(particlesPath, "*.png", recursive: false)
                    .WithMaxSize(1024, 1024)
                    .WithPadding(2)
                    .WithPowerOfTwo(true)
                    .WithScaleMode(TextureScaleMode.Linear) // Linear for smooth particles
                    .BuildAsync(cancellationToken);
                
                Logger.LogInformation("Particle atlas created with {RegionCount} textures", _particleAtlas.TotalRegionCount);
            }
            else
            {
                Logger.LogInformation("No particle textures found. Using procedural particles.");
                Logger.LogInformation("Add PNG files to assets/particles/ to see textured particles!");
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to load particle textures. Using procedural particles.");
        }
    }

    protected override void OnUpdate(GameTime gameTime)
    {
        HandlePerformanceHotkeys();
        if (CheckReturnToMenu()) return;

        // Switch effects (1-8 keys)
        if (_input.IsKeyPressed(Keys.D1)) SwitchEffect(EffectType.Fire);
        if (_input.IsKeyPressed(Keys.D2)) SwitchEffect(EffectType.Explosion);
        if (_input.IsKeyPressed(Keys.D3)) SwitchEffect(EffectType.Smoke);
        if (_input.IsKeyPressed(Keys.D4)) SwitchEffect(EffectType.Sparkles);
        if (_input.IsKeyPressed(Keys.D5)) SwitchEffect(EffectType.Trail);
        if (_input.IsKeyPressed(Keys.D6)) SwitchEffect(EffectType.TexturedFire);
        if (_input.IsKeyPressed(Keys.D7)) SwitchEffect(EffectType.RotatingSparkles);
        if (_input.IsKeyPressed(Keys.D8)) SwitchEffect(EffectType.MagicTrail);

        // Toggle continuous emission
        if (_input.IsKeyPressed(Keys.Space))
        {
            // Explosion is burst-only, don't allow continuous emission toggle
            if (_currentEffect == EffectType.Explosion)
            {
                Logger.LogInformation("Explosion is burst-only mode (click to spawn)");
                return; // Early exit, don't change anything
            }
            
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
            var mousePos = _input.MousePosition;
            SpawnBurst(mousePos, _currentEffect);
        }
    }

    protected override void OnRender(GameTime gameTime)
    {
        var y = 10f;
        var lineHeight = 20f;

        // Title
        _renderer.DrawText($"Enhanced Particle System", 10, y, Color.White);
        y += lineHeight;
        _renderer.DrawText($"Current Effect: {_currentEffect}", 10, y, Color.Cyan);
        y += lineHeight * 2;

        // Effect selection menu
        _renderer.DrawText("SELECT EFFECT:", 10, y, Color.Yellow);
        y += lineHeight;
        
        DrawEffectOption(1, "Fire", EffectType.Fire, ref y);
        DrawEffectOption(2, "Explosion", EffectType.Explosion, ref y);
        DrawEffectOption(3, "Smoke", EffectType.Smoke, ref y);
        DrawEffectOption(4, "Sparkles", EffectType.Sparkles, ref y);
        DrawEffectOption(5, "Trail", EffectType.Trail, ref y);
        DrawEffectOption(6, "Textured Fire", EffectType.TexturedFire, ref y);
        DrawEffectOption(7, "Rotating Sparkles", EffectType.RotatingSparkles, ref y);
        DrawEffectOption(8, "Magic Trail", EffectType.MagicTrail, ref y);
        
        y += lineHeight;

        _renderer.DrawText("STATUS:", 10, y, Color.Yellow);
        y += lineHeight;
        
        var emitter = _currentEmitter?.GetComponent<ParticleEmitterComponent>();
        if (emitter != null)
        {
            _renderer.DrawText($"  Active Particles: {emitter.ParticleCount} / {emitter.MaxParticles}", 10, y, Color.White);
            y += lineHeight;
            
            var allEmitters = _world.GetEntitiesWithComponent<ParticleEmitterComponent>();
            var totalParticles = allEmitters.Sum(e => e.GetComponent<ParticleEmitterComponent>()?.ParticleCount ?? 0);
            _renderer.DrawText($"  Total Particles: {totalParticles}", 10, y, Color.White);
            y += lineHeight;
            
            var fps = gameTime.DeltaTime > 0 ? 1.0 / gameTime.DeltaTime : 0;
            var fpsColor = fps >= 60 ? Color.Green : fps >= 30 ? Color.Yellow : Color.Red;
            _renderer.DrawText($"  FPS: {fps:F1}", 10, y, fpsColor);
            y += lineHeight;
            
            var emissionModeText = _currentEffect == EffectType.Explosion 
                ? "BURST ONLY" 
                : (_continuousEmission ? "ON" : "OFF");
            var emissionModeColor = _currentEffect == EffectType.Explosion 
                ? Color.Orange 
                : (_continuousEmission ? Color.Green : Color.Gray);
            _renderer.DrawText($"  Continuous Emission: {emissionModeText}", 10, y, emissionModeColor);
            y += lineHeight;
            
            _renderer.DrawText($"  Trails: {(emitter.EnableTrails ? "ENABLED" : "DISABLED")}", 10, y, 
                emitter.EnableTrails ? Color.Green : Color.Gray);
            y += lineHeight;
            _renderer.DrawText($"  Rotation: {(emitter.RotationSpeed != 0 ? "ENABLED" : "DISABLED")}", 10, y, 
                emitter.RotationSpeed != 0 ? Color.Green : Color.Gray);
            y += lineHeight;
            _renderer.DrawText($"  Textured: {(emitter.ParticleAtlasRegion != null || emitter.ParticleTexture != null ? "YES" : "NO")}", 10, y, 
                (emitter.ParticleAtlasRegion != null || emitter.ParticleTexture != null) ? Color.Green : Color.Gray);
            y += lineHeight;
        }

        y += lineHeight;

        // Controls
        _renderer.DrawText("CONTROLS:", 10, y, Color.Yellow);
        y += lineHeight;
        _renderer.DrawText("  1-8 - Switch effects", 10, y, Color.White);
        y += lineHeight;
        _renderer.DrawText("  SPACE - Toggle emission", 10, y, Color.White);
        y += lineHeight;
        _renderer.DrawText("  Left Click - Spawn burst", 10, y, Color.White);
        y += lineHeight;
        _renderer.DrawText("  F1/F3 - Performance overlay", 10, y, Color.White);
        y += lineHeight;
        _renderer.DrawText("  ESC - Return to menu", 10, y, Color.White);
        
        y += lineHeight * 2;

        // Texture info
        var hasTextures = _particleAtlas != null || _particleTexture != null;
        if (!hasTextures)
        {
            _renderer.DrawText("TIP: Add PNG files to assets/particles/", 10, y, Color.Yellow);
            y += lineHeight;
            _renderer.DrawText("     to enable textured particles!", 10, y, Color.Yellow);
        }

        RenderPerformanceOverlay();
    }

    private void DrawEffectOption(int number, string name, EffectType effectType, ref float y)
    {
        var isSelected = _currentEffect == effectType;
        var color = isSelected ? Color.Cyan : Color.Gray;
        var prefix = isSelected ? ">" : " ";
        
        _renderer.DrawText($"{prefix} {number}. {name}", 10, y, color);
        y += 20f;
    }

    private void SwitchEffect(EffectType effect)
    {
        _currentEffect = effect;
        Logger.LogInformation("Switched to: {Effect}", effect);
        
        // Clear old emitter
        if (_currentEmitter != null)
        {
            _world.DestroyEntity(_currentEmitter);
        }
        
        var centerPosition = new Vector2(640, 360);
        
        // Create new effect at center
        CreateEffect(effect, centerPosition);
        
        // Explosion gets an initial burst at center!
        if (effect == EffectType.Explosion)
        {
            SpawnBurst(centerPosition, effect);
            Logger.LogInformation("Initial explosion burst!");
        }
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
                emitter.IsEmitting = false; 
                break;
            
            case EffectType.Smoke:
                ConfigureSmoke(emitter);
                break;
            
            case EffectType.Sparkles:
                ConfigureSparkles(emitter);
                break;
            
            case EffectType.Trail:
                ConfigureTrailEffect(emitter);
                break;
            
            case EffectType.TexturedFire:
                ConfigureTexturedFire(emitter);
                break;
            
            case EffectType.RotatingSparkles:
                ConfigureRotatingSparkles(emitter);
                break;
            
            case EffectType.MagicTrail:
                ConfigureMagicTrail(emitter);
                break;
        }
        
        _currentEmitter = entity;
    }

    // Original effects (unchanged)
    private void ConfigureFire(ParticleEmitterComponent emitter)
    {
        emitter.EmissionRate = 50f;
        emitter.MaxParticles = 200;
        emitter.ParticleLifetime = 1.5f;
        emitter.StartColor = Color.FromArgb(255, 200, 50);
        emitter.EndColor = Color.FromArgb(0, 255, 50, 0);
        emitter.StartSize = 6f;
        emitter.EndSize = 2f;
        emitter.InitialVelocity = new Vector2(0, -80);
        emitter.VelocitySpread = 30f;
        emitter.Gravity = new Vector2(0, -20);
        emitter.SpawnRadius = 10f;
        emitter.BlendMode = BlendMode.Additive; // Makes fire glow!
        emitter.Shape = EmitterShape.Box;
        emitter.ShapeSize = new Vector2(20f, 5f); // Wider spawn area
    }

    private void ConfigureExplosion(ParticleEmitterComponent emitter)
    {
        emitter.EmissionRate = 200f;
        emitter.MaxParticles = 100;
        emitter.ParticleLifetime = 0.8f;
        emitter.LifetimeVariation = 0.3f;
        emitter.StartColor = Color.FromArgb(255, 255, 200);
        emitter.EndColor = Color.FromArgb(0, 255, 100, 0);
        emitter.StartSize = 8f;
        emitter.EndSize = 0f;
        emitter.InitialVelocity = new Vector2(100, 0);
        emitter.VelocitySpread = 180f;
        emitter.SpeedVariation = 0.8f;
        emitter.Gravity = new Vector2(0, 200);
        emitter.SpawnRadius = 5f;
        emitter.BlendMode = BlendMode.Additive; // Makes explosion bright!
        emitter.Shape = EmitterShape.Point; // Explode from center
    }

    private void ConfigureSmoke(ParticleEmitterComponent emitter)
    {
        emitter.EmissionRate = 20f;
        emitter.MaxParticles = 100;
        emitter.ParticleLifetime = 3f;
        emitter.StartColor = Color.FromArgb(200, 100, 100, 100);
        emitter.EndColor = Color.FromArgb(0, 50, 50, 50);
        emitter.StartSize = 4f;
        emitter.EndSize = 12f;
        emitter.InitialVelocity = new Vector2(0, -30);
        emitter.VelocitySpread = 45f;
        emitter.Gravity = new Vector2(0, -10);
        emitter.SpawnRadius = 5f;
        emitter.BlendMode = BlendMode.Alpha; // Normal blending for smoke
        emitter.Shape = EmitterShape.Circle;
    }

    private void ConfigureSparkles(ParticleEmitterComponent emitter)
    {
        emitter.EmissionRate = 30f;
        emitter.MaxParticles = 150;
        emitter.ParticleLifetime = 2f;
        emitter.StartColor = Color.FromArgb(255, 255, 100);
        emitter.EndColor = Color.FromArgb(0, 100, 200, 255);
        emitter.StartSize = 3f;
        emitter.EndSize = 1f;
        emitter.InitialVelocity = new Vector2(0, -50);
        emitter.VelocitySpread = 60f;
        emitter.Gravity = new Vector2(0, 50);
        emitter.SpawnRadius = 15f;
        emitter.BlendMode = BlendMode.Additive; // Sparkly!
        emitter.Shape = EmitterShape.Cone;
        emitter.ConeAngle = 60f;
    }

    private void ConfigureTrailEffect(ParticleEmitterComponent emitter)
    {
        emitter.EmissionRate = 100f;
        emitter.MaxParticles = 200;
        emitter.ParticleLifetime = 1f;
        emitter.StartColor = Color.FromArgb(100, 255, 255);
        emitter.EndColor = Color.FromArgb(0, 100, 100, 255);
        emitter.StartSize = 4f;
        emitter.EndSize = 2f;
        emitter.InitialVelocity = new Vector2(150, 0);
        emitter.VelocitySpread = 20f;
        emitter.Gravity = new Vector2(0, 100);
        emitter.SpawnRadius = 2f;
    }

    private void ConfigureTexturedFire(ParticleEmitterComponent emitter)
    {
        ConfigureFire(emitter); // Start with fire config
        
        // Add texture if available
        if (_particleAtlas != null && _particleAtlas.ContainsRegion("particle"))
        {
            emitter.ParticleAtlasRegion = _particleAtlas.GetRegion("particle");
            Logger.LogInformation("Using textured particles from atlas");
        }
        else if (_particleTexture != null)
        {
            emitter.ParticleTexture = _particleTexture;
            Logger.LogInformation("Using standalone particle texture");
        }
    }
    private void ConfigureRotatingSparkles(ParticleEmitterComponent emitter)
    {
        ConfigureSparkles(emitter); // Start with sparkles config
        
        // Add rotation
        emitter.InitialRotation = 0f;
        emitter.InitialRotationVariation = 1f; // Random initial rotation
        emitter.RotationSpeed = 3f; // 3 radians/sec
        emitter.RotationSpeedVariation = 0.5f; // ±50% variation
        
        // Add texture if available
        if (_particleAtlas != null && _particleAtlas.ContainsRegion("spark"))
        {
            emitter.ParticleAtlasRegion = _particleAtlas.GetRegion("spark");
        }
    }

    private void ConfigureMagicTrail(ParticleEmitterComponent emitter)
    {
        emitter.EmissionRate = 60f;
        emitter.MaxParticles = 150;
        emitter.ParticleLifetime = 2f;
        emitter.StartColor = Color.FromArgb(255, 100, 255);
        emitter.EndColor = Color.FromArgb(0, 100, 100, 255);
        emitter.StartSize = 5f;
        emitter.EndSize = 2f;
        emitter.InitialVelocity = new Vector2(0, -100);
        emitter.VelocitySpread = 30f;
        emitter.Gravity = new Vector2(0, 20);
        emitter.SpawnRadius = 8f;
        
        // Enable trails!
        emitter.EnableTrails = true;
        emitter.TrailLength = 8;
        emitter.TrailStartAlpha = 0.8f;
        emitter.TrailEndAlpha = 0.0f;
        
        // Add rotation for extra flair
        emitter.InitialRotationVariation = 1f;
        emitter.RotationSpeed = 2f;
        emitter.RotationSpeedVariation = 0.5f;
    }

    private void SpawnBurst(Vector2 position, EffectType effect)
    {
        Logger.LogInformation("Spawning burst at {X}, {Y}", position.X, position.Y);
        
        var entity = _world.CreateEntity("Burst");
        
        // Add components first, configure after (original pattern)
        var transform = entity.AddComponent<TransformComponent>();
        transform.Position = position;
        
        var emitter = entity.AddComponent<ParticleEmitterComponent>();
        emitter.IsEmitting = true;
        
        // Configure based on effect type
        switch (effect)
        {
            case EffectType.Fire:
            case EffectType.TexturedFire:
                ConfigureFire(emitter);
                emitter.EmissionRate = 200f; // Burst rate
                break;
        
            case EffectType.Explosion:
                ConfigureExplosion(emitter);
                emitter.EmissionRate = 500f; // Burst rate
                break;
        
            case EffectType.Smoke:
                ConfigureSmoke(emitter);
                emitter.EmissionRate = 100f; // Burst rate
                break;
        
            case EffectType.Sparkles:
            case EffectType.RotatingSparkles:
                ConfigureSparkles(emitter);
                emitter.EmissionRate = 150f; // Burst rate
                if (effect == EffectType.RotatingSparkles)
                {
                    emitter.InitialRotationVariation = 1f;
                    emitter.RotationSpeed = 3f;
                }
                break;
        
            case EffectType.Trail:
            case EffectType.MagicTrail:
                ConfigureTrailEffect(emitter);
                emitter.EmissionRate = 100f; // Burst rate
                if (effect == EffectType.MagicTrail)
                {
                    emitter.EnableTrails = true;
                    emitter.TrailLength = 5;
                }
                break;
        }
        
        // Apply textures for textured effects
        if (effect == EffectType.TexturedFire || effect == EffectType.RotatingSparkles)
        {
            if (_particleAtlas != null && _particleAtlas.ContainsRegion("particle"))
            {
                emitter.ParticleAtlasRegion = _particleAtlas.GetRegion("particle");
            }
        }
        
        // Add lifetime component for auto-cleanup (original pattern)
        var lifetime = entity.AddComponent<LifetimeComponent>();
        lifetime.Lifetime = 0.5f;
        lifetime.AutoDestroy = true;
    }
}