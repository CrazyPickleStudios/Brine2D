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
    /// Gets or sets the clear color used when clearing the screen.
    /// Scenes can set this in OnInitialize() to customize their background.
    /// </summary>
    Color ClearColor { get; set; }

    /// <summary>
    /// Gets or sets the active camera for rendering.
    /// If null, renders in screen space.
    /// </summary>
    ICamera? Camera { get; set; }

    /// <summary>
    /// Initializes the renderer.
    /// </summary>
    Task InitializeAsync(CancellationToken cancellationToken = default);
    
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
    void DrawRectangleFilled(float x, float y, float width, float height, Color color);

    /// <summary>
    /// Draws a rectangle outline (no fill).
    /// </summary>
    void DrawRectangleOutline(float x, float y, float width, float height, Color color, float thickness = 1f);

    /// <summary>
    /// Draws a filled circle.
    /// </summary>
    void DrawCircleFilled(float centerX, float centerY, float radius, Color color);

    /// <summary>
    /// Draws a circle outline (no fill).
    /// </summary>
    /// <param name="centerX">Center X position.</param>
    /// <param name="centerY">Center Y position.</param>
    /// <param name="radius">Circle radius.</param>
    /// <param name="color">Outline color.</param>
    /// <param name="thickness">Line thickness (default: 1.0f).</param>
    void DrawCircleOutline(float centerX, float centerY, float radius, Color color, float thickness = 1f);

    /// <summary>
    /// Draws a line between two points.
    /// </summary>
    /// <param name="x1">Start X position.</param>
    /// <param name="y1">Start Y position.</param>
    /// <param name="x2">End X position.</param>
    /// <param name="y2">End Y position.</param>
    /// <param name="color">Line color.</param>
    /// <param name="thickness">Line thickness (default: 1.0f).</param>
    void DrawLine(float x1, float y1, float x2, float y2, Color color, float thickness = 1f);

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

    /// <summary>
    /// Sets the default font used for text rendering.
    /// </summary>
    void SetDefaultFont(IFont? font);
}
