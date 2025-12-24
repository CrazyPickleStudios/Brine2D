using System.Numerics;

namespace Brine2D.Core.Collision;

/// <summary>
/// Simple spatial partitioning grid for efficient collision detection.
/// </summary>
public class SpatialGrid
{
    private readonly Dictionary<(int, int), List<CollisionShape>> _cells = new();
    private readonly int _cellSize;

    public SpatialGrid(int cellSize = 100)
    {
        _cellSize = cellSize;
    }

    /// <summary>
    /// Clears all shapes from the grid.
    /// </summary>
    public void Clear()
    {
        _cells.Clear();
    }

    /// <summary>
    /// Inserts a shape into the grid.
    /// </summary>
    public void Insert(CollisionShape shape)
    {
        var bounds = shape.GetBounds();
        var minCell = GetCell(bounds.TopLeft);
        var maxCell = GetCell(bounds.BottomRight);

        for (int x = minCell.x; x <= maxCell.x; x++)
        {
            for (int y = minCell.y; y <= maxCell.y; y++)
            {
                var key = (x, y);
                if (!_cells.ContainsKey(key))
                    _cells[key] = new List<CollisionShape>();
                
                _cells[key].Add(shape);
            }
        }
    }

    /// <summary>
    /// Gets all shapes that could potentially collide with the given shape.
    /// </summary>
    public HashSet<CollisionShape> Query(CollisionShape shape)
    {
        var results = new HashSet<CollisionShape>();
        var bounds = shape.GetBounds();
        var minCell = GetCell(bounds.TopLeft);
        var maxCell = GetCell(bounds.BottomRight);

        for (int x = minCell.x; x <= maxCell.x; x++)
        {
            for (int y = minCell.y; y <= maxCell.y; y++)
            {
                var key = (x, y);
                if (_cells.TryGetValue(key, out var cellShapes))
                {
                    foreach (var s in cellShapes)
                    {
                        if (s != shape)
                            results.Add(s);
                    }
                }
            }
        }

        return results;
    }

    private (int x, int y) GetCell(Vector2 position)
    {
        return (
            (int)Math.Floor(position.X / _cellSize),
            (int)Math.Floor(position.Y / _cellSize)
        );
    }
}