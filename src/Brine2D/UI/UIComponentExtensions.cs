namespace Brine2D.UI;

internal static class UIComponentExtensions
{
    /// <summary>
    /// Sorts the list by <see cref="IUIComponent.TabIndex"/> ascending, preserving the relative
    /// order of elements with equal TabIndex values (stable sort).
    /// </summary>
    internal static void StableSortByTabIndex(this List<IUIComponent> list)
    {
        // OrderBy is guaranteed stable in .NET.
        var sorted = list.OrderBy(c => c.TabIndex).ToList();
        list.Clear();
        list.AddRange(sorted);
    }
}
