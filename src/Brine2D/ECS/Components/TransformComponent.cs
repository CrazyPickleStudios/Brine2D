using System.Numerics;

namespace Brine2D.ECS.Components;

/// <summary>
///     Position, rotation, and scale for an entity.
///     World-space accessors (<see cref="Position" />, <see cref="Rotation" />, <see cref="Scale" />)
///     walk the parent hierarchy automatically; local-space accessors bypass it.
/// </summary>
public class TransformComponent : Component
{
    // Guards Position and Scale setters against division-by-zero when a parent's scale is zero.
    private const float ScaleEpsilon = 1e-6f;
    public Vector2 LocalPosition { get; set; }

    public float LocalRotation { get; set; }

    public Vector2 LocalScale { get; set; } = Vector2.One;

    /// <summary>
    ///     World-space position. Setting this back-computes <see cref="LocalPosition" />
    ///     through the parent's transform (rotation + scale).
    /// </summary>
    public Vector2 Position
    {
        get
        {
            var parent = GetParentTransform();

            if (parent == null)
            {
                return LocalPosition;
            }

            var rotated = Vector2.Transform(LocalPosition, Matrix3x2.CreateRotation(parent.Rotation));

            return parent.Position + rotated * parent.Scale;
        }
        set
        {
            var parent = GetParentTransform();

            if (parent == null)
            {
                LocalPosition = value;
                return;
            }

            var offset = value - parent.Position;
            var parentScale = parent.Scale;
            var safeScale = new Vector2(
                MathF.Abs(parentScale.X) < ScaleEpsilon ? ScaleEpsilon : parentScale.X,
                MathF.Abs(parentScale.Y) < ScaleEpsilon ? ScaleEpsilon : parentScale.Y);
            var unscaled = offset / safeScale;
            LocalPosition = Vector2.Transform(unscaled, Matrix3x2.CreateRotation(-parent.Rotation));
        }
    }

    /// <summary>
    ///     World-space rotation in <b>radians</b>. Additive through the parent hierarchy.
    /// </summary>
    /// <remarks>
    ///     Transform rotation uses radians while <see cref="Brine2D.Rendering.ICamera.Rotation"/>
    ///     (and <see cref="Brine2D.Rendering.Camera2D.Rotation"/>) uses degrees.
    ///     Convert with <c>rotation * (180f / MathF.PI)</c> when assigning to a camera.
    /// </remarks>
    public float Rotation
    {
        get
        {
            var parent = GetParentTransform();

            return parent?.Rotation + LocalRotation ?? LocalRotation;
        }
        set
        {
            var parent = GetParentTransform();
            LocalRotation = value - parent?.Rotation ?? value;
        }
    }

    /// <summary>
    ///     World-space scale. Multiplicative through the parent hierarchy.
    /// </summary>
    public Vector2 Scale
    {
        get
        {
            var parent = GetParentTransform();

            return parent == null ? LocalScale : parent.Scale * LocalScale;
        }
        set
        {
            var parent = GetParentTransform();
            if (parent == null)
            {
                LocalScale = value;
                return;
            }
            var parentScale = parent.Scale;
            var safeScale = new Vector2(
                MathF.Abs(parentScale.X) < ScaleEpsilon ? ScaleEpsilon : parentScale.X,
                MathF.Abs(parentScale.Y) < ScaleEpsilon ? ScaleEpsilon : parentScale.Y);
            LocalScale = value / safeScale;
        }
    }

    /// <summary>
    ///     Computes the world-space SRT matrix. Walks the parent hierarchy via
    ///     <see cref="Position" />/<see cref="Rotation" />/<see cref="Scale" />.
    /// </summary>
    public Matrix3x2 GetTransformMatrix()
    {
        return Matrix3x2.CreateScale(Scale) *
               Matrix3x2.CreateRotation(Rotation) *
               Matrix3x2.CreateTranslation(Position);
    }

    public void Rotate(float angleRadians)
    {
        LocalRotation += angleRadians;
    }

    public void Translate(Vector2 delta)
    {
        LocalPosition += delta;
    }

    private TransformComponent? GetParentTransform()
    {
        return Entity?.Parent?.GetComponent<TransformComponent>();
    }
}