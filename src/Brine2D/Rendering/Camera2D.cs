using System.Numerics;
using Brine2D.Core;

namespace Brine2D.Rendering;

/// <summary>
/// A 2D camera for viewing and navigating the game world.
/// </summary>
public class Camera2D : ICamera, ITrackableCamera, IDisposable
{
    private Vector2 _position;
    private float _zoom = 1.0f;
    private float _rotation;
    private int _viewportWidth;
    private int _viewportHeight;
    private ICameraManager? _manager;
    private string? _registeredName;

    private float _shakeIntensity;
    private float _shakeDuration;
    private float _shakeElapsed;
    private readonly Random _shakeRng = new();

    public Vector2 Position
    {
        get => _position;
        set => _position = value;
    }

    public float Zoom
    {
        get => _zoom;
        set => _zoom = Math.Max(0.01f, value); // Prevent zero/negative zoom
    }

    public float Rotation
    {
        get => _rotation;
        set => _rotation = value;
    }

    public int ViewportWidth => _viewportWidth;
    public int ViewportHeight => _viewportHeight;

    /// <summary>
    /// Gets the center of the viewport in screen coordinates.
    /// </summary>
    public Vector2 ViewportCenter => new Vector2(_viewportWidth / 2f, _viewportHeight / 2f);

    public Camera2D(int viewportWidth, int viewportHeight)
    {
        _viewportWidth = viewportWidth;
        _viewportHeight = viewportHeight;
    }

    public void SetViewport(int width, int height)
    {
        _viewportWidth = width;
        _viewportHeight = height;
    }

    public Matrix4x4 GetViewMatrix()
    {
        var shakeOffset = GetShakeOffset();
        var origin = new Vector3(-_position.X + shakeOffset.X, -_position.Y + shakeOffset.Y, 0);
        var center = new Vector3(ViewportCenter.X, ViewportCenter.Y, 0);

        var translationToOrigin = Matrix4x4.CreateTranslation(origin);
        var rotation = Matrix4x4.CreateRotationZ(MathF.PI / 180f * _rotation);
        var scale = Matrix4x4.CreateScale(_zoom, _zoom, 1);
        var translationToCenter = Matrix4x4.CreateTranslation(center);

        return translationToOrigin * rotation * scale * translationToCenter;
    }

    public Vector2 WorldToScreen(Vector2 worldPosition)
    {
        var viewMatrix = GetViewMatrix();
        var worldPos4 = new Vector4(worldPosition.X, worldPosition.Y, 0, 1);
        var screenPos4 = Vector4.Transform(worldPos4, viewMatrix);
        
        return new Vector2(screenPos4.X, screenPos4.Y);
    }

    public Rectangle GetVisibleBounds()
    {
        // Calculate half-extents in world space
        var halfWidth = ViewportWidth / (2.0f * Zoom);
        var halfHeight = ViewportHeight / (2.0f * Zoom);

        return new Rectangle(
            (int)(Position.X - halfWidth),
            (int)(Position.Y - halfHeight),
            (int)(halfWidth * 2),
            (int)(halfHeight * 2)
        );
    }

    public bool IsVisible(Vector2 worldPosition)
    {
        var bounds = GetVisibleBounds();
        return bounds.Contains((int)worldPosition.X, (int)worldPosition.Y);
    }

    public bool IsVisible(Rectangle worldBounds)
    {
        var cameraBounds = GetVisibleBounds();
        return cameraBounds.Intersects(worldBounds);
    }

    public void ClampToBounds(Rectangle worldBounds)
    {
        var visibleBounds = GetVisibleBounds();

        // Calculate how much we can move
        var halfWidth = visibleBounds.Width / 2f;
        var halfHeight = visibleBounds.Height / 2f;

        // Clamp position
        Position = new Vector2(
            Math.Clamp(Position.X, worldBounds.X + halfWidth, worldBounds.X + worldBounds.Width - halfWidth),
            Math.Clamp(Position.Y, worldBounds.Y + halfHeight, worldBounds.Y + worldBounds.Height - halfHeight)
        );
    }

    public void FollowSmooth(Vector2 targetPosition, float smoothing, float deltaTime)
    {
        if (smoothing <= 0f) { Position = targetPosition; return; }
        // Exponential decay; frame-rate independent. smoothing=5 is responsive, smoothing=1 is sluggish.
        var lerpFactor = 1f - MathF.Exp(-smoothing * deltaTime);
        Position = Vector2.Lerp(Position, targetPosition, lerpFactor);
    }

    public void ZoomSmooth(float targetZoom, float smoothing, float deltaTime)
    {
        if (smoothing <= 0f) { Zoom = targetZoom; return; }
        // Same exponential decay as FollowSmooth; frame-rate independent.
        var lerpFactor = 1f - MathF.Exp(-smoothing * deltaTime);
        Zoom = Zoom + (targetZoom - Zoom) * lerpFactor;
    }

    public Vector2 ScreenToWorld(Vector2 screenPosition)
    {
        // Invert the view matrix to go from screen to world
        Matrix4x4.Invert(GetViewMatrix(), out var inverseView);
        
        var screenPos4 = new Vector4(screenPosition.X, screenPosition.Y, 0, 1);
        var worldPos4 = Vector4.Transform(screenPos4, inverseView);
        
        return new Vector2(worldPos4.X, worldPos4.Y);
    }

    /// <summary>
    /// Moves the camera by the specified offset.
    /// </summary>
    public void Move(Vector2 offset)
    {
        _position += offset;
    }

    /// <summary>
    /// Moves the camera smoothly towards a target position.
    /// </summary>
    public void LerpTo(Vector2 target, float smoothing)
    {
        _position = Vector2.Lerp(_position, target, smoothing);
    }

    /// <summary>
    /// Immediately centers the camera on a target position.
    /// </summary>
    public void CenterOn(Vector2 target)
    {
        _position = target;
    }

    public void Shake(float intensity, float duration)
    {
        // Blend with any ongoing shake; take whichever is more intense
        if (intensity > _shakeIntensity * (1f - _shakeElapsed / Math.Max(_shakeDuration, 0.001f)))
        {
            _shakeIntensity = intensity;
            _shakeDuration = duration;
            _shakeElapsed = 0f;
        }
    }

    public void UpdateShake(float deltaTime)
    {
        if (_shakeElapsed < _shakeDuration)
            _shakeElapsed = Math.Min(_shakeElapsed + deltaTime, _shakeDuration);
    }

    private Vector2 GetShakeOffset()
    {
        if (_shakeElapsed >= _shakeDuration || _shakeIntensity <= 0f) return Vector2.Zero;
        var decay = 1f - (_shakeElapsed / _shakeDuration);
        var magnitude = _shakeIntensity * decay;
        return new Vector2(
            (_shakeRng.NextSingle() * 2f - 1f) * magnitude,
            (_shakeRng.NextSingle() * 2f - 1f) * magnitude);
    }

    public void TrackRegistration(ICameraManager manager, string name)
    {
        _manager = manager;
        _registeredName = name;
    }

    public void Dispose()
    {
        if (_manager != null && _registeredName != null)
        {
            _manager.RemoveCamera(_registeredName);
            _manager = null;
            _registeredName = null;
        }
    }
}