using System.Numerics;
using Brine2D.Core;
using Brine2D.ECS;

namespace Brine2D.Collision;

/// <summary>
///     Simple spatial partitioning grid for efficient area/point/ray queries.
///     Stores entities by their bounding rectangle. Not used for physics (Box2D handles that);
///     useful for gameplay queries like "find all enemies near this point".
/// </summary>
public class SpatialGrid(int cellSize = 100)
{
    private readonly Dictionary<(int, int), List<(Entity Entity, Rectangle Bounds)>> _cells = new();
    private int _clearCount;

    public void Clear()
    {
        foreach (var cell in _cells.Values)
        {
            cell.Clear();
        }

        if (++_clearCount >= 600)
        {
            _clearCount = 0;
            TrimEmptyCells();
        }
    }

    public void Insert(Entity entity, Rectangle bounds)
    {
        var minCell = GetCell(bounds.Left, bounds.Top);
        var maxCell = GetCell(bounds.Right, bounds.Bottom);

        for (var x = minCell.x; x <= maxCell.x; x++)
        {
            for (var y = minCell.y; y <= maxCell.y; y++)
            {
                var key = (x, y);
                if (!_cells.TryGetValue(key, out var list))
                {
                    list = [];
                    _cells[key] = list;
                }

                list.Add((entity, bounds));
            }
        }
    }

    public void QueryArea(Rectangle area, HashSet<Entity> results)
    {
        var minCell = GetCell(area.Left, area.Top);
        var maxCell = GetCell(area.Right, area.Bottom);

        for (var x = minCell.x; x <= maxCell.x; x++)
        {
            for (var y = minCell.y; y <= maxCell.y; y++)
            {
                if (_cells.TryGetValue((x, y), out var entries))
                {
                    foreach (var (entity, bounds) in entries)
                    {
                        if (area.Intersects(bounds))
                        {
                            results.Add(entity);
                        }
                    }
                }
            }
        }
    }

    public void QueryPoint(Vector2 point, HashSet<Entity> results)
    {
        var cell = GetCell(point.X, point.Y);

        if (_cells.TryGetValue(cell, out var entries))
        {
            foreach (var (entity, bounds) in entries)
            {
                if (bounds.Contains(point))
                {
                    results.Add(entity);
                }
            }
        }
    }

    public void QueryRay(Vector2 origin, Vector2 direction, float maxDistance, HashSet<Entity> results)
    {
        var end = origin + direction * maxDistance;

        var minX = MathF.Min(origin.X, end.X);
        var minY = MathF.Min(origin.Y, end.Y);
        var maxX = MathF.Max(origin.X, end.X);
        var maxY = MathF.Max(origin.Y, end.Y);

        var minCell = GetCell(minX, minY);
        var maxCell = GetCell(maxX, maxY);

        for (var x = minCell.x; x <= maxCell.x; x++)
        {
            for (var y = minCell.y; y <= maxCell.y; y++)
            {
                if (_cells.TryGetValue((x, y), out var entries))
                {
                    foreach (var (entity, _) in entries)
                    {
                        results.Add(entity);
                    }
                }
            }
        }
    }

    private (int x, int y) GetCell(float worldX, float worldY)
    {
        return ((int)MathF.Floor(worldX / cellSize), (int)MathF.Floor(worldY / cellSize));
    }

    private void TrimEmptyCells()
    {
        List<(int, int)>? toRemove = null;

        foreach (var (key, list) in _cells)
        {
            if (list.Count == 0)
            {
                (toRemove ??= []).Add(key);
            }
        }

        if (toRemove != null)
        {
            foreach (var key in toRemove)
            {
                _cells.Remove(key);
            }
        }
    }
}