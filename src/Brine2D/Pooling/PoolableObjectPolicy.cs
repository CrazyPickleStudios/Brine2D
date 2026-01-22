using Microsoft.Extensions.ObjectPool;

namespace Brine2D.Pooling;

/// <summary>
/// Pool policy for IPoolable objects.
/// Handles creation and reset logic.
/// </summary>
public class PoolableObjectPolicy<T> : PooledObjectPolicy<T> where T : class, IPoolable, new()
{
    public override T Create()
    {
        return new T();
    }

    public override bool Return(T obj)
    {
        obj.Reset();
        return true; // true = object can be reused
    }
}
