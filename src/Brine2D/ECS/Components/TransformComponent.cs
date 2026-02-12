using System.Numerics;

namespace Brine2D.ECS.Components;

/// <summary>
/// Component for entity position, rotation, and scale.
/// Supports hierarchical transforms (parent/child relationships).
/// </summary>
public class TransformComponent : Component
{
    private Vector2 _localPosition = Vector2.Zero;
    private float _localRotation = 0f;
    private Vector2 _localScale = Vector2.One;

    /// <summary>
    /// Gets or sets the local position relative to the parent (or world if no parent).
    /// </summary>
    public Vector2 LocalPosition
    {
        get => _localPosition;
        set => _localPosition = value;
    }
    
    /// <summary>
    /// Gets or sets the local rotation in radians.
    /// </summary>
    public float LocalRotation
    {
        get => _localRotation;
        set => _localRotation = value;
    }
    
    /// <summary>
    /// Gets or sets the local scale.
    /// </summary>
    public Vector2 LocalScale
    {
        get => _localScale;
        set => _localScale = value;
    }

    /// <summary>
    /// Gets or sets the world position (computed from parent hierarchy).
    /// Setting world position updates local position appropriately.
    /// </summary>
    public Vector2 Position
    {
        get
        {
            if (Entity?.Parent == null)
                return _localPosition; // Root entity
            
            var parentTransform = Entity.Parent.GetComponent<TransformComponent>();
            if (parentTransform == null)
                return _localPosition; // Parent has no transform
            
            // Apply parent's full transform to local position
            var parentPos = parentTransform.Position;
            var parentRot = parentTransform.Rotation;
            var parentScale = parentTransform.Scale;
            
            // Rotate and scale the local position offset
            var rotatedLocal = Vector2.Transform(_localPosition, Matrix3x2.CreateRotation(parentRot));
            var scaledLocal = rotatedLocal * parentScale;
            
            return parentPos + scaledLocal;
        }
        set
        {
            if (Entity?.Parent == null)
            {
                _localPosition = value; // Root entity
            }
            else
            {
                var parentTransform = Entity.Parent.GetComponent<TransformComponent>();
                if (parentTransform == null)
                {
                    _localPosition = value;
                }
                else
                {
                    // Convert world position to local space
                    var parentPos = parentTransform.Position;
                    var parentRot = parentTransform.Rotation;
                    var parentScale = parentTransform.Scale;
                    
                    var offset = value - parentPos;
                    
                    // Inverse transform: unscale, then unrotate
                    var unscaledOffset = offset / parentScale;
                    var unrotatedOffset = Vector2.Transform(unscaledOffset, Matrix3x2.CreateRotation(-parentRot));
                    
                    _localPosition = unrotatedOffset;
                }
            }
        }
    }
    
    /// <summary>
    /// Gets or sets the world rotation (computed from parent hierarchy).
    /// </summary>
    public float Rotation
    {
        get
        {
            if (Entity?.Parent == null)
                return _localRotation;
            
            var parentTransform = Entity.Parent.GetComponent<TransformComponent>();
            if (parentTransform == null)
                return _localRotation;
            
            return parentTransform.Rotation + _localRotation;
        }
        set
        {
            if (Entity?.Parent == null)
            {
                _localRotation = value;
            }
            else
            {
                var parentTransform = Entity.Parent.GetComponent<TransformComponent>();
                if (parentTransform == null)
                {
                    _localRotation = value;
                }
                else
                {
                    _localRotation = value - parentTransform.Rotation;
                }
            }
        }
    }
    
    /// <summary>
    /// Gets or sets the world scale (computed from parent hierarchy).
    /// </summary>
    public Vector2 Scale
    {
        get
        {
            if (Entity?.Parent == null)
                return _localScale;
            
            var parentTransform = Entity.Parent.GetComponent<TransformComponent>();
            if (parentTransform == null)
                return _localScale;
            
            return parentTransform.Scale * _localScale;
        }
        set
        {
            if (Entity?.Parent == null)
            {
                _localScale = value;
            }
            else
            {
                var parentTransform = Entity.Parent.GetComponent<TransformComponent>();
                if (parentTransform == null)
                {
                    _localScale = value;
                }
                else
                {
                    _localScale = value / parentTransform.Scale;
                }
            }
        }
    }

    /// <summary>
    /// Translates the entity by the specified delta in local space.
    /// </summary>
    public void Translate(Vector2 delta)
    {
        _localPosition += delta;
    }

    /// <summary>
    /// Rotates the entity by the specified angle in radians.
    /// </summary>
    public void Rotate(float angleRadians)
    {
        _localRotation += angleRadians;
    }
    
    /// <summary>
    /// Gets the full transformation matrix for this transform.
    /// Combines position, rotation, and scale with parent transforms.
    /// </summary>
    /// <remarks>
    /// Use this for rendering or when you need the complete transform matrix.
    /// The matrix is computed from world position, rotation, and scale.
    /// </remarks>
    public Matrix3x2 GetTransformMatrix()
    {
        return Matrix3x2.CreateScale(Scale) *
               Matrix3x2.CreateRotation(Rotation) *
               Matrix3x2.CreateTranslation(Position);
    }
}