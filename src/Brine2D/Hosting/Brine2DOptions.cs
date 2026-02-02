using Brine2D.Audio;
using Brine2D.ECS;
using Brine2D.Input;
using Brine2D.Rendering;

namespace Brine2D.Hosting;

/// <summary>
/// Root configuration options for Brine2D game engine.
/// </summary>
/// <remarks>
/// This class follows the ASP.NET Core options pattern, allowing configuration
/// through code, JSON files (gamesettings.json), environment variables, or other
/// configuration providers.
/// </remarks>
public class Brine2DOptions
{
    /// <summary>
    /// Configuration section name for binding from JSON.
    /// </summary>
    public const string SectionName = "Brine2D";
    
    /// <summary>
    /// Gets or sets window configuration options.
    /// </summary>
    public WindowOptions Window { get; set; } = new();
    
    /// <summary>
    /// Gets or sets rendering configuration options.
    /// </summary>
    public RenderingOptions Rendering { get; set; } = new();
    
    /// <summary>
    /// Gets or sets audio configuration options.
    /// </summary>
    public AudioOptions Audio { get; set; } = new();
    
    /// <summary>
    /// Gets or sets input configuration options.
    /// </summary>
    public InputOptions Input { get; set; } = new();
    
    /// <summary>
    /// Gets or sets ECS configuration options.
    /// </summary>
    public ECSOptions ECS { get; set; } = new();
}