using System;

namespace Brine2D;

// TODO: Needs review
public sealed class FilesystemModule
{
    /// <summary>
    /// <para>Append data to an existing file.</para>
    /// </summary>
    /// <param name="name">The name (and path) of the file.</param>
    /// <param name="data">The string data to append to the file.</param>
    /// <param name="size">How many bytes to write.</param>
    /// <returns>
    /// <list type="bullet">
    /// <item><term>success</term><description>True if the operation was successful, or nil if there was an error.</description></item>
    /// <item><term>errormsg</term><description>The error message on failure.</description></item>
    /// </list>
    /// </returns>
    /// TODO: Is MaxValue the right default for size here?
    public (bool success, string errormsg) Append(string name, string data, double size = double.MaxValue) => throw new NotImplementedException();

    /// <summary>
    /// <para>Append data to an existing file.</para>
    /// </summary>
    /// <param name="name">The name (and path) of the file.</param>
    /// <param name="data">The Data object to append to the file.</param>
    /// <param name="size">How many bytes to write.</param>
    /// <returns>
    /// <list type="bullet">
    /// <item><term>success</term><description>True if the operation was successful, or nil if there was an error.</description></item>
    /// <item><term>errormsg</term><description>The error message on failure.</description></item>
    /// </list>
    /// </returns>
    /// TODO: Is MaxValue the right default for size here?
    public (bool success, string errormsg) Append(string name, object data, double size = double.MaxValue) => throw new NotImplementedException();

/// <summary>
        /// <para>Gets whether love.filesystem follows symbolic links.</para>
        /// </summary>
        /// <param name="enable">Whether love.filesystem follows symbolic links.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>enable</term><description>Whether love.filesystem follows symbolic links.</description></item>
        /// </list>
        /// </returns>
    public bool AreSymlinksEnabled(bool enable) => throw new NotImplementedException();

/// <summary>
        /// <para>Recursively creates a directory.</para>
        /// <para>When called with "a/b" it creates both "a" and "a/b", if they don't exist already.</para>
        /// </summary>
        /// <param name="name">The directory to create.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>success</term><description>True if the directory was created, false if not.</description></item>
        /// </list>
        /// </returns>
    public bool CreateDirectory(string name) => throw new NotImplementedException();

/// <summary>
        /// <para>Returns a table with the names of files and subdirectories in the specified path. The table is not sorted in any way; the order is undefined.</para>
        /// <para>If the path passed to the function exists in the game and the save directory, it will list the files and directories from both places.</para>
        /// </summary>
        /// <param name="dir">The directory.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>files</term><description>A with the names of all files and subdirectories as strings.</description></item>
        /// </list>
        /// </returns>
    public object Enumerate(string dir) => throw new NotImplementedException();

/// <summary>
        /// <para>Returns a table with the names of files and subdirectories in the specified path. The table is not sorted in any way; the order is undefined.</para>
        /// <para>If the path passed to the function exists in the game and the save directory, it will list the files and directories from both places.</para>
        /// </summary>
    public void Enumerate() => throw new NotImplementedException();

/// <summary>
        /// <para>Check whether a file or directory exists.</para>
        /// </summary>
        /// <param name="filename">The path to a potential file or directory.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>exists</term><description>True if there is a file or directory with the specified name. False otherwise.</description></item>
        /// </list>
        /// </returns>
    public bool Exists(string filename) => throw new NotImplementedException();

/// <summary>
        /// <para>Returns the application data directory (could be the same as getUserDirectory)</para>
        /// </summary>
        /// <param name="path">The path of the application data directory</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>path</term><description>The path of the application data directory</description></item>
        /// </list>
        /// </returns>
    public string GetAppdataDirectory(string path) => throw new NotImplementedException();

/// <summary>
        /// <para>Gets the filesystem paths that will be searched for c libraries when require is called.</para>
        /// <para>The paths string returned by this function is a sequence of path templates separated by semicolons. The argument passed to require will be inserted in place of any question mark ("?") character in each template (after the dot characters in the argument passed to require are replaced by directory separators.) Additionally, any occurrence of a double question mark ("??") will be replaced by the name passed to require and the default library extension for the platform.</para>
        /// <para>The paths are relative to the game's source and save directories, as well as any paths mounted with love.filesystem.mount.</para>
        /// </summary>
        /// <param name="paths">The paths that the function will check for c libraries in love's filesystem.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>paths</term><description>The paths that the function will check for c libraries in love's filesystem.</description></item>
        /// </list>
        /// </returns>
    public string GetCRequirePath(string paths) => throw new NotImplementedException();

/// <summary>
        /// <para>Returns a table with the names of files and subdirectories in the specified path. The table is not sorted in any way; the order is undefined.</para>
        /// <para>If the path passed to the function exists in the game and the save directory, it will list the files and directories from both places.</para>
        /// </summary>
        /// <param name="dir">The directory.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>files</term><description>A with the names of all files and subdirectories as strings.</description></item>
        /// </list>
        /// </returns>
    public object GetDirectoryItems(string dir) => throw new NotImplementedException();

/// <summary>
        /// <para>Returns a table with the names of files and subdirectories in the specified path. The table is not sorted in any way; the order is undefined.</para>
        /// <para>If the path passed to the function exists in the game and the save directory, it will list the files and directories from both places.</para>
        /// </summary>
    // TODO: public void GetDirectoryItems() => throw new NotImplementedException();

/// <summary>
        /// <para>Gets the write directory name for your game.</para>
        /// <para>Note that this only returns the name of the folder to store your files in, not the full path.</para>
        /// </summary>
        /// <param name="name">The identity that is used as write directory.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>name</term><description>The identity that is used as write directory.</description></item>
        /// </list>
        /// </returns>
    public string GetIdentity(string name) => throw new NotImplementedException();

/// <summary>
        /// <para>Gets information about the specified file or directory.</para>
        /// </summary>
        /// <param name="path">The file or directory path to check.</param>
        /// <param name="filtertype">If supplied, this parameter causes getInfo to only return the info table if the item at the given path matches the specified file type.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>info</term><description>
        /// A table containing information about the specified path, or nil if nothing exists at the path. The table contains the following fields:
        /// <list type="bullet">
        /// <item><term>type</term><description>FileType: The type of the object at the path (file, directory, symlink, etc.)</description></item>
        /// <item><term>size</term><description>number: The size in bytes of the file, or nil if it can't be determined.</description></item>
        /// <item><term>modtime</term><description>number: The file's last modification time in seconds since the unix epoch, or nil if it can't be determined.</description></item>
        /// </list>
        /// </description></item>
        /// </list>
        /// </returns>
    public object GetInfo(string path, FileType? filtertype = null) => throw new NotImplementedException();

/// <summary>
        /// <para>Gets information about the specified file or directory.</para>
        /// </summary>
        /// <param name="path">The file or directory path to check.</param>
        /// <param name="info">A table which will be filled in with info about the specified path.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>info</term><description>
        /// The table given as an argument, or nil if nothing exists at the path. The table will be filled in with the following fields:
        /// <list type="bullet">
        /// <item><term>type</term><description>FileType: The type of the object at the path (file, directory, symlink, etc.)</description></item>
        /// <item><term>size</term><description>number: The size in bytes of the file, or nil if it can't be determined.</description></item>
        /// <item><term>modtime</term><description>number: The file's last modification time in seconds since the unix epoch, or nil if it can't be determined.</description></item>
        /// </list>
        /// </description></item>
        /// </list>
        /// </returns>
    public object GetInfo(string path, object info) => throw new NotImplementedException();

/// <summary>
        /// <para>Gets information about the specified file or directory.</para>
        /// </summary>
        /// <param name="path">The file or directory path to check.</param>
        /// <param name="filtertype">Causes getInfo to only return the info table if the item at the given path matches the specified file type.</param>
        /// <param name="info">A table which will be filled in with info about the specified path.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>info</term><description>
        /// The table given as an argument, or nil if nothing exists at the path. The table will be filled in with the following fields:
        /// <list type="bullet">
        /// <item><term>type</term><description>FileType: The type of the object at the path (file, directory, symlink, etc.)</description></item>
        /// <item><term>size</term><description>number: The size in bytes of the file, or nil if it can't be determined.</description></item>
        /// <item><term>modtime</term><description>number: The file's last modification time in seconds since the unix epoch, or nil if it can't be determined.</description></item>
        /// </list>
        /// </description></item>
        /// </list>
        /// </returns>
    public object GetInfo(string path, FileType filtertype, object info) => throw new NotImplementedException();

/// <summary>
        /// <para>Gets the last modification time of a file.</para>
        /// </summary>
        /// <param name="filename">The path and name to a file.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>modtime</term><description>The last modification time in seconds since the unix epoch or nil on failure.</description></item>
        /// <item><term>errormsg</term><description>The error message on failure.</description></item>
        /// </list>
        /// </returns>
    public (double modtime, string errormsg) GetLastModified(string filename) => throw new NotImplementedException();

/// <summary>
        /// <para>Gets the last modification time of a file.</para>
        /// </summary>
    public void GetLastModified() => throw new NotImplementedException();

/// <summary>
        /// <para>Gets the platform-specific absolute path of the directory containing a filepath.</para>
        /// <para>This can be used to determine whether a file is inside the save directory or the game's source .love.</para>
        /// </summary>
        /// <param name="filepath">The filepath to get the directory of.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>realdir</term><description>The platform-specific full path of the directory containing the filepath.</description></item>
        /// </list>
        /// </returns>
    public string GetRealDirectory(string filepath) => throw new NotImplementedException();

/// <summary>
        /// <para>Gets the platform-specific absolute path of the directory containing a filepath.</para>
        /// <para>This can be used to determine whether a file is inside the save directory or the game's source .love.</para>
        /// </summary>
    public void GetDirectoryItems() => throw new NotImplementedException();

/// <summary>
        /// <para>Gets the filesystem paths that will be searched when require is called.</para>
        /// <para>The paths string returned by this function is a sequence of path templates separated by semicolons. The argument passed to require will be inserted in place of any question mark ("?") character in each template (after the dot characters in the argument passed to require are replaced by directory separators.)</para>
        /// <para>The paths are relative to the game's source and save directories, as well as any paths mounted with love.filesystem.mount.</para>
        /// </summary>
        /// <param name="paths">The paths that the function will check in love's filesystem.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>paths</term><description>The paths that the function will check in love's filesystem.</description></item>
        /// </list>
        /// </returns>
    public string GetRequirePath(string paths) => throw new NotImplementedException();

/// <summary>
        /// <para>Gets the full path to the designated save directory.</para>
        /// <para>This can be useful if you want to use the standard io library (or something else) to</para>
        /// <para>read or write in the save directory.</para>
        /// </summary>
        /// <param name="dir">The absolute path to the save directory.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>dir</term><description>The absolute path to the save directory.</description></item>
        /// </list>
        /// </returns>
    public string GetSaveDirectory(string dir) => throw new NotImplementedException();

/// <summary>
        /// <para>Gets the size in bytes of a file.</para>
        /// </summary>
        /// <param name="filename">The path and name to a file.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>size</term><description>The size in bytes of the file, or nil on failure.</description></item>
        /// <item><term>errormsg</term><description>The error message on failure.</description></item>
        /// </list>
        /// </returns>
    public (double size, string errormsg) GetSize(string filename) => throw new NotImplementedException();

/// <summary>
        /// <para>Returns the full path to the the .love file or directory. If the game is fused to the LÖVE executable, then the executable is returned.</para>
        /// </summary>
        /// <param name="path">The full platform-dependent path of the .love file or directory.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>path</term><description>The full platform-dependent path of the .love file or directory.</description></item>
        /// </list>
        /// </returns>
    public string GetSource(string path) => throw new NotImplementedException();

/// <summary>
        /// <para>Returns the full path to the directory containing the .love file. If the game is fused to the LÖVE executable, then the directory containing the executable is returned.</para>
        /// <para>If love.filesystem.isFused is true, the path returned by this function can be passed to love.filesystem.mount, which will make the directory containing the main game (e.g. C:\Program Files\coolgame\) readable by love.filesystem.</para>
        /// </summary>
        /// <param name="path">The full platform-dependent path of the directory containing the .love file.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>path</term><description>The full platform-dependent path of the directory containing the .love file.</description></item>
        /// </list>
        /// </returns>
    public string GetSourceBaseDirectory(string path) => throw new NotImplementedException();

/// <summary>
        /// <para>Returns the full path to the directory containing the .love file. If the game is fused to the LÖVE executable, then the directory containing the executable is returned.</para>
        /// <para>If love.filesystem.isFused is true, the path returned by this function can be passed to love.filesystem.mount, which will make the directory containing the main game (e.g. C:\Program Files\coolgame\) readable by love.filesystem.</para>
        /// </summary>
    public void IsFused() => throw new NotImplementedException();

/// <summary>
        /// <para>Returns the path of the user's directory</para>
        /// </summary>
        /// <param name="path">The path of the user's directory</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>path</term><description>The path of the user's directory</description></item>
        /// </list>
        /// </returns>
    public string GetUserDirectory(string path) => throw new NotImplementedException();

/// <summary>
        /// <para>Gets the current working directory.</para>
        /// </summary>
        /// <param name="cwd">The current working directory.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>cwd</term><description>The current working directory.</description></item>
        /// </list>
        /// </returns>
    public string GetWorkingDirectory(string cwd) => throw new NotImplementedException();

/// <summary>
        /// <para>Initializes love.filesystem, will be called internally, so should not be used explicitly.</para>
        /// </summary>
        /// <param name="appname">The name of the application binary, typically .</param>
    public void Init(string appname) => throw new NotImplementedException();

/// <summary>
        /// <para>Check whether something is a directory.</para>
        /// </summary>
        /// <param name="filename">The path to a potential directory.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>isDir</term><description>True if there is a directory with the specified name. False otherwise.</description></item>
        /// </list>
        /// </returns>
    public bool IsDirectory(string filename) => throw new NotImplementedException();

/// <summary>
        /// <para>Check whether something is a file.</para>
        /// </summary>
        /// <param name="filename">The path to a potential file.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>isFile</term><description>True if there is a file with the specified name. False otherwise.</description></item>
        /// </list>
        /// </returns>
    public bool IsFile(string filename) => throw new NotImplementedException();

/// <summary>
        /// <para>Gets whether the game is in fused mode or not.</para>
        /// <para>If a game is in fused mode, its save directory will be directly in the Appdata directory instead of Appdata/LOVE/. The game will also be able to load C Lua dynamic libraries which are located in the save directory.</para>
        /// <para>A game is in fused mode if the source .love has been fused to the executable (see Game Distribution), or if "--fused" has been given as a command-line argument when starting the game.</para>
        /// </summary>
        /// <param name="fused">True if the game is in fused mode, false otherwise.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>fused</term><description>True if the game is in fused mode, false otherwise.</description></item>
        /// </list>
        /// </returns>
    public bool IsFused(bool fused) => throw new NotImplementedException();

/// <summary>
        /// <para>Gets whether a filepath is actually a symbolic link.</para>
        /// <para>If symbolic links are not enabled (via love.filesystem.setSymlinksEnabled), this function will always return false.</para>
        /// </summary>
        /// <param name="path">The file or directory path to check.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>symlink</term><description>True if the path is a symbolic link, false otherwise.</description></item>
        /// </list>
        /// </returns>
    public bool IsSymlink(string path) => throw new NotImplementedException();

/// <summary>
        /// <para>Iterate over the lines in a file.</para>
        /// </summary>
        /// <param name="name">The name (and path) of the file</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>iterator</term><description>A function that iterates over all the lines in the file, returning the line with newlines stripped (if the line ends with , both are stripped independently of the OS)</description></item>
        /// </list>
        /// </returns>
    public object Lines(string name) => throw new NotImplementedException();

/// <summary>
        /// <para>Iterate over the lines in a file.</para>
        /// </summary>
    public void Lines() => throw new NotImplementedException();

/// <summary>
        /// <para>Loads a Lua file (but does not run it).</para>
        /// <para>This is equivalent to loadfile except it operates on LÖVE filesystem paths.</para>
        /// </summary>
        /// <param name="name">The name (and path) of the file.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>chunk</term><description>The loaded chunk.</description></item>
        /// <item><term>errormsg</term><description>The error message if file could not be opened.</description></item>
        /// </list>
        /// </returns>
    public (object chunk, string errormsg) Load(string name) => throw new NotImplementedException();

/// <summary>
        /// <para>Mounts a zip file or folder in the game's save directory for reading.</para>
        /// <para>It is also possible to mount love.filesystem.getSourceBaseDirectory if the game is in fused mode.</para>
        /// </summary>
        /// <param name="archive">The folder or zip file in the game's save directory to mount.</param>
        /// <param name="mountpoint">The new path the archive will be mounted to.</param>
        /// <param name="appendToPath">Whether the archive will be searched when reading a filepath before or after already-mounted archives. This includes the game's source and save directories.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>success</term><description>True if the archive was successfully mounted, false otherwise.</description></item>
        /// </list>
        /// </returns>
    public bool Mount(string archive, string mountpoint, bool appendToPath = false) => throw new NotImplementedException();

/// <summary>
        /// <para>Mounts a zip file or folder in the game's save directory for reading.</para>
        /// <para>It is also possible to mount love.filesystem.getSourceBaseDirectory if the game is in fused mode.</para>
        /// </summary>
        /// <param name="filedata">The FileData object in memory to mount.</param>
        /// <param name="mountpoint">The new path the archive will be mounted to.</param>
        /// <param name="appendToPath">Whether the archive will be searched when reading a filepath before or after already-mounted archives. This includes the game's source and save directories.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>success</term><description>True if the archive was successfully mounted, false otherwise.</description></item>
        /// </list>
        /// </returns>
    public bool Mount(object filedata, string mountpoint, bool appendToPath = false) => throw new NotImplementedException();

/// <summary>
        /// <para>Mounts a zip file or folder in the game's save directory for reading.</para>
        /// <para>It is also possible to mount love.filesystem.getSourceBaseDirectory if the game is in fused mode.</para>
        /// </summary>
        /// <param name="data">The Data object in memory to mount.</param>
        /// <param name="archivename">The name to associate the mounted data with, for use with . Must be unique compared to other mounted data.</param>
        /// <param name="mountpoint">The new path the archive will be mounted to.</param>
        /// <param name="appendToPath">Whether the archive will be searched when reading a filepath before or after already-mounted archives. This includes the game's source and save directories.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>success</term><description>True if the archive was successfully mounted, false otherwise.</description></item>
        /// </list>
        /// </returns>
    public bool Mount(object data, string archivename, string mountpoint, bool appendToPath = false) => throw new NotImplementedException();

/// <summary>
        /// <para>Creates a new File object.</para>
        /// <para>It needs to be opened before it can be accessed.</para>
        /// </summary>
        /// <param name="filename">The filename of the file.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>file</term><description>The new File object.</description></item>
        /// </list>
        /// </returns>
    public object NewFile(string filename) => throw new NotImplementedException();

/// <summary>
        /// <para>Creates a new File object.</para>
        /// <para>It needs to be opened before it can be accessed.</para>
        /// </summary>
    public void NewFile() => throw new NotImplementedException();

/// <summary>
        /// <para>Creates a new File object.</para>
        /// <para>It needs to be opened before it can be accessed.</para>
        /// </summary>
        /// <param name="filename">The filename of the file.</param>
        /// <param name="mode">The mode to open the file in.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>file</term><description>The new File object, or nil if an error occurred.</description></item>
        /// <item><term>errorstr</term><description>The error string if an error occurred.</description></item>
        /// </list>
        /// </returns>
    public (object file, string errorstr) NewFile(string filename, FileMode mode) => throw new NotImplementedException();

