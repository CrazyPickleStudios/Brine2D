using System.Numerics;
using Brine2D.Core;

namespace Brine2D.Rendering;

/// <summary>
/// A 2D camera for viewing and navigating the game world.
/// </summary>
public sealed class Camera2D : ICamera, IShakableCamera, ITrackableCamera, IDisposable
{
    private const float MinZoom = 0.01f;
    private const float MaxZoom = 100f;

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
    private Vector2 _cachedShakeOffset;
    private readonly Random _shakeRng;
    private ShakeSpace _shakeSpace = ShakeSpace.World;

    private bool _viewMatrixDirty = true;
    private Matrix4x4 _cachedViewMatrix;

    private bool _visibleBoundsDirty = true;
    private Rectangle _cachedVisibleBounds;

    public Vector2 Position
    {
        get => _position;
        set
        {
            _position = value;
            _viewMatrixDirty = true;
            _visibleBoundsDirty = true;
        }
    }

    public float Zoom
    {
        get => _zoom;
        set
        {
            _zoom = Math.Clamp(value, MinZoom, MaxZoom);
            _viewMatrixDirty = true;
            _visibleBoundsDirty = true;
        }
    }

    public float Rotation
    {
        get => _rotation;
        set
        {
            _rotation = value;
            _viewMatrixDirty = true;
            _visibleBoundsDirty = true;
        }
    }

    /// <summary>
    /// Determines how shake offset is applied. <see cref="Rendering.ShakeSpace.World"/> (default)
    /// applies the offset before zoom/rotation, so shake scales with zoom.
    /// <see cref="Rendering.ShakeSpace.Screen"/> applies the offset after zoom/rotation,
    /// so shake feels consistent regardless of zoom level.
    /// </summary>
    public ShakeSpace ShakeSpace
    {
        get => _shakeSpace;
        set
        {
            if (_shakeSpace == value) return;
            _shakeSpace = value;
            if (_cachedShakeOffset != Vector2.Zero)
                _viewMatrixDirty = true;
        }
    }

    public int ViewportWidth => _viewportWidth;
    public int ViewportHeight => _viewportHeight;

    /// <summary>
    /// Gets the center of the viewport in screen coordinates.
    /// </summary>
    public Vector2 ViewportCenter => new Vector2(_viewportWidth / 2f, _viewportHeight / 2f);

    public Camera2D(int viewportWidth, int viewportHeight)
        : this(viewportWidth, viewportHeight, shakeSeed: null)
    {
    }

    /// <summary>
    /// Creates a new camera with an optional deterministic seed for shake randomness.
    /// </summary>
    public Camera2D(int viewportWidth, int viewportHeight, int? shakeSeed)
    {
        _viewportWidth = Math.Max(viewportWidth, 0);
        _viewportHeight = Math.Max(viewportHeight, 0);
        _shakeRng = shakeSeed.HasValue ? new Random(shakeSeed.Value) : new Random();
    }

    public void SetViewport(int width, int height)
    {
        _viewportWidth = Math.Max(width, 0);
        _viewportHeight = Math.Max(height, 0);
        _viewMatrixDirty = true;
        _visibleBoundsDirty = true;
    }

    public Matrix4x4 GetViewMatrix()
    {
        if (!_viewMatrixDirty)
            return _cachedViewMatrix;

        var worldShake = _shakeSpace == ShakeSpace.World ? _cachedShakeOffset : Vector2.Zero;
        var origin = new Vector3(-_position.X + worldShake.X, -_position.Y + worldShake.Y, 0);
        var center = new Vector3(ViewportCenter.X, ViewportCenter.Y, 0);

        _cachedViewMatrix =
            Matrix4x4.CreateTranslation(origin) *
            Matrix4x4.CreateRotationZ(MathF.PI / 180f * _rotation) *
            Matrix4x4.CreateScale(_zoom, _zoom, 1) *
            Matrix4x4.CreateTranslation(center);

        if (_shakeSpace == ShakeSpace.Screen && _cachedShakeOffset != Vector2.Zero)
            _cachedViewMatrix *= Matrix4x4.CreateTranslation(_cachedShakeOffset.X, _cachedShakeOffset.Y, 0);

        _viewMatrixDirty = false;
        return _cachedViewMatrix;
    }

    public Vector2 WorldToScreen(Vector2 worldPosition)
    {
        var viewMatrix = GetViewMatrix();
        var worldPos4 = new Vector4(worldPosition.X, worldPosition.Y, 0, 1);
        var screenPos4 = Vector4.Transform(worldPos4, viewMatrix);

        return new Vector2(screenPos4.X, screenPos4.Y);
    }

