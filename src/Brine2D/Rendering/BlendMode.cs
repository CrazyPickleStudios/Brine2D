namespace Brine2D.Rendering;

/// <summary>
/// Blend modes for rendering.
/// Controls how pixels are combined when drawing on top of existing pixels.
/// </summary>
public enum BlendMode
{
    /// <summary>
    /// Standard alpha blending (default).
    /// Result = Source * SourceAlpha + Dest * (1 - SourceAlpha)
    /// Best for: Normal sprites, UI elements
    /// </summary>
    Alpha,
    
    /// <summary>
    /// Additive blending (for fire, explosions, lights).
    /// Result = Source + Dest
    /// Creates a glowing effect where overlapping particles brighten.
    /// Best for: Fire, explosions, energy effects, lights
    /// </summary>
    Additive,
    
    /// <summary>
    /// Multiplicative blending (for shadows, darkening).
    /// Result = Source * Dest
    /// Best for: Shadows, fog, darkening effects
    /// </summary>
    Multiply,
    
    /// <summary>
    /// No blending (opaque).
    /// Result = Source
    /// Best for: Solid, opaque objects
    /// </summary>
    None
}