    /// <summary>
    /// <para>Read the contents of a file.</para>
    /// </summary>
    /// <param name="name">The name (and path) of the file.</param>
    /// <param name="size">How many bytes to read.</param>
    /// <returns>
    /// <list type="bullet">
    /// <item><term>contents</term><description>The file contents.</description></item>
    /// <item><term>size</term><description>How many bytes have been read.</description></item>
    /// </list>
    /// </returns>
    /// TODO: Is MaxValue the right default for size here?
    public (string contents, double size) Read(string name, double size = Double.MaxValue) => throw new NotImplementedException();

    /// <summary>
    /// <para>Read the contents of a file.</para>
    /// </summary>
    /// <param name="container">What type to return the file's contents as.</param>
    /// <param name="name">The name (and path) of the file</param>
    /// <param name="size">How many bytes to read</param>
    /// <returns>
    /// <list type="bullet">
    /// <item><term>contents</term><description>or string containing the file contents.</description></item>
    /// <item><term>size</term><description>How many bytes have been read.</description></item>
    /// </list>
    /// </returns>
    /// TODO: Is MaxValue the right default for size here?
    public (object contents, double size) Read(object container, string name, double size = double.MaxValue) => throw new NotImplementedException();

/// <summary>
        /// <para>Removes a file or empty directory.</para>
        /// </summary>
        /// <param name="name">The file or directory to remove.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>success</term><description>True if the file/directory was removed, false otherwise.</description></item>
        /// </list>
        /// </returns>
    public bool Remove(string name) => throw new NotImplementedException();

/// <summary>
        /// <para>Removes a file or empty directory.</para>
        /// </summary>
    public void CreateDirectory() => throw new NotImplementedException();

/// <summary>
        /// <para>Sets the filesystem paths that will be searched for c libraries when require is called.</para>
        /// <para>The paths string returned by this function is a sequence of path templates separated by semicolons. The argument passed to require will be inserted in place of any question mark ("?") character in each template (after the dot characters in the argument passed to require are replaced by directory separators.) Additionally, any occurrence of a double question mark ("??") will be replaced by the name passed to require and the default library extension for the platform.</para>
        /// <para>The paths are relative to the game's source and save directories, as well as any paths mounted with love.filesystem.mount.</para>
        /// </summary>
        /// <param name="paths">The paths that the function will check in love's filesystem.</param>
    public void SetCRequirePath(string paths) => throw new NotImplementedException();

/// <summary>
        /// <para>Sets the write directory for your game. Equivalent to the identity/appendidentity pair for conf.lua.</para>
        /// <para>Note that you can only set the name of the folder to store your files in, not the location.</para>
        /// </summary>
        /// <param name="name">The new identity that will be used as write directory.</param>
    public void SetIdentity(string name) => throw new NotImplementedException();

/// <summary>
        /// <para>Sets the write directory for your game. Equivalent to the identity/appendidentity pair for conf.lua.</para>
        /// <para>Note that you can only set the name of the folder to store your files in, not the location.</para>
        /// </summary>
    public void SetIdentity() => throw new NotImplementedException();

/// <summary>
        /// <para>Sets the filesystem paths that will be searched when require is called.</para>
        /// <para>The paths string given to this function is a sequence of path templates separated by semicolons. The argument passed to require will be inserted in place of any question mark ("?") character in each template (after the dot characters in the argument passed to require are replaced by directory separators.)</para>
        /// <para>The paths are relative to the game's source and save directories, as well as any paths mounted with love.filesystem.mount.</para>
        /// </summary>
        /// <param name="paths">The paths that the function will check in love's filesystem.</param>
    public void SetRequirePath(string paths) => throw new NotImplementedException();

/// <summary>
        /// <para>Sets the source of the game, where the code is present. This function can only be called once, and is normally automatically done by LÖVE.</para>
        /// </summary>
        /// <param name="path">Absolute path to the game's source folder.</param>
    public void SetSource(string path) => throw new NotImplementedException();

/// <summary>
        /// <para>Sets whether love.filesystem follows symbolic links. It is enabled by default in version 0.10.0 and newer, and disabled by default in 0.9.2.</para>
        /// </summary>
        /// <param name="enable">Whether love.filesystem should follow symbolic links.</param>
    public void SetSymlinksEnabled(bool enable) => throw new NotImplementedException();

/// <summary>
        /// <para>Unmounts a zip file or folder previously mounted for reading with love.filesystem.mount.</para>
        /// </summary>
        /// <param name="archive">The folder or zip file in the game's save directory which is currently mounted.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>success</term><description>True if the archive was successfully unmounted, false otherwise.</description></item>
        /// </list>
        /// </returns>
    public bool Unmount(string archive) => throw new NotImplementedException();

/// <summary>
        /// <para>Unmounts a zip file or folder previously mounted for reading with love.filesystem.mount.</para>
        /// </summary>
    public void Mount() => throw new NotImplementedException();

