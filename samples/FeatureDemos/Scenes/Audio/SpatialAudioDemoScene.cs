using Brine2D.Audio;
using Brine2D.Core;
using Brine2D.ECS;
using Brine2D.ECS.Components;
using Brine2D.Input;
using Brine2D.Rendering;
using Microsoft.Extensions.Logging;
using System.Numerics;
using Brine2D.Systems.Audio;
using Brine2D.Engine;
using Brine2D.Performance;
using Color = Brine2D.Core.Color;

namespace FeatureDemos.Scenes.Audio;

/// <summary>
/// Demo scene showcasing 2D spatial audio with distance attenuation and stereo panning.
/// </summary>
public class SpatialAudioDemoScene : DemoSceneBase
{
    private readonly IAudioService _audio;
    private readonly IInputContext _input;

    private Entity? _player;
    private Entity? _soundSource1;
    private Entity? _soundSource2;
    private Entity? _soundSource3;

    private ISoundEffect? _coinSound;
    private ISoundEffect? _explosionSound;
    private ISoundEffect? _ambientSound;

    private bool _soundsLoaded;

    public SpatialAudioDemoScene(
        IInputContext input,
        ISceneManager sceneManager,
        IGameContext gameContext,
        IAudioService audio,
        PerformanceOverlay? perfOverlay = null)
        : base(input, sceneManager, gameContext, perfOverlay)
    {
        _input = input;
        _audio = audio;
    }

    protected override async Task OnLoadAsync(CancellationToken cancellationToken, IProgress<float>? progress = null)
    {
        Logger.LogInformation("=== Spatial Audio Demo Scene ===");
        Logger.LogInformation("Move the player (WASD) to hear spatial audio!");
        Logger.LogInformation("Controls:");
        Logger.LogInformation("  WASD - Move player (audio listener)");
        Logger.LogInformation("  1-3 - Toggle sound sources");
        Logger.LogInformation("  ESC - Return to menu");
        Logger.LogInformation("");

        Renderer.ClearColor = new Color(20, 20, 30);

        await LoadSoundsAsync(cancellationToken);
        CreatePlayer();
        CreateSoundSources();
    }

