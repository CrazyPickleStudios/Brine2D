namespace Brine2D.Filesystem
{
    /// <summary>
    /// <para>Represents a file dropped onto the window.</para>
/// <para>Note that the DroppedFile type can only be obtained from love.filedropped callback, and can't be constructed manually by the user.</para>
    /// </summary>
    // TODO: Requires Review
    public class DroppedFile
    {
        /// <summary>
        /// <para>Closes a File.</para>
        /// </summary>
        /// <param name="success">Whether closing was successful.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>success</term><description>Whether closing was successful.</description></item>
        /// </list>
        /// </returns>
        public bool Close(bool success) => throw new NotImplementedException();
        /// <summary>
        /// <para>Flushes any buffered written data in the file to the disk.</para>
        /// </summary>
        /// <param name="success">Whether the file successfully flushed any buffered data to the disk.</param>
        /// <param name="err">The error string, if an error occurred and the file could not be flushed.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>success</term><description>Whether the file successfully flushed any buffered data to the disk.</description></item>
        /// <item><term>err</term><description>The error string, if an error occurred and the file could not be flushed.</description></item>
        /// </list>
        /// </returns>
        public (bool success, string err) Flush(bool success, string err = "nil") => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the buffer mode of a file.</para>
        /// </summary>
        /// <param name="mode">The current buffer mode of the file.</param>
        /// <param name="size">The maximum size in bytes of the file's buffer.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>mode</term><description>The current buffer mode of the file.</description></item>
        /// <item><term>size</term><description>The maximum size in bytes of the file's buffer.</description></item>
        /// </list>
        /// </returns>
        public (object mode, double size) GetBuffer(object mode, double size) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the filename that the File object was created with. If the file object originated from the love.filedropped callback, the filename will be the full platform-dependent file path.</para>
        /// </summary>
        /// <param name="filename">The filename of the File.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>filename</term><description>The filename of the File.</description></item>
        /// </list>
        /// </returns>
        public string GetFilename(string filename) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the FileMode the file has been opened with.</para>
        /// </summary>
        /// <param name="mode">The mode this file has been opened with.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>mode</term><description>The mode this file has been opened with.</description></item>
        /// </list>
        /// </returns>
        public FileMode GetMode(FileMode mode) => throw new NotImplementedException();
        /// <summary>
        /// <para>Returns the file size.</para>
        /// </summary>
        /// <param name="size">The file size in bytes.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>size</term><description>The file size in bytes.</description></item>
        /// </list>
        /// </returns>
        public double GetSize(double size) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets whether end-of-file has been reached.</para>
        /// </summary>
        /// <param name="eof">Whether EOF has been reached.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>eof</term><description>Whether EOF has been reached.</description></item>
        /// </list>
        /// </returns>
        public bool IsEOF(bool eof) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets whether the file is open.</para>
        /// </summary>
        /// <param name="open">True if the file is currently open, false otherwise.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>open</term><description>True if the file is currently open, false otherwise.</description></item>
        /// </list>
        /// </returns>
        public bool IsOpen(bool open) => throw new NotImplementedException();
        /// <summary>
        /// <para>Iterate over all the lines in a file.</para>
        /// </summary>
        /// <param name="iterator">The iterator (can be used in for loops).</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>iterator</term><description>The iterator (can be used in for loops).</description></item>
        /// </list>
        /// </returns>
        public object Lines(object iterator) => throw new NotImplementedException();
        /// <summary>
        /// <para>Open the file for write, read or append.</para>
        /// </summary>
        /// <param name="mode">The mode to open the file in.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>ok</term><description>True on success, false otherwise.</description></item>
        /// <item><term>err</term><description>The error string if an error occurred.</description></item>
        /// </list>
        /// </returns>
        public (bool ok, string err) Open(FileMode mode) => throw new NotImplementedException();
        /// <summary>
        /// <para>Read a number of bytes from a file.</para>
        /// </summary>
        /// <param name="bytes">The number of bytes to read.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>contents</term><description>The contents of the read bytes.</description></item>
        /// <item><term>size</term><description>How many bytes have been read.</description></item>
        /// </list>
        /// </returns>
        // TODO: public (string contents, double size) Read(double bytes = all) => throw new NotImplementedException();
        /// <summary>
        /// <para>Read a number of bytes from a file.</para>
        /// </summary>
        /// <param name="container">What type to return the file's contents as.</param>
        /// <param name="bytes">The number of bytes to read.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>contents</term><description>or string containing the read bytes.</description></item>
        /// <item><term>size</term><description>How many bytes have been read.</description></item>
        /// </list>
        /// </returns>
        // TODO: public (object contents, double size) Read(object container, double bytes = all) => throw new NotImplementedException();
        /// <summary>
        /// <para>Seek to a position in a file</para>
        /// </summary>
        /// <param name="pos">The position to seek to, relative to start of file.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>success</term><description>Whether the operation was successful</description></item>
        /// </list>
        /// </returns>
        public bool Seek(double pos) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the buffer mode for a file opened for writing or appending. Files with buffering enabled will not write data to the disk until the buffer size limit is reached, depending on the buffer mode.</para>
        /// <para>File:flush will force any buffered data to be written to the disk.</para>
        /// </summary>
        /// <param name="mode">The buffer mode to use.</param>
        /// <param name="size">The maximum size in bytes of the file's buffer.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>success</term><description>Whether the buffer mode was successfully set.</description></item>
        /// <item><term>errorstr</term><description>The error string, if the buffer mode could not be set and an error occurred.</description></item>
        /// </list>
        /// </returns>
        public (bool success, string errorstr) SetBuffer(object mode, double size = 0) => throw new NotImplementedException();
        /// <summary>
        /// <para>Returns the position in the file.</para>
        /// </summary>
        /// <param name="pos">The current position.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>pos</term><description>The current position.</description></item>
        /// </list>
        /// </returns>
        public double Tell(double pos) => throw new NotImplementedException();
        /// <summary>
        /// <para>Write data to a file.</para>
        /// </summary>
        /// <param name="data">The string data to write.</param>
        /// <param name="size">How many bytes to write.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>success</term><description>Whether the operation was successful.</description></item>
        /// <item><term>err</term><description>The error string if an error occurred.</description></item>
        /// </list>
        /// </returns>
        // TODO: public (bool success, string err) Write(string data, double size = all) => throw new NotImplementedException();
        /// <summary>
        /// <para>Write data to a file.</para>
        /// </summary>
        /// <param name="data">The Data object to write.</param>
        /// <param name="size">How many bytes to write.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>success</term><description>Whether the operation was successful.</description></item>
        /// <item><term>errorstr</term><description>The error string if an error occurred.</description></item>
        /// </list>
        /// </returns>
        // TODO: public (bool success, string errorstr) Write(object data, double size = all) => throw new NotImplementedException();
        /// <summary>
        /// <para>Write data to a file.</para>
        /// </summary>
        public void NewFile() => throw new NotImplementedException();
        /// <summary>
        /// <para>Destroys the object's Lua reference. The object will be completely deleted if it's not referenced by any other LÖVE object or thread.</para>
        /// <para>This method can be used to immediately clean up resources without waiting for Lua's garbage collector.</para>
        /// </summary>
        /// <param name="success">True if the object was released by this call, false if it had been previously released.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>success</term><description>True if the object was released by this call, false if it had been previously released.</description></item>
        /// </list>
        /// </returns>
        public bool Release(bool success) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the type of the object as a string.</para>
        /// </summary>
        /// <param name="type">The type as a string.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>type</term><description>The type as a string.</description></item>
        /// </list>
        /// </returns>
        public string Type(string type) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the type of the object as a string.</para>
        /// </summary>
        // TODO: public void NewImage() => throw new NotImplementedException();
        /// <summary>
        /// <para>Checks whether an object is of a certain type. If the object has the type with the specified name in its hierarchy, this function will return true.</para>
        /// </summary>
        /// <param name="name">The name of the type to check for.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>b</term><description>True if the object is of the specified type, false otherwise.</description></item>
        /// </list>
        /// </returns>
        public bool TypeOf(string name) => throw new NotImplementedException();
        /// <summary>
        /// <para>Checks whether an object is of a certain type. If the object has the type with the specified name in its hierarchy, this function will return true.</para>
        /// </summary>
        // TODO: public void NewImage() => throw new NotImplementedException();
    }
}