    /// <summary>
    /// <para>Write data to a file in the save directory. If the file existed already, it will be completely replaced by the new contents.</para>
    /// </summary>
    /// <param name="name">The name (and path) of the file.</param>
    /// <param name="data">The string data to write to the file.</param>
    /// <param name="size">How many bytes to write.</param>
    /// <returns>
    /// <list type="bullet">
    /// <item><term>success</term><description>If the operation was successful.</description></item>
    /// <item><term>message</term><description>Error message if operation was unsuccessful.</description></item>
    /// </list>
    /// </returns>
    /// TODO: Is MaxValue the right default for size here?
    public (bool success, string message) Write(string name, string data, double size = double.MaxValue) => throw new NotImplementedException();

    /// <summary>
    /// <para>Write data to a file in the save directory. If the file existed already, it will be completely replaced by the new contents.</para>
    /// </summary>
    /// <param name="name">The name (and path) of the file.</param>
    /// <param name="data">The Data object to write to the file.</param>
    /// <param name="size">How many bytes to write.</param>
    /// <returns>
    /// <list type="bullet">
    /// <item><term>success</term><description>If the operation was successful.</description></item>
    /// <item><term>message</term><description>Error message if operation was unsuccessful.</description></item>
    /// </list>
    /// </returns>
    /// TODO: Is MaxValue the right default for size here?
    public (bool success, string message) Write(string name, object data, double size = double.MaxValue) => throw new NotImplementedException();

/// <summary>
        /// <para>Write data to a file in the save directory. If the file existed already, it will be completely replaced by the new contents.</para>
        /// </summary>
    public void Write() => throw new NotImplementedException();

}
