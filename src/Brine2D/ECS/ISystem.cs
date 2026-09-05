namespace Brine2D.ECS;

/// <summary>
///     Interface for systems that process entities in the ECS.
///     Systems run before behaviors during EntityWorld.Update().
/// </summary>
/// <remarks>
///     Systems are scene-scoped and automatically cleaned up when the scene unloads.
///     Use systems for batch processing of many entities (physics, collision, etc.).
///     For entity-specific logic, use Behavior instead.
///     <para>
///     <b>Ambient vs. parameter-passed:</b> unlike <see cref="Behavior"/> (which exposes
///     its framework services as ambient properties since one instance is bound to one
///     entity for its entire attached lifetime), a system instance is shared across many
///     entities and batch-processes all of them on each call. There is no single "current
///     entity" to be ambient about, so systems instead take <see cref="IEntityWorld"/> (and
///     the entities/components within it) as explicit parameters to <c>Update</c>,
///     <c>FixedUpdate</c>, or <c>Render</c>. See <see cref="Behavior"/>'s remarks for the
///     full rule.
///     </para>
/// </remarks>
public interface ISystem
{
    /// <summary>
    ///     Whether this system is currently enabled.
    ///     Disabled systems are skipped during Update().
    /// </summary>
    bool IsEnabled { get; set; }
}