    /// <summary>
    /// Gets the visible world bounds. Intentionally excludes shake offset so that
    /// culling remains stable during camera shake.
    /// </summary>
    public Rectangle GetVisibleBounds()
    {
        if (!_visibleBoundsDirty)
            return _cachedVisibleBounds;

        var halfWidth = ViewportWidth / (2.0f * Zoom);
        var halfHeight = ViewportHeight / (2.0f * Zoom);

        if (_rotation != 0f)
        {
            var rad = MathF.PI / 180f * _rotation;
            var sin = MathF.Abs(MathF.Sin(rad));
            var cos = MathF.Abs(MathF.Cos(rad));
            var expandedHalfWidth = halfWidth * cos + halfHeight * sin;
            var expandedHalfHeight = halfWidth * sin + halfHeight * cos;
            halfWidth = expandedHalfWidth;
            halfHeight = expandedHalfHeight;
        }

        _cachedVisibleBounds = new Rectangle(
            Position.X - halfWidth,
            Position.Y - halfHeight,
            halfWidth * 2,
            halfHeight * 2
        );

        _visibleBoundsDirty = false;
        return _cachedVisibleBounds;
    }

    public bool IsVisible(Vector2 worldPosition)
    {
        var bounds = GetVisibleBounds();
        return bounds.Contains(worldPosition.X, worldPosition.Y);
    }

    public bool IsVisible(Rectangle worldBounds)
    {
        var cameraBounds = GetVisibleBounds();
        return cameraBounds.Intersects(worldBounds);
    }

    public void ClampToBounds(Rectangle worldBounds)
    {
        var visibleBounds = GetVisibleBounds();

        var halfWidth = visibleBounds.Width / 2f;
        var halfHeight = visibleBounds.Height / 2f;

        float clampedX;
        float clampedY;

        if (visibleBounds.Width >= worldBounds.Width)
            clampedX = worldBounds.X + worldBounds.Width / 2f;
        else
            clampedX = Math.Clamp(Position.X, worldBounds.X + halfWidth, worldBounds.X + worldBounds.Width - halfWidth);

        if (visibleBounds.Height >= worldBounds.Height)
            clampedY = worldBounds.Y + worldBounds.Height / 2f;
        else
            clampedY = Math.Clamp(Position.Y, worldBounds.Y + halfHeight, worldBounds.Y + worldBounds.Height - halfHeight);

        Position = new Vector2(clampedX, clampedY);
    }

    public Vector2 ScreenToWorld(Vector2 screenPosition)
    {
        if (!Matrix4x4.Invert(GetViewMatrix(), out var inverseView))
            throw new InvalidOperationException("Camera view matrix is non-invertible; zoom may be near zero.");

        var screenPos4 = new Vector4(screenPosition.X, screenPosition.Y, 0, 1);
        var worldPos4 = Vector4.Transform(screenPos4, inverseView);

        return new Vector2(worldPos4.X, worldPos4.Y);
    }

    /// <summary>
    /// Moves the camera by the specified offset.
    /// Bypasses the <see cref="Position"/> setter for performance; dirty flags are set manually.
    /// </summary>
    public void Move(Vector2 offset)
    {
        _position += offset;
        _viewMatrixDirty = true;
        _visibleBoundsDirty = true;
    }

    /// <summary>
    /// Immediately centers the camera on a target position.
    /// Bypasses the <see cref="Position"/> setter for performance; dirty flags are set manually.
    /// </summary>
    public void CenterOn(Vector2 target)
    {
        _position = target;
        _viewMatrixDirty = true;
        _visibleBoundsDirty = true;
    }

    /// <summary>
    /// Cancels any ongoing camera shake.
    /// </summary>
    public void CancelShake()
    {
        _shakeIntensity = 0f;
        _shakeDuration = 0f;
        _shakeElapsed = 0f;

        if (_cachedShakeOffset != Vector2.Zero)
        {
            _cachedShakeOffset = Vector2.Zero;
            _viewMatrixDirty = true;
        }
    }

    public void Shake(float intensity, float duration)
    {
        var remainingIntensity = _shakeDuration > 0f
            ? _shakeIntensity * (1f - _shakeElapsed / _shakeDuration)
            : 0f;

        if (intensity > remainingIntensity)
        {
            _shakeIntensity = intensity;
            _shakeDuration = duration;
            _shakeElapsed = 0f;
        }
    }

    public void UpdateShake(float deltaTime)
    {
        if (_shakeIntensity <= 0f && _cachedShakeOffset == Vector2.Zero)
            return;

        if (_shakeElapsed < _shakeDuration)
            _shakeElapsed = Math.Min(_shakeElapsed + deltaTime, _shakeDuration);

        var newOffset = ComputeShakeOffset();

        if (newOffset != _cachedShakeOffset)
        {
            _cachedShakeOffset = newOffset;
            _viewMatrixDirty = true;
        }

        if (_shakeElapsed >= _shakeDuration && _cachedShakeOffset == Vector2.Zero)
        {
            _shakeIntensity = 0f;
            _shakeDuration = 0f;
            _shakeElapsed = 0f;
        }
    }

    private Vector2 ComputeShakeOffset()
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
            if (_manager.GetCamera(_registeredName) == this)
                _manager.RemoveCamera(_registeredName);

            _manager = null;
            _registeredName = null;
        }
    }
}