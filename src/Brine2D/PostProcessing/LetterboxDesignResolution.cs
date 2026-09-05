namespace Brine2D.Rendering.SDL.PostProcessing;

/// <summary>
/// Marker/config record indicating that a <see cref="Brine2D.Rendering.SDL.PostProcessing.Effects.LetterboxEffect"/>
/// has been registered and the main/ping-pong post-processing render targets should be
/// created at this fixed design resolution instead of the live viewport size.
/// </summary>
/// <param name="Width">The fixed design width, in pixels.</param>
/// <param name="Height">The fixed design height, in pixels.</param>
public sealed record LetterboxDesignResolution(int Width, int Height);
