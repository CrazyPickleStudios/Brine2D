namespace Brine2D;

/// <summary>
///     The superclass of all data.
/// </summary>
public abstract class Data : Object
{
    /// <summary>
    ///     Creates a new copy of the Data object.
    /// </summary>
    /// <returns>
    ///     The new copy.
    /// </returns>
    public abstract Data Clone();

    /// <summary>
    ///     <para>Gets an FFI pointer to the Data.</para>
    ///     <para>
    ///         This function should be preferred instead of Data:getPointer because the latter uses light userdata which
    ///         can't store more all possible memory addresses on some new ARM64 architectures, when LuaJIT is used.
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     Use at your own risk. Directly reading from and writing to the raw memory owned by the Data will bypass any
    ///     safety checks and thread-safety the Data might normally have.
    /// </remarks>
    /// <returns>
    ///     A raw void* pointer to the Data, or null if FFI is unavailable.
    /// </returns>
    public abstract IntPtr GetFFIPointer();

    /// <summary>
    ///     <para>Gets a pointer to the Data. Can be used with libraries such as LuaJIT's FFI.</para>
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Use at your own risk. Directly reading from and writing to the raw memory owned by the Data will bypass any
    ///         safety checks and thread-safety the Data might normally have.
    ///     </para>
    ///     <para>
    ///         Since LÖVE 11.3, Data:getFFIPointer is a preferred alternative because it can work with new 64-bit
    ///         architectures.
    ///     </para>
    /// </remarks>
    /// <returns>
    ///     A raw pointer to the Data.
    /// </returns>
    public abstract IntPtr GetPointer();

    /// <summary>
    ///     <para>Gets the Data's size in bytes.</para>
    /// </summary>
    /// <returns>
    ///     The size of the Data in bytes.
    /// </returns>
    public abstract double GetSize();

    /// <summary>
    ///     <para>Gets the full Data as a string.</para>
    /// </summary>
    /// <param name="offset">Optional byte offset into the Data's memory to copy from</param>
    /// <param name="size">Optionally limit the copied string to the specified number of bytes.</param>
    /// <returns>
    ///     The raw data.
    /// </returns>
    /// TODO: the original signature is Data:getSize(), not sure what the most idiomatic way to represent that in C# is.
    public abstract string GetString(double offset = 0, double size = double.MaxValue);
}