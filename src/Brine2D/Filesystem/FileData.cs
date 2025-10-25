namespace Brine2D.Filesystem;

/// <summary>
///     Data representing the contents of a file.
/// </summary>
public class FileData : DataObject
{
    private readonly string _extension;
    private readonly string _filename;
    private readonly string _name;
    private readonly long _size;
    private char[]? _data;

    internal FileData(long size, string filename)
    {
        _data = null;
        this._size = size;
        this._filename = filename;

        try
        {
            _data = new char[size];
        }
        catch (OutOfMemoryException ex)
        {
            // TODO: Why? -RP
            throw new OutOfMemoryException("Out of memory.", ex);
        }

        var dotPos = filename.IndexOf('.');

        if (dotPos != -1)
        {
            _extension = filename[(dotPos + 1)..];
            _name = filename[..dotPos];
        }
        else
        {
            _name = filename;
        }
    }

    internal FileData(FileData c)
    {
        _data = null;
        _size = c._size;
        _filename = c._filename;
        _extension = c._extension;
        _name = c._name;

        try
        {
            _data = new char[_size];
        }
        catch (OutOfMemoryException ex)
        {
            // TODO: Why? -RP
            throw new OutOfMemoryException("Out of memory.", ex);
        }

        Array.Copy(c._data!, _data, _size);
    }

    /// <summary>
    ///     Creates a new copy of the Data object.
    /// </summary>
    /// <returns>
    ///     The new copy.
    /// </returns>
    public override FileData Clone()
    {
        return new FileData(this);
    }

    /// <summary>
    ///     Gets the extension of the FileData.
    /// </summary>
    /// <returns>
    ///     The extension of the file the FileData represents.
    /// </returns>
    public string GetExtension()
    {
        return _extension;
    }

    /// <summary>
    ///     <para>Gets an FFI pointer to the Data.</para>
    ///     <para>This function should be preferred instead of Data:getPointer because the latter uses</para>
    ///     <para>light userdata which can't store more all possible memory addresses on some new ARM64</para>
    ///     <para>architectures, when LuaJIT is used.</para>
    /// </summary>
    /// <returns>
    ///     A raw pointer to the Data, or if FFI is unavailable.
    /// </returns>
    public object GetFFIPointer()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     Gets the filename of the FileData.
    /// </summary>
    /// <returns>
    ///     The name of the file the FileData represents.
    /// </returns>
    public string GetFilename()
    {
        return _filename;
    }

    /// <summary>
    ///     Gets a pointer to the Data. Can be used with libraries such as LuaJIT's FFI.
    /// </summary>
    /// <param name="userdata">A raw pointer to the Data.</param>
    /// <returns>
    ///     A raw pointer to the Data.
    /// </returns>
    public object GetPointer(object userdata)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     Gets the Data's size in bytes.
    /// </summary>
    /// <returns>
    ///     The size of the Data in bytes.
    /// </returns>
    public override long GetSize()
    {
        return _size;
    }

    /// <summary>
    ///     Gets the full Data as a string.
    /// </summary>
    /// <returns>
    ///     The raw data.
    /// </returns>
    public override string GetString(int offset = 0, int? size = null)
    {
        return new string(_data, offset, (int)(size ?? this._size - offset));
    }

    /// <inheritdoc />
    public override bool Release()
    {
        if (_data != null)
        {
            Array.Clear(_data);
            _data = null;
        }

        return base.Release();
    }
}