using Brine2D.Core.Math;
using Brine2D.Core.Scene;

namespace Brine2D.Core.Components;

/// <summary>
///     2D transform component describing spatial state of an <see cref="Entity" />: position, rotation, and scale.
/// </summary>
/// <remarks>
///     <para>Values are in world space. Rotation is expressed in radians.</para>
/// </remarks>
public sealed class Transform2D : Component
{
    /// <summary>
    ///     Position in 2D space.
    /// </summary>
    public Vector2 Position { get; set; }

    /// <summary>
    ///     Rotation angle in radians.
    /// </summary>
    /// <remarks>
    ///     Positive rotates counter-clockwise.
    /// </remarks>
    public float Rotation { get; set; }

    /// <summary>
    ///     Non-uniform scale on the X (horizontal) and Y (vertical) axes.
    /// </summary>
    /// <remarks>
    ///     Defaults to <see cref="Vector2.One" />.
    /// </remarks>
    public Vector2 Scale { get; set; } = new(1, 1);
}