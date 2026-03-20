using System.Numerics;

namespace Brine2D.ECS.Components;

/// <summary>
/// Position, rotation, and scale for an entity.
/// World-space accessors (<see cref="Position"/>, <see cref="Rotation"/>, <see cref="Scale"/>)
/// walk the parent hierarchy automatically; local-space accessors bypass it.
/// </summary>
public class TransformComponent : Component
{
    public Vector2 LocalPosition { get; set; }
    public float LocalRotation { get; set; }
    public Vector2 LocalScale { get; set; } = Vector2.One;

    /// <summary>
    /// World-space position. Setting this back-computes <see cref="LocalPosition"/>
    /// through the parent's transform (rotation + scale).
    /// </summary>
    public Vector2 Position
    {
        get
        {
            var parent = GetParentTransform();
            if (parent == null)
                return LocalPosition;

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
            var unscaled = offset / parent.Scale;
            LocalPosition = Vector2.Transform(unscaled, Matrix3x2.CreateRotation(-parent.Rotation));
        }
    }

    /// <summary>
    /// World-space rotation in radians. Additive through the parent hierarchy.
    /// </summary>
    public float Rotation
    {
        get
        {
            var parent = GetParentTransform();
            return parent == null ? LocalRotation : parent.Rotation + LocalRotation;
        }
        set
        {
            var parent = GetParentTransform();
            LocalRotation = parent == null ? value : value - parent.Rotation;
        }
    }

    /// <summary>
    /// World-space scale. Multiplicative through the parent hierarchy.
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
            LocalScale = parent == null ? value : value / parent.Scale;
        }
    }

    public void Translate(Vector2 delta) => LocalPosition += delta;

    public void Rotate(float angleRadians) => LocalRotation += angleRadians;

    /// <summary>
    /// Computes the world-space SRT matrix. Walks the parent hierarchy via
    /// <see cref="Position"/>/<see cref="Rotation"/>/<see cref="Scale"/>.
    /// </summary>
    public Matrix3x2 GetTransformMatrix()
    {
        return Matrix3x2.CreateScale(Scale) *
               Matrix3x2.CreateRotation(Rotation) *
               Matrix3x2.CreateTranslation(Position);
    }

    private TransformComponent? GetParentTransform()
        => Entity?.Parent?.GetComponent<TransformComponent>();
}