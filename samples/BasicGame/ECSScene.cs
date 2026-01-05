using System.Numerics;
using Brine2D.Audio;
using Brine2D.Audio.ECS;
using Brine2D.Core;
using Brine2D.Core.Collision;
using Brine2D.ECS;
using Brine2D.ECS.Components;
using Brine2D.ECS.Serialization;
using Brine2D.Rendering.ECS;
using Brine2D.Input.ECS;
using Brine2D.ECS.Systems;
using Brine2D.Input;
using Brine2D.Rendering;
using Microsoft.Extensions.Logging;

namespace BasicGame;

public class ECSScene : Scene
{
    private readonly IEntityWorld _world;
    private readonly IRenderer _renderer;
    private readonly ICameraManager _cameraManager;
    private readonly ITextureLoader _textureLoader;
    private readonly IAudioService _audioService;
    private readonly IInputService _input;
    private readonly IGameContext _gameContext;

    private readonly SpriteRenderingSystem _spriteRenderer;
    private readonly CameraSystem _cameraSystem;
    private readonly VelocitySystem _velocitySystem;
    private readonly PhysicsSystem _physicsSystem;
    private readonly PlayerControllerSystem _playerControllerSystem;
    private readonly AISystem _aiSystem;
    private readonly ParticleSystem _particleSystem;
    private readonly AudioSystem _audioSystem;
    private readonly DebugRenderer _debugRenderer;

    private readonly PrefabLibrary _prefabLibrary;
    private readonly EntitySerializer _serializer;

    private Entity? _player;
    private ICamera? _mainCamera;
    private ICamera? _minimapCamera;

    private bool _showDebug = true;

    public ECSScene(
        IEntityWorld world,
        IRenderer renderer,
        ICameraManager cameraManager,
        ITextureLoader textureLoader,
        IAudioService audioService,
        IInputService input,
        IGameContext gameContext,
        SpriteRenderingSystem spriteRenderer,
        CameraSystem cameraSystem,
        VelocitySystem velocitySystem,
        PhysicsSystem physicsSystem,
        PlayerControllerSystem playerControllerSystem,
        AISystem aiSystem,
        ParticleSystem particleSystem,
        AudioSystem audioSystem,
        DebugRenderer debugRenderer,
        PrefabLibrary prefabLibrary,
        EntitySerializer serializer,
        ILogger<ECSScene> logger) : base(logger)
    {
        _world = world;
        _renderer = renderer;
        _cameraManager = cameraManager;
        _textureLoader = textureLoader;
        _audioService = audioService;
        _input = input;
        _gameContext = gameContext;
        _spriteRenderer = spriteRenderer;
        _cameraSystem = cameraSystem;
        _velocitySystem = velocitySystem;
        _physicsSystem = physicsSystem;
        _playerControllerSystem = playerControllerSystem;
        _aiSystem = aiSystem;
        _particleSystem = particleSystem;
        _audioSystem = audioSystem;
        _debugRenderer = debugRenderer;
        _prefabLibrary = prefabLibrary;
        _serializer = serializer;
    }

