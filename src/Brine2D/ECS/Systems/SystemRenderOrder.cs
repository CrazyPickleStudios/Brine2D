namespace Brine2D.ECS.Systems;

/// <summary>
/// Defines standard execution order values for render systems.
/// Lower values execute first (render behind).
/// </summary>
/// <remarks>
/// <para>
/// These constants provide a structured way to order render system execution
/// for proper layering. Systems can use these values directly or add offsets.
/// </para>
/// <example>
/// <code>
/// // Render in background layer
/// public int RenderOrder => SystemRenderOrder.Background;
/// 
/// // Render between sprites and UI
/// public int RenderOrder => SystemRenderOrder.Sprites + 100;
/// </code>
/// </example>
/// </remarks>
public static class SystemRenderOrder
{
    /// <summary>
    /// Background rendering (e.g., skybox, parallax backgrounds).
    /// Order: -100
    /// </summary>
    public const int Background = -100;
    
    /// <summary>
    /// Tilemap/terrain rendering.
    /// Order: -50
    /// </summary>
    public const int Tilemap = -50;
    
    /// <summary>
    /// Main sprite rendering (default for most render systems).
    /// Order: 0
    /// </summary>
    public const int Sprites = 0;
    
    /// <summary>
    /// Particle rendering (above sprites).
    /// Order: 100
    /// </summary>
    public const int Particles = 100;
    
    /// <summary>
    /// Lighting and effects.
    /// Order: 500
    /// </summary>
    public const int Lighting = 500;
    
    /// <summary>
    /// UI rendering (above game world).
    /// Order: 900
    /// </summary>
    public const int UI = 900;
    
    /// <summary>
    /// Debug rendering (always on top).
    /// Order: 1000
    /// </summary>
    public const int Debug = 1000;
}