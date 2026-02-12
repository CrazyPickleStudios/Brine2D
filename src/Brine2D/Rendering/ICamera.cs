using System.Numerics;
using Brine2D.Core;

namespace Brine2D.Rendering;

/// <summary>
/// Represents a 2D camera for viewing the game world.
/// </summary>
public interface ICamera
{
    /// <summary>
    /// Gets or sets the camera position in world space.
    /// </summary>
    Vector2 Position { get; set; }

    /// <summary>
    /// Gets or sets the camera zoom level (1.0 = normal, 2.0 = 2x zoom in).
    /// </summary>
    float Zoom { get; set; }

    /// <summary>
    /// Gets or sets the camera rotation in degrees.
    /// </summary>
    float Rotation { get; set; }

    /// <summary>
    /// Gets the viewport width in pixels.
    /// </summary>
    int ViewportWidth { get; }

    /// <summary>
    /// Gets the viewport height in pixels.
    /// </summary>
    int ViewportHeight { get; }

    /// <summary>
    /// Converts a world position to screen position.
    /// </summary>
    Vector2 WorldToScreen(Vector2 worldPosition);

    /// <summary>
    /// Converts a screen position to world position.
    /// </summary>
    Vector2 ScreenToWorld(Vector2 screenPosition);

    /// <summary>
    /// Gets the view matrix for transforming world coordinates to camera space.
    /// </summary>
    Matrix4x4 GetViewMatrix();

    /// <summary>
    /// Sets the viewport size (typically called when window resizes).
    /// </summary>
    void SetViewport(int width, int height);
    
    /// <summary>
    /// Gets the visible world bounds (frustum rectangle).
    /// </summary>
    /// <returns>Rectangle representing the visible area in world space.</returns>
    /// <example>
    /// <code>
    /// var visibleArea = camera.GetVisibleBounds();
    /// // Cull entities outside this area
    /// if (!visibleArea.Contains(entity.Position)) return;
    /// </code>
    /// </example>
    Rectangle GetVisibleBounds();
    
    /// <summary>
    /// Checks if a world position is visible by the camera.
    /// </summary>
    /// <param name="worldPosition">Position in world space.</param>
    /// <returns>True if the position is within the camera's view frustum.</returns>
    bool IsVisible(Vector2 worldPosition);
    
    /// <summary>
    /// Checks if a rectangle is visible by the camera (frustum culling).
    /// </summary>
    /// <param name="worldBounds">Rectangle in world space.</param>
    /// <returns>True if the rectangle intersects with the camera's view frustum.</returns>
    /// <example>
    /// <code>
    /// var spriteBounds = new Rectangle(sprite.X, sprite.Y, sprite.Width, sprite.Height);
    /// if (camera.IsVisible(spriteBounds))
    /// {
    ///     renderer.DrawTexture(sprite.Texture, ...);
    /// }
    /// </code>
    /// </example>
    bool IsVisible(Rectangle worldBounds);
    
    /// <summary>
    /// Clamps the camera position to stay within world bounds.
    /// </summary>
    /// <param name="worldBounds">The world boundaries to clamp to.</param>
    /// <example>
    /// <code>
    /// // Keep camera within level bounds
    /// camera.ClampToBounds(new Rectangle(0, 0, levelWidth, levelHeight));
    /// </code>
    /// </example>
    void ClampToBounds(Rectangle worldBounds);
    
    /// <summary>
    /// Smoothly moves the camera towards a target position.
    /// </summary>
    /// <param name="targetPosition">Target world position.</param>
    /// <param name="smoothing">Smoothing factor (0-1, higher = slower). 0 = instant, 0.9 = very smooth.</param>
    /// <param name="deltaTime">Time since last frame in seconds.</param>
    /// <example>
    /// <code>
    /// // Smooth camera follow
    /// protected override void OnUpdate(GameTime gameTime)
    /// {
    ///     camera.FollowSmooth(player.Position, smoothing: 0.1f, (float)gameTime.DeltaTime);
    /// }
    /// </code>
    /// </example>
    void FollowSmooth(Vector2 targetPosition, float smoothing, float deltaTime);
}