    protected override void OnInitialize()
    {
        Logger.LogInformation("ECS Scene initialized!");

        _mainCamera = _cameraManager.GetCamera("main");

        _minimapCamera = new Camera2D(200, 200);
        _cameraManager.RegisterCamera("minimap", _minimapCamera);
        if (_minimapCamera != null)
        {
            _minimapCamera.Zoom = 0.2f;
        }

        // Create player prefab with new API
        var playerPrefab = new EntityPrefab("Player");
        playerPrefab.Tags.Add("Player");

        playerPrefab.AddComponent<TransformComponent>(t =>
        {
            t.Position = Vector2.Zero;
            t.Scale = Vector2.One;
        });

        playerPrefab.AddComponent<SpriteComponent>(s =>
        {
            s.TexturePath = "assets/sprites/player.png";
            s.Tint = Color.White;
        });

        playerPrefab.AddComponent<VelocityComponent>(v =>
        {
            v.MaxSpeed = 300f;
            v.Friction = 5f;
        });

        playerPrefab.AddComponent<PlayerControllerComponent>(pc =>
        {
            pc.MoveSpeed = 200f;
            pc.InputMode = InputMode.KeyboardAndGamepad;
        });

        playerPrefab.AddComponent<ColliderComponent>(c =>
        {
            c.Shape = new BoxCollider(32, 48);
        });

        playerPrefab.AddComponent<CameraFollowComponent>(cf =>
        {
            cf.CameraName = "main";
            cf.Smoothing = 5f;
        });

        playerPrefab.AddComponent<ParticleEmitterComponent>(pe =>
        {
            pe.EmissionRate = 20f;
            pe.ParticleLifetime = 1f;
            pe.StartColor = new Color(255, 200, 0); // Orange
            pe.EndColor = new Color(255, 0, 0, 0); // Fade to transparent
            pe.InitialVelocity = new Vector2(0, -30);
            pe.VelocitySpread = 360f;
            pe.Gravity = new Vector2(0, 50);
            pe.SpawnRadius = 10f;
            pe.IsEmitting = true;
        });

        playerPrefab.AddComponent<AudioSourceComponent>(a =>
        {
            a.Volume = 1.0f;
        });

        // Register prefab
        _prefabLibrary.Register(playerPrefab);

        // Instantiate player at specific position
        _player = playerPrefab.Instantiate(_world, new Vector2(640, 360));

        // Create enemy prefab
        var enemyPrefab = new EntityPrefab("Enemy");
        enemyPrefab.Tags.Add("Enemy");

        enemyPrefab.AddComponent<TransformComponent>();

        enemyPrefab.AddComponent<SpriteComponent>(s =>
        {
            s.TexturePath = "assets/sprites/enemy.png";
            s.Tint = new Color(255, 100, 100);
        });

        enemyPrefab.AddComponent<VelocityComponent>(v =>
        {
            v.MaxSpeed = 150f;
        });

        enemyPrefab.AddComponent<AIControllerComponent>(ai =>
        {
            ai.Behavior = AIBehavior.Chase;
            ai.MoveSpeed = 100f;
            ai.TargetTag = "Player";
            ai.DetectionRange = 400f;
            ai.StopDistance = 50f;
        });

        _prefabLibrary.Register(enemyPrefab);

        // Spawn 5 enemies at random positions
        var random = new Random();
        for (int i = 0; i < 5; i++)
        {
            var randomPos = new Vector2(
                random.Next(200, 1000),
                random.Next(200, 600));

            enemyPrefab.Instantiate(_world, randomPos);
        }

        Logger.LogInformation("Spawned player + 5 enemies from prefabs!");
    }

    protected override async Task OnLoadAsync(CancellationToken cancellationToken)
    {
        await _spriteRenderer.LoadTexturesAsync(cancellationToken);

        // Load jump sound effect
        var jumpSoundPath = "assets/audio/jump.mp3";
        if (File.Exists(jumpSoundPath))
        {
            var jumpSound = await _audioService.LoadSoundAsync(jumpSoundPath, cancellationToken);
            var audioSource = _player?.GetComponent<AudioSourceComponent>();
            if (audioSource != null)
            {
                audioSource.SoundEffect = jumpSound;
            }
        }
        else
        {
            Logger.LogWarning("Jump sound not found at: {Path}", jumpSoundPath);
        }
    }

    protected override void OnUpdate(GameTime gameTime)
    {
        // Save game
        if (_input.IsKeyPressed(Keys.F5))
        {
            _ = Task.Run(async () =>
            {
                await _serializer.SaveWorldAsync(_world, "saves/quicksave.json");
                Logger.LogInformation("Game saved!");
            });
        }

        // Load game (now actually works!)
        if (_input.IsKeyPressed(Keys.F9))
        {
            _ = Task.Run(async () =>
            {
                await _serializer.LoadAndRestoreWorldAsync(_world, "saves/quicksave.json");
                Logger.LogInformation("Game loaded and restored!");
            });
        }

        // Toggle debug view
        if (_input.IsKeyPressed(Keys.F3))
        {
            _showDebug = !_showDebug;
            Logger.LogInformation("Debug overlay: {State}", _showDebug ? "ON" : "OFF");
        }

        // Trigger jump sound
        if (_input.IsKeyPressed(Keys.Space))
        {
            var audioSource = _player?.GetComponent<AudioSourceComponent>();
            if (audioSource != null)
            {
                audioSource.TriggerPlay = true;
            }
        }

        // Update systems in correct order
        _playerControllerSystem.Update(gameTime);
        _aiSystem.Update(gameTime);
        _particleSystem.Update(gameTime);
        _audioSystem.Update(gameTime);
        _velocitySystem.Update(gameTime);
        _physicsSystem.Update(gameTime);
        _cameraSystem.Update(gameTime);
        _world.Update(gameTime);
    }

    protected override void OnRender(GameTime gameTime)
    {
        _renderer.Clear(new Color(40, 40, 40));
        _renderer.BeginFrame();

        _renderer.Camera = _mainCamera;
        _spriteRenderer.Render(_renderer);
        _particleSystem.Render(_renderer);

        // Draw debug overlay
        if (_showDebug)
        {
            _debugRenderer.Render(_renderer);
        }

        _renderer.Camera = null;

        _renderer.EndFrame();
    }
}