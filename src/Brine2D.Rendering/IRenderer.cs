using System;
using System.Collections.Generic;
using System.Text;

namespace Brine2D.Rendering;

/// <summary>
/// Interface for rendering graphics to the screen.
/// </summary>
public interface IRenderer : IDisposable
{
    /// <summary>
    /// Gets a value indicating whether the renderer is initialized.
    /// </summary>
    bool IsInitialized { get; }

    /// <summary>
    /// Initializes the renderer.
    /// </summary>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears the screen with the specified color.
    /// </summary>
    void Clear(Color color);

    /// <summary>
    /// Begins a new frame. Call this before drawing.
    /// </summary>
    void BeginFrame();

    /// <summary>
    /// Ends the current frame and presents it to the screen.
    /// </summary>
    void EndFrame();

    /// <summary>
    /// Draws a filled rectangle.
    /// </summary>
    void DrawRectangle(float x, float y, float width, float height, Color color);

    /// <summary>
    /// Draws a texture at the specified position.
    /// </summary>
    /// <param name="texture">The texture to draw.</param>
    /// <param name="x">X position.</param>
    /// <param name="y">Y position.</param>
    void DrawTexture(ITexture texture, float x, float y);

    /// <summary>
    /// Draws a texture with scaling.
    /// </summary>
    /// <param name="texture">The texture to draw.</param>
    /// <param name="x">X position.</param>
    /// <param name="y">Y position.</param>
    /// <param name="width">Destination width.</param>
    /// <param name="height">Destination height.</param>
    void DrawTexture(ITexture texture, float x, float y, float width, float height);

    /// <summary>
    /// Draws a portion of a texture (for sprite sheets).
    /// </summary>
    /// <param name="texture">The texture to draw.</param>
    /// <param name="sourceX">Source X position in the texture.</param>
    /// <param name="sourceY">Source Y position in the texture.</param>
    /// <param name="sourceWidth">Source width.</param>
    /// <param name="sourceHeight">Source height.</param>
    /// <param name="destX">Destination X position.</param>
    /// <param name="destY">Destination Y position.</param>
    /// <param name="destWidth">Destination width.</param>
    /// <param name="destHeight">Destination height.</param>
    void DrawTexture(ITexture texture, 
        float sourceX, float sourceY, float sourceWidth, float sourceHeight,
        float destX, float destY, float destWidth, float destHeight);

    /// <summary>
    /// Draws text at the specified position.
    /// </summary>
    void DrawText(string text, float x, float y, Color color);
}
