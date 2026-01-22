using System;
using System.Collections.Generic;
using System.Text;

namespace Brine2D.Pooling;

/// <summary>
/// Interface for objects that can be pooled and reset.
/// </summary>
public interface IPoolable
{
    /// <summary>
    /// Called when the object is returned to the pool.
    /// Reset state here to avoid polluting reused objects.
    /// </summary>
    void Reset();
}