    private async Task LoadSoundsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var soundsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "sounds");
            
            if (Directory.Exists(soundsPath))
            {
                var coinPath = Path.Combine(soundsPath, "coin.mp3");
                var explosionPath = Path.Combine(soundsPath, "explosion.mp3");
                var ambientPath = Path.Combine(soundsPath, "ambient.mp3");

                if (File.Exists(coinPath))
                {
                    _coinSound = await _audio.LoadSoundAsync(coinPath, cancellationToken);
                    Logger.LogInformation("Loaded coin sound");
                }

                if (File.Exists(explosionPath))
                {
                    _explosionSound = await _audio.LoadSoundAsync(explosionPath, cancellationToken);
                    Logger.LogInformation("Loaded explosion sound");
                }

                if (File.Exists(ambientPath))
                {
                    _ambientSound = await _audio.LoadSoundAsync(ambientPath, cancellationToken);
                    Logger.LogInformation("Loaded ambient sound");
                }

                _soundsLoaded = true;
            }
            else
            {
                Logger.LogInformation("No sounds directory found. Demo will show visualization only.");
                Logger.LogInformation("Add WAV files to assets/sounds/ to hear spatial audio!");
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to load sounds");
        }
    }

    private void CreatePlayer()
    {
        _player = World.CreateEntity("Player");

        _player.AddComponent<TransformComponent>();
        var transform = _player.GetComponent<TransformComponent>();
        transform.Position = new Vector2(640, 360);

        _player.AddComponent<AudioListenerComponent>();
        var listener = _player.GetComponent<AudioListenerComponent>();
        listener.GlobalSpatialVolume = 1.0f;
    }

    private void CreateSoundSources()
    {
        _soundSource1 = World.CreateEntity("CoinSource");
        _soundSource1.AddComponent<TransformComponent>();
        var transform1 = _soundSource1.GetComponent<TransformComponent>();
        transform1.Position = new Vector2(200, 360);

        _soundSource1.AddComponent<SoundEffectSourceComponent>();
        var audio1 = _soundSource1.GetComponent<SoundEffectSourceComponent>();
        audio1.SoundEffect = _coinSound;
        audio1.EnableSpatialAudio = true;
        audio1.MinDistance = 50f;
        audio1.MaxDistance = 500f;
        audio1.RolloffFactor = 1.0f;
        audio1.SpatialBlend = 1.0f;
        audio1.Volume = 0.5f;
        audio1.LoopCount = -1;
        audio1.PlayOnEnable = true;

        _soundSource2 = World.CreateEntity("ExplosionSource");
        _soundSource2.AddComponent<TransformComponent>();
        var transform2 = _soundSource2.GetComponent<TransformComponent>();
        transform2.Position = new Vector2(1080, 360);

        _soundSource2.AddComponent<SoundEffectSourceComponent>();
        var audio2 = _soundSource2.GetComponent<SoundEffectSourceComponent>();
        audio2.SoundEffect = _explosionSound;
        audio2.EnableSpatialAudio = true;
        audio2.MinDistance = 100f;
        audio2.MaxDistance = 600f;
        audio2.RolloffFactor = 2.0f;
        audio2.SpatialBlend = 1.0f;
        audio2.Volume = 0.8f;

        _soundSource3 = World.CreateEntity("AmbientSource");
        _soundSource3.AddComponent<TransformComponent>();
        var transform3 = _soundSource3.GetComponent<TransformComponent>();
        transform3.Position = new Vector2(640, 100);

        _soundSource3.AddComponent<SoundEffectSourceComponent>();
        var audio3 = _soundSource3.GetComponent<SoundEffectSourceComponent>();
        audio3.SoundEffect = _ambientSound;
        audio3.EnableSpatialAudio = true;
        audio3.MinDistance = 80f;
        audio3.MaxDistance = 400f;
        audio3.RolloffFactor = 1.5f;
        audio3.SpatialBlend = 0.7f;
        audio3.Volume = 0.3f;
        audio3.LoopCount = -1;
        audio3.PlayOnEnable = true;
    }

    protected override void OnUpdate(GameTime gameTime)
    {
        HandlePerformanceHotkeys();
        if (CheckReturnToMenu()) return;

        if (_player != null)
        {
            var transform = _player.GetComponent<TransformComponent>();
            if (transform != null)
            {
                var moveSpeed = 300f * (float)gameTime.DeltaTime;
                var movement = Vector2.Zero;

                if (_input.IsKeyDown(Key.W)) movement.Y -= moveSpeed;
                if (_input.IsKeyDown(Key.S)) movement.Y += moveSpeed;
                if (_input.IsKeyDown(Key.A)) movement.X -= moveSpeed;
                if (_input.IsKeyDown(Key.D)) movement.X += moveSpeed;

                transform.Position += movement;
                transform.Position = new Vector2(
                    Math.Clamp(transform.Position.X, 50, 1230),
                    Math.Clamp(transform.Position.Y, 50, 670));
            }
        }

        if (_input.IsKeyPressed(Key.D1) && _soundSource1 != null)
        {
            var audio = _soundSource1.GetComponent<SoundEffectSourceComponent>();
            if (audio != null)
            {
                audio.IsEnabled = !audio.IsEnabled;
                Logger.LogInformation("Sound 1 (Coin): {State}", audio.IsEnabled ? "ON" : "OFF");
            }
        }

        if (_input.IsKeyPressed(Key.D2) && _soundSource2 != null)
        {
            var audio = _soundSource2.GetComponent<SoundEffectSourceComponent>();
            if (audio != null)
            {
                audio.TriggerPlay = true;
                Logger.LogInformation("Sound 2 (Explosion) triggered!");
            }
        }

        if (_input.IsKeyPressed(Key.D3) && _soundSource3 != null)
        {
            var audio = _soundSource3.GetComponent<SoundEffectSourceComponent>();
            if (audio != null)
            {
                audio.IsEnabled = !audio.IsEnabled;
                Logger.LogInformation("Sound 3 (Ambient): {State}", audio.IsEnabled ? "ON" : "OFF");
            }
        }
    }

    protected override void OnRender(GameTime gameTime)
    {
        var y = 10f;
        var lineHeight = 20f;

        Renderer.DrawText("=== 2D Spatial Audio Demo ===", 10, y, Color.White);
        y += lineHeight * 2;

        Renderer.DrawText("Move the player (green) with WASD", 10, y, Color.Cyan);
        y += lineHeight;
        Renderer.DrawText("Hear sounds change based on distance & position", 10, y, Color.Cyan);
        y += lineHeight * 2;

        Renderer.DrawText("CONTROLS:", 10, y, Color.Yellow);
        y += lineHeight;
        Renderer.DrawText("  WASD - Move player (listener)", 10, y, Color.White);
        y += lineHeight;
        Renderer.DrawText("  1 - Toggle Coin (left)", 10, y, Color.White);
        y += lineHeight;
        Renderer.DrawText("  2 - Trigger Explosion (right)", 10, y, Color.White);
        y += lineHeight;
        Renderer.DrawText("  3 - Toggle Ambient (top)", 10, y, Color.White);
        y += lineHeight;
        Renderer.DrawText("  ESC - Return to menu", 10, y, Color.White);
        y += lineHeight * 2;

        if (!_soundsLoaded)
        {
            Renderer.DrawText("No sounds loaded - visualization only!", 10, y, Color.Orange);
            y += lineHeight;
            Renderer.DrawText("Add WAV files to assets/sounds/ folder", 10, y, Color.Orange);
            y += lineHeight;
        }

        if (_player != null)
        {
            var transform = _player.GetComponent<TransformComponent>();
            if (transform != null)
            {
                Renderer.DrawCircleFilled(transform.Position.X, transform.Position.Y, 15, Color.Green);
                Renderer.DrawText("Player", transform.Position.X - 20, transform.Position.Y - 30, Color.Green);
            }
        }

        DrawSoundSource(_soundSource1, "Coin", Color.Cyan);
        DrawSoundSource(_soundSource2, "Explosion", Color.Red);
        DrawSoundSource(_soundSource3, "Ambient", Color.Purple);

        if (_player != null)
        {
            var playerPos = _player.GetComponent<TransformComponent>()?.Position;
            if (playerPos.HasValue)
            {
                DrawConnectionLine(_soundSource1, playerPos.Value, Color.Cyan);
                DrawConnectionLine(_soundSource2, playerPos.Value, Color.Red);
                DrawConnectionLine(_soundSource3, playerPos.Value, Color.Purple);
            }
        }

        RenderPerformanceOverlay();
    }

    private void DrawSoundSource(Entity? source, string label, Color color)
    {
        if (source == null) return;

        var transform = source.GetComponent<TransformComponent>();
        var audio = source.GetComponent<SoundEffectSourceComponent>();
        
        if (transform == null || audio == null) return;

        var pos = transform.Position;
        var isActive = audio.IsEnabled;

        Renderer.DrawCircleOutline(pos.X, pos.Y, audio.MinDistance, new Color(color.R, color.G, color.B, 50), 1);
        Renderer.DrawCircleOutline(pos.X, pos.Y, audio.MaxDistance, new Color(color.R, color.G, color.B, 30), 1);

        var sourceColor = isActive ? color : new Color((byte)(color.R / 2), (byte)(color.G / 2), (byte)(color.B / 2));
        Renderer.DrawCircleFilled(pos.X, pos.Y, 10, sourceColor);
        Renderer.DrawText(label, pos.X - 20, pos.Y - 25, sourceColor);
        
        string status = audio.LoopCount == -1 
            ? (isActive ? "ON" : "OFF")
            : (audio.IsPlaying ? "FIRING" : "READY");
    
        Renderer.DrawText(status, pos.X - 20, pos.Y + 15, sourceColor);

        if (_player != null && isActive)
        {
            var playerPos = _player.GetComponent<TransformComponent>()?.Position;
            if (playerPos.HasValue)
            {
                var distance = Vector2.Distance(pos, playerPos.Value);
                Renderer.DrawText($"d:{distance:F0} v:{audio.SpatialVolume:F2} p:{audio.SpatialPan:F2}", 
                    pos.X - 40, pos.Y + 30, Color.White);
            }
        }
    }

    private void DrawConnectionLine(Entity? source, Vector2 playerPos, Color color)
    {
        if (source == null) return;

        var transform = source.GetComponent<TransformComponent>();
        var audio = source.GetComponent<SoundEffectSourceComponent>();
        
        if (transform == null || audio == null || !audio.IsEnabled) return;

        var lineColor = new Color(color.R, color.G, color.B, 100);
        Renderer.DrawLine(playerPos.X, playerPos.Y, transform.Position.X, transform.Position.Y, lineColor, 1);
    }
}