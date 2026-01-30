using Brine2D.ECS.Systems;
using Brine2D.Rendering;

namespace Brine2D.Engine.Systems;

/// <summary>
/// Configures scene-specific systems.
/// Similar to ASP.NET's IApplicationBuilder for middleware.
/// </summary>
public interface ISystemConfigurator
{
    /// <summary>
    /// Adds an update system to this scene's pipeline.
    /// System is created using dependency injection.
    /// </summary>
    void AddUpdateSystem<T>() where T : class, IUpdateSystem;
    
    /// <summary>
    /// Adds a render system to this scene's pipeline.
    /// System is created using dependency injection.
    /// </summary>
    void AddRenderSystem<T>() where T : class, IRenderSystem;
    
    /// <summary>
    /// Disables a global system for this scene only.
    /// The system is not removed, just skipped during execution.
    /// </summary>
    void DisableSystem<T>() where T : class, IUpdateSystem;
    
    /// <summary>
    /// Disables a system by name for this scene only.
    /// </summary>
    void DisableSystem(string systemName);
}