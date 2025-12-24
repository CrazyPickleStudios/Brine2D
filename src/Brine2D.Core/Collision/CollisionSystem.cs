using System.Numerics;

namespace Brine2D.Core.Collision;

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
            // Use spatial partitioning for better performance
            candidates = _spatialGrid.Query(shape);
        }
        else
        {
            // Brute force - check all shapes
            candidates = _shapes.Where(s => s != shape && s.IsEnabled);
        }

        foreach (var other in candidates)
        {
            if (shape.Intersects(other))
            {
                collisions.Add(other);
            }
        }

        return collisions;
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
    public List<CollisionShape> QueryArea(RectangleF area)
    {
        var results = new List<CollisionShape>();

        foreach (var shape in _shapes)
        {
            if (!shape.IsEnabled)
                continue;

            if (area.Intersects(shape.GetBounds()))
            {
                results.Add(shape);
            }
        }

        return results;
    }
}