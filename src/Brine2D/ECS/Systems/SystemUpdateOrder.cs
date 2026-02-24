namespace Brine2D.ECS.Systems;

/// <summary>
/// Defines standard execution order values for update systems.
/// Lower values execute first.
/// </summary>
/// <remarks>
/// <para>
/// These constants provide a structured way to order system execution
/// without magic numbers. Systems can use these values directly or
/// add offsets for fine-grained control.
/// </para>
/// <example>
/// <code>
/// // Use standard order
/// public int UpdateOrder => SystemUpdateOrder.Physics;
/// 
/// // Custom offset (run right after physics)
/// public int UpdateOrder => SystemUpdateOrder.Physics + 10;
/// 
/// // Between two phases
/// public int UpdateOrder => (SystemUpdateOrder.Physics + SystemUpdateOrder.Collision) / 2;
/// </code>
/// </example>
/// </remarks>
public static class SystemUpdateOrder
{
    // ===== Input Phase (-200 to -100) =====
    
    /// <summary>
    /// Pre-input processing (e.g., input buffering, replay systems).
    /// Order: -200
    /// </summary>
    public const int PreInput = -200;
    
    /// <summary>
    /// Input processing (e.g., reading keyboard, mouse, gamepad).
    /// Order: -100
    /// </summary>
    public const int Input = -100;
    
    /// <summary>
    /// Post-input processing (e.g., input smoothing, dead zone handling).
    /// Order: -50
    /// </summary>
    public const int PostInput = -50;
    
    // ===== Main Update Phase (-49 to 99) =====
    
    /// <summary>
    /// Early update phase (runs before main update).
    /// Order: -25
    /// </summary>
    public const int EarlyUpdate = -25;
    
    /// <summary>
    /// Main update phase (default for most systems and behaviors).
    /// Order: 0
    /// </summary>
    public const int Update = 0;
    
    /// <summary>
    /// Post-update phase (runs after main update but before physics).
    /// Order: 50
    /// </summary>
    public const int PostUpdate = 50;
    
    // ===== Physics Phase (100 to 299) =====
    
    /// <summary>
    /// Pre-physics phase (e.g., applying forces, setting velocities).
    /// Order: 90
    /// </summary>
    public const int PrePhysics = 90;
    
    /// <summary>
    /// Physics simulation (e.g., velocity integration, position updates).
    /// Order: 100
    /// </summary>
    public const int Physics = 100;
    
    /// <summary>
    /// Post-physics phase (e.g., physics cleanup, constraint solving).
    /// Order: 150
    /// </summary>
    public const int PostPhysics = 150;
    
    // ===== Collision Phase (200 to 399) =====
    
    /// <summary>
    /// Collision detection and resolution.
    /// Order: 200
    /// </summary>
    public const int Collision = 200;
    
    /// <summary>
    /// Post-collision processing (e.g., trigger events, damage calculation).
    /// Order: 250
    /// </summary>
    public const int PostCollision = 250;
    
    // ===== Animation & Effects Phase (400 to 799) =====
    
    /// <summary>
    /// Animation systems (e.g., skeletal animation, sprite animation).
    /// Order: 400
    /// </summary>
    public const int Animation = 400;
    
    /// <summary>
    /// Particle systems and visual effects.
    /// Order: 500
    /// </summary>
    public const int Particles = 500;
    
    /// <summary>
    /// Audio systems (e.g., 3D audio positioning, music transitions).
    /// Order: 600
    /// </summary>
    public const int Audio = 600;
    
    // ===== Late Update Phase (800 to 999) =====
    
    /// <summary>
    /// Late update phase (e.g., camera follow, UI updates).
    /// Order: 800
    /// </summary>
    public const int LateUpdate = 800;
    
    /// <summary>
    /// Very late update (e.g., final position adjustments, look-at systems).
    /// Order: 900
    /// </summary>
    public const int VeryLateUpdate = 900;
    
    // ===== Pre-Render Phase (1000+) =====
    
    /// <summary>
    /// Pre-render preparation (e.g., frustum culling, visibility determination).
    /// Order: 1000
    /// </summary>
    public const int PreRender = 1000;
}