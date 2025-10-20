namespace Brine2D;

/// <summary>
///     The superclass of all Brine2D types.
/// </summary>
public abstract class Object
{
    /// <summary>
    ///     <para>
    ///         Destroys the object's Lua reference. The object will be completely deleted if it's not referenced by any other
    ///         LÖVE object or thread.
    ///     </para>
    ///     <para>This method can be used to immediately clean up resources without waiting for Lua's garbage collector.</para>
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         After this method has been called, attempting to call any other method on the object or using the object as an
    ///         argument in another LÖVE API will cause an error.
    ///     </para>
    /// </remarks>
    /// <returns>
    ///     <see langword="true" /> if the object was released by this call, <see langword="false" /> if it had been previously
    ///     released.
    /// </returns>
    /// TODO: This matches the Love2D API, but C# has garbage collection. Consider whether this method is necessary.
    public virtual bool Release()
    {
        return true;
    }

    /// <summary>
    ///     Gets the type of the object as a string.
    /// </summary>
    /// <returns>The type as a string.</returns>
    /// TODO: This matches the Love2D API, but C# has built-in type checking. Consider whether this method is necessary.
    public virtual string Type()
    {
        return GetType().Name;
    }

    /// <summary>
    ///     <para>Checks whether an object is of a certain type.</para>
    ///     <para>If the object has the type with the specified name in its hierarchy, this function will return true.</para>
    /// </summary>
    /// <param name="name">The name of the type to check for.</param>
    /// <returns>True if the object is of the specified type, false otherwise.</returns>
    /// TODO: This matches the Love2D API, but C# has built-in type checking. Consider whether this method is necessary.
    public virtual bool TypeOf(string name)
    {
        for (var t = GetType(); t != null; t = t.BaseType)
        {
            if (t.Name == name) return true;
        }
        return false;
    }
}