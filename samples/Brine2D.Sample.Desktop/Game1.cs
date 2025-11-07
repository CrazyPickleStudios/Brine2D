using Brine2D.Core.Components;
using Brine2D.Core.Graphics;
using Brine2D.Core.Hosting;
using Brine2D.Core.Input;
using Brine2D.Core.Math;
using Brine2D.Core.Runtime;
using Brine2D.Core.Timing;

namespace Brine2D.Sample.Desktop;

public sealed class Game1 : IGame
{
    private const float PlayerSpeed = 250f;

    // Edge detection for toggle keys
    private readonly HashSet<Key> _held = new();
    private readonly Transform2D _player = new();
    private CameraController2D _camCtrl = new();
    private IEngineContext _engine = null!;
    private bool _letterbox = true;
    private bool _manualPan;

    // Camera-side toggles (renderer is internal, so avoid direct casts)
    private bool _pixelSnap = true;
    private ITexture2D _tex = null!;

    public void Draw(GameTime time)
    {
        _engine.Renderer.Clear(Color.Black);

        // World (camera)
        _engine.Sprites.Begin(_camCtrl.Camera);

        // Center-origin draw at player position
        var origin = new Vector2(_tex.Width / 2f, _tex.Height / 2f);

        // Sample background and foreground draws (no renderer parallax here since renderer is internal)
        var bgDst = new Rectangle(-200, -120, _tex.Width, _tex.Height);
        _engine.Sprites.Draw(_tex, null, bgDst, new Color(200, 200, 255), 0f, origin);

        var playerDst = new Rectangle((int)_player.Position.X, (int)_player.Position.Y, _tex.Width, _tex.Height);
        _engine.Sprites.Draw(_tex, null, playerDst, Color.White, 0f, origin);

        var fgDst = new Rectangle(+200, +120, _tex.Width, _tex.Height);
        _engine.Sprites.Draw(_tex, null, fgDst, new Color(255, 220, 200), 0f, origin);

        _engine.Sprites.End();

        // UI (screen-space)
        _engine.Sprites.Begin();
        _engine.Sprites.Draw(_tex, null, new Rectangle(10, 10, _tex.Width, _tex.Height), Color.White);
        _engine.Sprites.End();
    }

    public void Initialize(IEngineContext engine)
    {
        _engine = engine;

        // Assets
        _tex = engine.Content.Load<ITexture2D>("images/brine2d_logo.png");

        // World setup
        _player.Position = new Vector2(0, 0);

        // Camera controller
        _camCtrl = new CameraController2D();
        _camCtrl.Initialize(_engine);
        _camCtrl.Target = _player; // follow the player
        _camCtrl.FollowLerp = 8f; // smooth follow (0 = instant)
        _camCtrl.PixelSnap = _pixelSnap; // crisp pixel-aligned movement at 0-rotation
        _camCtrl.EnableManualPan = _manualPan; // arrows move player by default
        _camCtrl.PanSpeed = 500f; // world units per second (when manual pan is enabled)
        _camCtrl.EnableWheelZoom = true; // mouse wheel zoom
        _camCtrl.MinZoom = 0.5f;
        _camCtrl.MaxZoom = 3f;
        _camCtrl.Zoom = 1f;

        // Virtual resolution (letterbox)
        if (_letterbox)
        {
            _camCtrl.VirtualWidth = 320;
            _camCtrl.VirtualHeight = 180;
        }
        else
        {
            _camCtrl.VirtualWidth = 0;
            _camCtrl.VirtualHeight = 0;
        }

        // Deadzone centered in virtual view
        _camCtrl.DeadZone = new Rectangle(110, 60, 100, 60);

        // World bounds (keep camera within this region)
        _camCtrl.WorldBounds = new Rectangle(-1000, -1000, 2000, 2000);

        // Start camera centered on player
        _camCtrl.Camera.Position = _player.Position;
    }

    public void Update(GameTime time)
    {
        var dt = (float)time.DeltaSeconds;

        // Camera toggles (edge-triggered; F2/F3/F4)
        if (WasPressed(Key.F2))
        {
            _pixelSnap = !_pixelSnap;
            _camCtrl.PixelSnap = _pixelSnap;
        }

        if (WasPressed(Key.F3))
        {
            _letterbox = !_letterbox;
            if (_letterbox)
            {
                _camCtrl.VirtualWidth = 320;
                _camCtrl.VirtualHeight = 180;
            }
            else
            {
                _camCtrl.VirtualWidth = 0;
                _camCtrl.VirtualHeight = 0;
            }
        }

        if (WasPressed(Key.F4))
        {
            _manualPan = !_manualPan;
            _camCtrl.EnableManualPan = _manualPan;
        }

        // Player movement in world space (disabled if manual pan is active)
        float dx = 0f, dy = 0f;
        if (!_manualPan)
        {
            if (_engine.Input.IsKeyDown(Key.Left))
            {
                dx -= 1f;
            }

            if (_engine.Input.IsKeyDown(Key.Right))
            {
                dx += 1f;
            }

            if (_engine.Input.IsKeyDown(Key.Up))
            {
                dy -= 1f;
            }

            if (_engine.Input.IsKeyDown(Key.Down))
            {
                dy += 1f;
            }
        }

        if (dx != 0f || dy != 0f)
        {
            var len = MathF.Sqrt(dx * dx + dy * dy);
            if (len > 0f)
            {
                dx /= len;
                dy /= len;
            }

            _player.Position = new Vector2(
                _player.Position.X + dx * PlayerSpeed * dt,
                _player.Position.Y + dy * PlayerSpeed * dt
            );
        }

        // Let the camera follow target, apply deadzone, wheel zoom, bounds, etc.
        _camCtrl.Update(time);
    }

    // Edge-triggered key press helper
    private bool WasPressed(Key key)
    {
        var down = _engine.Input.IsKeyDown(key);
        if (down && !_held.Contains(key))
        {
            _held.Add(key);
            return true;
        }

        if (!down && _held.Contains(key))
        {
            _held.Remove(key);
        }

        return false;
    }
}