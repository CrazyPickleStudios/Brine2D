using System.Buffers;

namespace Brine2D.ECS;

internal static class SortHelper
{
    public static void StableSort<T>(List<T> list, Comparison<T> comparison)
    {
        if (list.Count <= 1) return;

        var count = list.Count;
        var indexed = ArrayPool<(T Item, int Index)>.Shared.Rent(count);
        try
        {
            for (int i = 0; i < count; i++)
                indexed[i] = (list[i], i);

            indexed.AsSpan(0, count).Sort((a, b) =>
            {
                int result = comparison(a.Item, b.Item);
                return result != 0 ? result : a.Index.CompareTo(b.Index);
            });

            for (int i = 0; i < count; i++)
                list[i] = indexed[i].Item;
        }
        finally
        {
            ArrayPool<(T, int)>.Shared.Return(indexed, clearArray: true);
        }
    }
}