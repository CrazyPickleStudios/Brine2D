using System.Numerics;
using System.Text.Json.Serialization;

namespace Brine2D.ECS.Components;

/// <summary>
/// Component for entity position, rotation, and scale with hierarchical support.
/// Supports parent/child relationships with relative transforms.
/// </summary>
public class TransformComponent : Component
{
    private Vector2 _localPosition;
    private float _localRotation;
    private Vector2 _localScale = Vector2.One;
    
    private Vector2 _worldPosition;
    private float _worldRotation;
    private Vector2 _worldScale = Vector2.One;
    
    private bool _isDirty = true;

    /// <summary>
    /// Parent transform (null if root entity).
    /// </summary>
    [JsonIgnore]
    public TransformComponent? Parent { get; private set; }

    /// <summary>
    /// Children transforms.
    /// </summary>
    [JsonIgnore]
    public IReadOnlyList<TransformComponent> Children => _children.AsReadOnly();
    
    private readonly List<TransformComponent> _children = new();

    /// <summary>
    /// Local position relative to parent (or world if no parent).
    /// </summary>
    public Vector2 LocalPosition
    {
        get => _localPosition;
        set
        {
            if (_localPosition != value)
            {
                _localPosition = value;
                MarkDirty();
            }
        }
    }

    /// <summary>
    /// Local rotation in degrees relative to parent.
    /// </summary>
    public float LocalRotation
    {
        get => _localRotation;
        set
        {
            if (_localRotation != value)
            {
                _localRotation = value;
                MarkDirty();
            }
        }
    }

    /// <summary>
    /// Local scale relative to parent.
    /// </summary>
    public Vector2 LocalScale
    {
        get => _localScale;
        set
        {
            if (_localScale != value)
            {
                _localScale = value;
                MarkDirty();
            }
        }
    }

    /// <summary>
    /// World position (calculated from hierarchy).
    /// Setting this will adjust LocalPosition to achieve the desired world position.
    /// </summary>
    [JsonIgnore]
    public Vector2 WorldPosition
    {
        get
        {
            UpdateWorldTransform();
            return _worldPosition;
        }
        set
        {
            if (Parent != null)
            {
                // Convert world position to local position
                var parentWorldPos = Parent.WorldPosition;
                var parentWorldRot = Parent.WorldRotation;
                var parentWorldScale = Parent.WorldScale;

                // Inverse transform to get local position
                var offset = value - parentWorldPos;
                var angle = -parentWorldRot * MathF.PI / 180f;
                var cos = MathF.Cos(angle);
                var sin = MathF.Sin(angle);

                LocalPosition = new Vector2(
                    (offset.X * cos - offset.Y * sin) / parentWorldScale.X,
                    (offset.X * sin + offset.Y * cos) / parentWorldScale.Y
                );
            }
            else
            {
                LocalPosition = value;
            }
        }
    }

    /// <summary>
    /// World rotation in degrees (calculated from hierarchy).
    /// </summary>
    [JsonIgnore]
    public float WorldRotation
    {
        get
        {
            UpdateWorldTransform();
            return _worldRotation;
        }
        set
        {
            if (Parent != null)
            {
                LocalRotation = value - Parent.WorldRotation;
            }
            else
            {
                LocalRotation = value;
            }
        }
    }

    /// <summary>
    /// World scale (calculated from hierarchy).
    /// </summary>
    [JsonIgnore]
    public Vector2 WorldScale
    {
        get
        {
            UpdateWorldTransform();
            return _worldScale;
        }
    }

    /// <summary>
    /// Convenience property: same as WorldPosition.
    /// </summary>
    [JsonIgnore]
    public Vector2 Position
    {
        get => WorldPosition;
        set => WorldPosition = value;
    }

    /// <summary>
    /// Convenience property: same as WorldRotation.
    /// </summary>
    [JsonIgnore]
    public float Rotation
    {
        get => WorldRotation;
        set => WorldRotation = value;
    }

    /// <summary>
    /// Convenience property: same as LocalScale (scale is typically set locally).
    /// </summary>
    public Vector2 Scale
    {
        get => LocalScale;
        set => LocalScale = value;
    }

    /// <summary>
    /// Sets the parent of this transform.
    /// Pass null to detach from parent.
    /// </summary>
    public void SetParent(TransformComponent? newParent, bool keepWorldPosition = true)
    {
        if (Parent == newParent)
            return;

        // Store current world position if we want to keep it
        var worldPos = keepWorldPosition ? WorldPosition : LocalPosition;
        var worldRot = keepWorldPosition ? WorldRotation : LocalRotation;

        // Remove from old parent
        if (Parent != null)
        {
            Parent._children.Remove(this);
        }

        // Set new parent
        Parent = newParent;

        // Add to new parent
        if (Parent != null)
        {
            Parent._children.Add(this);
        }

        // Restore world position if requested
        if (keepWorldPosition)
        {
            WorldPosition = worldPos;
            WorldRotation = worldRot;
        }

        MarkDirty();
    }

    /// <summary>
    /// Marks this transform and all children as dirty (needing recalculation).
    /// </summary>
    private void MarkDirty()
    {
        _isDirty = true;

        // Propagate to children
        foreach (var child in _children)
        {
            child.MarkDirty();
        }
    }

    /// <summary>
    /// Updates world transform from local transform and parent.
    /// </summary>
    private void UpdateWorldTransform()
    {
        if (!_isDirty)
            return;

        if (Parent != null)
        {
            // Get parent's world transform
            var parentWorldPos = Parent.WorldPosition;
            var parentWorldRot = Parent.WorldRotation;
            var parentWorldScale = Parent.WorldScale;

            // Calculate world scale
            _worldScale = LocalScale * parentWorldScale;

            // Calculate world rotation
            _worldRotation = LocalRotation + parentWorldRot;

            // Calculate world position (rotate and scale local position, then add parent position)
            var angle = parentWorldRot * MathF.PI / 180f;
            var cos = MathF.Cos(angle);
            var sin = MathF.Sin(angle);
            
            var scaledLocal = LocalPosition * parentWorldScale;
            _worldPosition = parentWorldPos + new Vector2(
                scaledLocal.X * cos - scaledLocal.Y * sin,
                scaledLocal.X * sin + scaledLocal.Y * cos
            );
        }
        else
        {
            // No parent, world = local
            _worldPosition = _localPosition;
            _worldRotation = _localRotation;
            _worldScale = _localScale;
        }

        _isDirty = false;
    }

    /// <summary>
    /// Gets all children recursively.
    /// </summary>
    public IEnumerable<TransformComponent> GetChildrenRecursive()
    {
        foreach (var child in _children)
        {
            yield return child;

            foreach (var grandchild in child.GetChildrenRecursive())
            {
                yield return grandchild;
            }
        }
    }

    /// <summary>
    /// Detaches all children.
    /// </summary>
    public void ClearChildren()
    {
        foreach (var child in _children.ToList())
        {
            child.SetParent(null);
        }
    }

    protected internal override void OnRemoved()
    {
        base.OnRemoved();

        // Detach from parent
        SetParent(null);

        // Detach all children
        ClearChildren();
    }
}