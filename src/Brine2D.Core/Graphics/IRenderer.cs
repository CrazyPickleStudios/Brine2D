using Brine2D.Core.Math;

namespace Brine2D.Core.Graphics;

/// <summary>
///     Defines high-level rendering operations for a graphics backend, such as clearing the current render target and
///     presenting the rendered frame to the display.
/// </summary>
/// <remarks>
///     Implementations are expected to manage any underlying command submission and swap-chain or back-buffer presentation
///     details as appropriate for the platform.
/// </remarks>
public interface IRenderer
{
    /// <summary>
    ///     Clears the current render target to the specified color.
    /// </summary>
    /// <param name="color">The color to use when clearing, e.g., <see cref="Color.Black" />.</param>
    void Clear(Color color);

    /// <summary>
    ///     Presents the contents of the back buffer to the display.
    /// </summary>
    /// <remarks>
    ///     <para>Typically flushes pending draw commands and swaps the back buffer to the screen.</para>
    ///     <para>May block depending on synchronization (e.g., VSync) settings of the implementation.</para>
    /// </remarks>
    void Present();
}