using System.Numerics;
using Brine2D.Core;
using Brine2D.Rendering.Text;

namespace Brine2D.Rendering;

/// <summary>
/// Core rendering interface for Brine2D.
/// </summary>
/// <remarks>
/// <para>
/// Extends <see cref="IDrawContext"/> with lifecycle management, render targets, and viewport state.
/// Consumers that only need drawing operations should depend on <see cref="IDrawContext"/> instead.
/// </para>
/// <para>
/// Implementations must be idempotent with respect to <see cref="IDisposable.Dispose"/>.
/// <see cref="GameEngine.ShutdownAsync"/> disposes the renderer explicitly before the DI
/// container tears down singletons; the container will then call <see cref="IDisposable.Dispose"/>
/// a second time during host disposal. Guard against double-disposal with a standard
/// <c>_disposed</c> flag checked at the top of your <c>Dispose</c> implementation.
/// </para>
/// </remarks>
public interface IRenderer : IDrawContext, IDisposable
{
    const byte DefaultRenderLayer = 128;
    const BlendMode DefaultBlendMode = BlendMode.Alpha;

    bool IsInitialized { get; }

    /// <summary>
    /// Gets or sets the active camera used for view-projection transforms.
    /// </summary>
    /// <remarks>
    /// Changes take effect at the start of the next render pass. Within a single frame,
    /// a new render pass begins when the render target changes.
    /// </remarks>
    ICamera? Camera { get; set; }

    Color ClearColor { get; set; }
    int Width { get; }
    int Height { get; }

    Task InitializeAsync(CancellationToken cancellationToken = default);
    void BeginFrame();
    void ApplyPostProcessing();
    void EndFrame();

    /// <summary>
    /// Create a render target for off-screen rendering.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Render targets are fixed-size and do not automatically resize when the window resizes.
    /// If you need a render target that matches the window size, recreate it in response to
    /// window resize events.
    /// </para>
    /// <para>
    /// The internal post-processing render targets automatically resize with the window.
    /// </para>
    /// </remarks>
    /// <param name="width">Width in pixels</param>
    /// <param name="height">Height in pixels</param>
    /// <returns>A new render target that you must dispose when done</returns>
    /// <exception cref="NotSupportedException">Thrown in headless mode where no GPU is available</exception>
    /// <example>
    /// <code>
    /// // Create minimap render target
    /// using var minimap = renderer.CreateRenderTarget(256, 256);
    /// 
    /// // Render scene to minimap
    /// renderer.PushRenderTarget(minimap);
    /// RenderMinimapView();
    /// renderer.PopRenderTarget();
    /// 
    /// // Draw minimap texture to screen
    /// renderer.DrawTexture(minimap.Texture, 10, 10);
    /// </code>
    /// </example>
    IRenderTarget CreateRenderTarget(int width, int height);

    /// <summary>
    /// Set the active render target (null = render to screen).
    /// </summary>
    /// <param name="target">Render target to draw to, or null for screen</param>
    void SetRenderTarget(IRenderTarget? target);

    /// <summary>
    /// Get the currently active render target (null = rendering to screen).
    /// </summary>
    /// <returns>Current render target or null if rendering to screen</returns>
    IRenderTarget? GetRenderTarget();

    /// <summary>
    /// Push current render target onto stack and set a new one.
    /// Useful for nested render-to-texture operations.
    /// </summary>
    /// <param name="target">New render target to set, or null for screen</param>
    void PushRenderTarget(IRenderTarget? target);

    /// <summary>
    /// Restore the previous render target from the stack.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if stack is empty</exception>
    void PopRenderTarget();

    /// <summary>
    /// Set a scissor rectangle to clip all rendering to a specific region.
    /// All draw calls will be clipped to this rectangle until disabled with null.
    /// </summary>
    /// <param name="rect">Rectangle to clip to, or null to disable clipping</param>
    /// <remarks>
    /// <para>
    /// Scissor rects are useful for:
    /// - UI scroll views and panels
    /// - Text clipping in bounded areas
    /// - Split-screen rendering
    /// - Preventing rendering outside specific regions
    /// </para>
    /// <para>
    /// The scissor rect is in screen coordinates (not world coordinates).
    /// It is not affected by camera transforms.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Clip to a 200x200 region
    /// renderer.SetScissorRect(new Rectangle(10, 10, 200, 200));
    /// renderer.DrawTexture(largeTexture, 0, 0); // Only visible part inside rect
    /// 
    /// // Disable clipping
    /// renderer.SetScissorRect(null);
    /// renderer.DrawTexture(largeTexture, 0, 0); // Fully visible
    /// 
    /// // Nested clipping for scroll view
    /// renderer.SetScissorRect(scrollViewBounds);
    /// foreach (var item in scrollItems)
    /// {
    ///     renderer.DrawText(item.Text, item.X, item.Y, Color.White);
    /// }
    /// renderer.SetScissorRect(null);
    /// </code>
    /// </example>
    void SetScissorRect(Rectangle? rect);

    /// <summary>
    /// Get the current scissor rectangle (null if clipping is disabled).
    /// </summary>
    /// <returns>Current scissor rectangle or null</returns>
    Rectangle? GetScissorRect();

    /// <summary>
    /// Push the current scissor rect onto a stack and set a new one.
    /// Useful for nested clipping regions (e.g., nested scroll views).
    /// </summary>
    /// <param name="rect">New scissor rectangle, or null to disable clipping</param>
    /// <remarks>
    /// This is particularly useful when rendering UI hierarchies where child
    /// elements need to be clipped to parent bounds.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Outer panel
    /// renderer.PushScissorRect(outerPanelBounds);
    /// RenderOuterPanel();
    /// 
    ///     // Inner scroll view (clipped to both outer and inner)
    ///     renderer.PushScissorRect(innerScrollViewBounds);
    ///     RenderScrollContent();
    ///     renderer.PopScissorRect();
    /// 
    /// renderer.PopScissorRect();
    /// </code>
    /// </example>
    void PushScissorRect(Rectangle? rect);

    /// <summary>
    /// Restore the previous scissor rectangle from the stack.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if stack is empty</exception>
    void PopScissorRect();
}