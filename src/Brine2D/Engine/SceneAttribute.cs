namespace Brine2D.Engine;

/// <summary>
/// Attribute for configuring scene metadata and behavior.
/// Follows ASP.NET's attribute-based configuration pattern.
/// </summary>
/// <example>
/// <code>
/// [Scene(Name = "Main Menu", EnableLifecycleHooks = true)]
/// public class MenuScene : Scene
/// {
///     // Name is "Main Menu" instead of "MenuScene"
/// }
/// 
/// [Scene(EnableAutomaticFrameManagement = false)]
/// public class CustomRenderScene : Scene
/// {
///     // Manual control over BeginFrame/EndFrame
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class SceneAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the scene name. If null, uses the class name.
    /// </summary>
    public string? Name { get; set; }
    
    /// <summary>
    /// Gets or sets whether lifecycle hooks execute automatically.
    /// Default is true.
    /// </summary>
    public bool EnableLifecycleHooks { get; set; } = true;
    
    /// <summary>
    /// Gets or sets whether frame management happens automatically.
    /// Default is true.
    /// </summary>
    public bool EnableAutomaticFrameManagement { get; set; } = true;
}