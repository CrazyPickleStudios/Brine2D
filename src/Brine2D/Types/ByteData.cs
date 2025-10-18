//namespace Brine2D
//{
//    /// <summary>
//    /// <para>Data object containing arbitrary bytes in an contiguous memory.</para>
///// <para>There are currently no LÖVE functions provided for manipulating the contents of a ByteData, but Data:getFFIPointer can be used with LuaJIT's FFI to access and write to the contents directly.</para>
///// <para>Used primarily for creating ImageData, Image, also love.filesystem.mount etc.</para>
//    /// </summary>
//    // TODO: Requires Review
//    public class ByteData : Data
//    {
//        /// <summary>
//        /// <para>Replaces all or part of the ByteData's memory with the contents of a string.</para>
//        /// </summary>
//        /// <param name="data">The bytes to copy to the Data object.</param>
//        /// <param name="offset">An optional byte offset into the Data's memory to copy to.</param>
//        public void SetString(string data, double offset = 0) => throw new NotImplementedException();
//        /// <summary>
//        /// <para>Creates a new copy of the Data object.</para>
//        /// </summary>
//        /// <param name="clone">The new copy.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>clone</term><description>The new copy.</description></item>
//        /// </list>
//        /// </returns>
//        public object Clone(object clone) => throw new NotImplementedException();
//        /// <summary>
//        /// <para>Gets an FFI pointer to the Data.</para>
//        /// <para>This function should be preferred instead of Data:getPointer because the latter uses</para>
//        /// <para>light userdata which can't store more all possible memory addresses on some new ARM64</para>
//        /// <para>architectures, when LuaJIT is used.</para>
//        /// </summary>
//        /// <param name="pointer">A raw pointer to the Data, or if FFI is unavailable.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>pointer</term><description>A raw pointer to the Data, or if FFI is unavailable.</description></item>
//        /// </list>
//        /// </returns>
//        public object GetFFIPointer(object pointer) => throw new NotImplementedException();
//        /// <summary>
//        /// <para>Gets a pointer to the Data. Can be used with libraries such as LuaJIT's FFI.</para>
//        /// </summary>
//        /// <param name="userdata">A raw pointer to the Data.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>userdata</term><description>A raw pointer to the Data.</description></item>
//        /// </list>
//        /// </returns>
//        public object GetPointer(object userdata) => throw new NotImplementedException();
//        /// <summary>
//        /// <para>Gets the Data's size in bytes.</para>
//        /// </summary>
//        /// <param name="size">The size of the Data in bytes.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>size</term><description>The size of the Data in bytes.</description></item>
//        /// </list>
//        /// </returns>
//        public double GetSize(double size) => throw new NotImplementedException();
//        /// <summary>
//        /// <para>Gets the full Data as a string.</para>
//        /// </summary>
//        /// <param name="data">The raw data.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>data</term><description>The raw data.</description></item>
//        /// </list>
//        /// </returns>
//        public string GetString(string data) => throw new NotImplementedException();
//        /// <summary>
//        /// <para>Destroys the object's Lua reference. The object will be completely deleted if it's not referenced by any other LÖVE object or thread.</para>
//        /// <para>This method can be used to immediately clean up resources without waiting for Lua's garbage collector.</para>
//        /// </summary>
//        /// <param name="success">True if the object was released by this call, false if it had been previously released.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>success</term><description>True if the object was released by this call, false if it had been previously released.</description></item>
//        /// </list>
//        /// </returns>
//        public bool Release(bool success) => throw new NotImplementedException();
//        /// <summary>
//        /// <para>Gets the type of the object as a string.</para>
//        /// </summary>
//        /// <param name="type">The type as a string.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>type</term><description>The type as a string.</description></item>
//        /// </list>
//        /// </returns>
//        public string Type(string type) => throw new NotImplementedException();
//        /// <summary>
//        /// <para>Gets the type of the object as a string.</para>
//        /// </summary>
//        // TODO: public void NewImage() => throw new NotImplementedException();
//        /// <summary>
//        /// <para>Checks whether an object is of a certain type. If the object has the type with the specified name in its hierarchy, this function will return true.</para>
//        /// </summary>
//        /// <param name="name">The name of the type to check for.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>b</term><description>True if the object is of the specified type, false otherwise.</description></item>
//        /// </list>
//        /// </returns>
//        // TODO: public bool TypeOf(string name) => throw new NotImplementedException();
//        /// <summary>
//        /// <para>Checks whether an object is of a certain type. If the object has the type with the specified name in its hierarchy, this function will return true.</para>
//        /// </summary>
//        public void NewImage() => throw new NotImplementedException();
//    }
//}
