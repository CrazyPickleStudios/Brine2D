using Brine2D.Core;

namespace Brine2D.Rendering;

/// <summary>
/// Shared scissor rectangle intersection logic used by both
/// <see cref="HeadlessRenderer"/> and <see cref="SDL3StateManager"/>.
/// </summary>
internal static class ScissorRectHelper
{
    /// <summary>
    /// Computes the effective scissor rectangle when pushing a new rect onto an existing one.
    /// Returns the intersection of <paramref name="current"/> and <paramref name="incoming"/>,
    /// or a zero-area rectangle when the two do not overlap.
    /// </summary>
    public static Rectangle? Intersect(Rectangle? current, Rectangle? incoming)
    {
        if (!incoming.HasValue)
            return current;

        if (!current.HasValue)
            return incoming;

        return current.Value.Intersection(incoming.Value)
               ?? new Rectangle(incoming.Value.X, incoming.Value.Y, 0, 0);
    }
}