using System.Numerics;
using Brine2D.Core;

namespace Brine2D.Collision;

/// <summary>
/// Manages collision detection between shapes.
/// </summary>
public class CollisionSystem
{
    private readonly List<CollisionShape> _shapes = new();
    private readonly SpatialGrid? _spatialGrid;
    private readonly bool _useSpatialPartitioning;

    public CollisionSystem(bool useSpatialPartitioning = false, int gridCellSize = 100)
    {
        _useSpatialPartitioning = useSpatialPartitioning;
        if (_useSpatialPartitioning)
        {
            _spatialGrid = new SpatialGrid(gridCellSize);
        }
    }

    /// <summary>
    /// Registers a shape for collision detection.
    /// </summary>
    public void AddShape(CollisionShape shape)
    {
        if (!_shapes.Contains(shape))
        {
            _shapes.Add(shape);
        }
    }

    /// <summary>
    /// Unregisters a shape from collision detection.
    /// </summary>
    public void RemoveShape(CollisionShape shape)
    {
        _shapes.Remove(shape);
    }

    /// <summary>
    /// Clears all registered shapes.
    /// </summary>
    public void Clear()
    {
        _shapes.Clear();
    }

    /// <summary>
    /// Checks if a shape collides with any registered shape.
    /// </summary>
    public bool CheckCollision(CollisionShape shape)
    {
        if (!shape.IsEnabled)
            return false;

        foreach (var other in _shapes)
        {
            if (other == shape || !other.IsEnabled)
                continue;

            if (shape.Intersects(other))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Rebuilds the spatial grid. Call this after moving many objects.
    /// </summary>
    public void UpdateSpatialGrid()
    {
        if (_spatialGrid == null) return;

        _spatialGrid.Clear();
        foreach (var shape in _shapes)
        {
            if (shape.IsEnabled)
                _spatialGrid.Insert(shape);
        }
    }

    /// <summary>
    /// Gets all shapes that collide with the given shape.
    /// </summary>
    public List<CollisionShape> GetCollisions(CollisionShape shape)
    {
        var collisions = new List<CollisionShape>();

        if (!shape.IsEnabled)
            return collisions;

        IEnumerable<CollisionShape> candidates;

        if (_useSpatialPartitioning && _spatialGrid != null)
        {
            candidates = _spatialGrid.Query(shape);
        }
        else
        {
            candidates = _shapes.Where(s => s != shape && s.IsEnabled);
        }

        foreach (var other in candidates)
        {
            if (shape.Intersects(other))
                collisions.Add(other);
        }

        return collisions;
    }

    /// <summary>
    /// Fills <paramref name="results"/> with all shapes that collide with <paramref name="shape"/>.
    /// Clears the list before writing. Use this overload in hot paths to avoid
    /// allocating a new <see cref="List{T}"/> on every call.
    /// </summary>
    public void GetCollisions(CollisionShape shape, List<CollisionShape> results)
    {
        results.Clear();

        if (!shape.IsEnabled)
            return;

        if (_useSpatialPartitioning && _spatialGrid != null)
        {
            foreach (var other in _spatialGrid.Query(shape))
            {
                if (shape.Intersects(other))
                    results.Add(other);
            }
        }
        else
        {
            foreach (var other in _shapes)
            {
                if (other != shape && other.IsEnabled && shape.Intersects(other))
                    results.Add(other);
            }
        }
    }

    /// <summary>
    /// Checks if a point intersects any registered shape.
    /// </summary>
    public CollisionShape? PointTest(Vector2 point)
    {
        foreach (var shape in _shapes)
        {
            if (!shape.IsEnabled)
                continue;

            if (shape.GetBounds().Contains(point))
                return shape;
        }

        return null;
    }

    /// <summary>
    /// Gets all shapes within a rectangular area.
    /// </summary>
    public List<CollisionShape> QueryArea(Rectangle area)
    {
        var results = new List<CollisionShape>();

        foreach (var shape in _shapes)
        {
            if (!shape.IsEnabled)
                continue;

            if (area.Intersects(shape.GetBounds()))
                results.Add(shape);
        }

        return results;
    }

    /// <summary>
    /// Fills <paramref name="results"/> with all shapes within <paramref name="area"/>.
    /// Clears the list before writing. Use this overload in hot paths to avoid
    /// allocating a new <see cref="List{T}"/> on every call.
    /// </summary>
    public void QueryArea(Rectangle area, List<CollisionShape> results)
    {
        results.Clear();

        foreach (var shape in _shapes)
        {
            if (shape.IsEnabled && area.Intersects(shape.GetBounds()))
                results.Add(shape);
        }
    }

    /// <summary>
    /// Tries to get the first collision with the given shape.
    /// Short-circuits on the first hit — does not allocate a list.
    /// </summary>
    public bool TryGetFirstCollision(CollisionShape shape, out CollisionShape? collision)
    {
        if (!shape.IsEnabled)
        {
            collision = null;
            return false;
        }

        if (_useSpatialPartitioning && _spatialGrid != null)
        {
            foreach (var other in _spatialGrid.Query(shape))
            {
                if (shape.Intersects(other))
                {
                    collision = other;
                    return true;
                }
            }
        }
        else
        {
            foreach (var other in _shapes)
            {
                if (other != shape && other.IsEnabled && shape.Intersects(other))
                {
                    collision = other;
                    return true;
                }
            }
        }

        collision = null;
        return false;
    }

    /// <summary>
    /// Gets all collisions with a specific tag (requires ColliderComponent integration).
    /// Returns an empty collection until shape-to-entity mapping is implemented.
    /// </summary>
    public IReadOnlyList<CollisionShape> GetCollisionsByTag(CollisionShape shape, string tag)
    {
        return Array.Empty<CollisionShape>();
    }
}