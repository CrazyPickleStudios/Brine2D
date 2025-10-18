namespace Brine2D;

// TODO: Needs review
public sealed class DataModule
{
/// <summary>
        /// <para>Compresses a string or data using a specific compression algorithm.</para>
        /// </summary>
        /// <param name="container">What type to return the compressed data as.</param>
        /// <param name="format">The format to use when compressing the string.</param>
        /// <param name="rawstring">The raw (un-compressed) string to compress.</param>
        /// <param name="level">The level of compression to use, between 0 and 9. -1 indicates the default level. The meaning of this argument depends on the compression format being used.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>compressedData</term><description>/ which contains the compressed version of rawstring.</description></item>
        /// </list>
        /// </returns>
    public object Compress(ContainerType container, CompressedDataFormat format, string rawstring, double level = -1) => throw new NotImplementedException();

/// <summary>
        /// <para>Compresses a string or data using a specific compression algorithm.</para>
        /// </summary>
        /// <param name="container">What type to return the compressed data as.</param>
        /// <param name="format">The format to use when compressing the data.</param>
        /// <param name="data">A Data object containing the raw (un-compressed) data to compress.</param>
        /// <param name="level">The level of compression to use, between 0 and 9. -1 indicates the default level. The meaning of this argument depends on the compression format being used.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>compressedData</term><description>/ which contains the compressed version of data.</description></item>
        /// </list>
        /// </returns>
    public object Compress(ContainerType container, CompressedDataFormat format, object data, double level = -1) => throw new NotImplementedException();

/// <summary>
        /// <para>Decode Data or a string from any of the EncodeFormats to Data or string.</para>
        /// </summary>
        /// <param name="container">What type to return the decoded data as.</param>
        /// <param name="format">The format of the input data.</param>
        /// <param name="sourceString">The raw (encoded) data to decode.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>decoded</term><description>/ which contains the decoded version of source.</description></item>
        /// </list>
        /// </returns>
    public object Decode(ContainerType container, EncodeFormat format, string sourceString) => throw new NotImplementedException();

/// <summary>
        /// <para>Decode Data or a string from any of the EncodeFormats to Data or string.</para>
        /// </summary>
        /// <param name="container">What type to return the decoded data as.</param>
        /// <param name="format">The format of the input data.</param>
        /// <param name="sourceData">The raw (encoded) data to decode.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>decoded</term><description>/ which contains the decoded version of source.</description></item>
        /// </list>
        /// </returns>
    public object Decode(ContainerType container, EncodeFormat format, object sourceData) => throw new NotImplementedException();

/// <summary>
        /// <para>Decompresses a CompressedData or previously compressed string or Data object.</para>
        /// </summary>
        /// <param name="container">What type to return the decompressed data as.</param>
        /// <param name="compressedData">The compressed data to decompress.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>decompressedData</term><description>/ containing the raw decompressed data.</description></item>
        /// </list>
        /// </returns>
    public object Decompress(ContainerType container, object compressedData) => throw new NotImplementedException();

/// <summary>
        /// <para>Decompresses a CompressedData or previously compressed string or Data object.</para>
        /// </summary>
        /// <param name="container">What type to return the decompressed data as.</param>
        /// <param name="format">The format that was used to compress the given string.</param>
        /// <param name="compressedString">A string containing data previously compressed with .</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>decompressedData</term><description>/ containing the raw decompressed data.</description></item>
        /// </list>
        /// </returns>
    public object Decompress(ContainerType container, CompressedDataFormat format, string compressedString) => throw new NotImplementedException();

/// <summary>
        /// <para>Decompresses a CompressedData or previously compressed string or Data object.</para>
        /// </summary>
        /// <param name="container">What type to return the decompressed data as.</param>
        /// <param name="format">The format that was used to compress the given data.</param>
        /// <param name="data">A Data object containing data previously compressed with .</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>decompressedData</term><description>/ containing the raw decompressed data.</description></item>
        /// </list>
        /// </returns>
    public object Decompress(ContainerType container, CompressedDataFormat format, object data) => throw new NotImplementedException();

/// <summary>
        /// <para>Encode Data or a string to a Data or string in one of the EncodeFormats.</para>
        /// </summary>
        /// <param name="container">What type to return the encoded data as.</param>
        /// <param name="format">The format of the output data.</param>
        /// <param name="sourceString">The raw data to encode.</param>
        /// <param name="linelength">The maximum line length of the output. Only supported for base64, ignored if 0.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>encoded</term><description>/ which contains the encoded version of source.</description></item>
        /// </list>
        /// </returns>
    public object Encode(ContainerType container, EncodeFormat format, string sourceString, double linelength = 0) => throw new NotImplementedException();

/// <summary>
        /// <para>Encode Data or a string to a Data or string in one of the EncodeFormats.</para>
        /// </summary>
        /// <param name="container">What type to return the encoded data as.</param>
        /// <param name="format">The format of the output data.</param>
        /// <param name="sourceData">The raw data to encode.</param>
        /// <param name="linelength">The maximum line length of the output. Only supported for base64, ignored if 0.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>encoded</term><description>/ which contains the encoded version of source.</description></item>
        /// </list>
        /// </returns>
    public object Encode(ContainerType container, EncodeFormat format, object sourceData, double linelength = 0) => throw new NotImplementedException();

/// <summary>
        /// <para>Gets the size in bytes that a given format used with love.data.pack will use.</para>
        /// <para>This function behaves the same as Lua 5.3's string.packsize.</para>
        /// </summary>
        /// <param name="format">A string determining how the values are packed. Follows the rules of .</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>size</term><description>The size in bytes that the packed data will use.</description></item>
        /// </list>
        /// </returns>
    public double GetPackedSize(string format) => throw new NotImplementedException();

/// <summary>
        /// <para>Packs (serializes) simple Lua values.</para>
        /// <para>This function behaves the same as Lua 5.3's string.pack.</para>
        /// </summary>
        /// <param name="container">What type to return the encoded data as.</param>
        /// <param name="format">A string determining how the values are packed. Follows the rules of .</param>
        /// <param name="v1">The first value (number, boolean, or string) to serialize.</param>
        /// <param name="">Additional values to serialize.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>data</term><description>/ which contains the serialized data.</description></item>
        /// </list>
        /// </returns>
        /// TODO: What best matches value ...?
    public object Pack(ContainerType container, string format, object v1, object values) => throw new NotImplementedException();

/// <summary>
        /// <para>Compute the message digest of a string using a specified hash algorithm.</para>
        /// </summary>
        /// <param name="hashFunction">Hash algorithm to use.</param>
        /// <param name="string">String to hash.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>rawdigest</term><description>Raw message digest string.</description></item>
        /// </list>
        /// </returns>
    public string Hash(HashFunction hashFunction, string str) => throw new NotImplementedException();

/// <summary>
        /// <para>Compute the message digest of a string using a specified hash algorithm.</para>
        /// </summary>
        /// <param name="hashFunction">Hash algorithm to use.</param>
        /// <param name="data">Data to hash.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>rawdigest</term><description>Raw message digest string.</description></item>
        /// </list>
        /// </returns>
    public string Hash(HashFunction hashFunction, object data) => throw new NotImplementedException();

/// <summary>
        /// <para>Compute the message digest of a string using a specified hash algorithm.</para>
        /// </summary>
    public void Encode() => throw new NotImplementedException();

/// <summary>
        /// <para>Creates a new Data object containing arbitrary bytes.</para>
        /// <para>Data:getPointer along with LuaJIT's FFI can be used to manipulate the contents of the ByteData object after it has been created.</para>
        /// </summary>
        /// <param name="datastring">The byte string to copy.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>bytedata</term><description>The new Data object.</description></item>
        /// </list>
        /// </returns>
    public object NewByteData(string datastring) => throw new NotImplementedException();

/// <summary>
        /// <para>Creates a new Data object containing arbitrary bytes.</para>
        /// <para>Data:getPointer along with LuaJIT's FFI can be used to manipulate the contents of the ByteData object after it has been created.</para>
        /// </summary>
        /// <param name="size">The size in bytes of the new Data object.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>bytedata</term><description>The new Data object.</description></item>
        /// </list>
        /// </returns>
    public object NewByteData(double size) => throw new NotImplementedException();

/// <summary>
        /// <para>Creates a new Data referencing a subsection of an existing Data object.</para>
        /// </summary>
        /// <param name="data">The Data object to reference.</param>
        /// <param name="offset">The offset of the subsection to reference, in bytes.</param>
        /// <param name="size">The size in bytes of the subsection to reference.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>view</term><description>The new Data view.</description></item>
        /// </list>
        /// </returns>
    public object NewDataView(object data, double offset, double size) => throw new NotImplementedException();

/// <summary>
        /// <para>Unpacks (deserializes) a byte-string or Data into simple Lua values.</para>
        /// <para>This function behaves the same as Lua 5.3's string.unpack.</para>
        /// </summary>
        /// <param name="format">A string determining how the values were packed. Follows the rules of .</param>
        /// <param name="datastring">A string containing the packed (serialized) data.</param>
        /// <param name="pos">Where to start reading in the string. Negative values can be used to read relative from the end of the string.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>v1</term><description>The first value (number, boolean, or string) that was unpacked.</description></item>
        /// <item><term></term><description>Additional unpacked values.</description></item>
        /// <item><term>index</term><description>The index of the first unread byte in the data string.</description></item>
        /// </list>
        /// </returns>
    public (object v1, object, double index) Unpack(string format, string datastring, double pos = 1) => throw new NotImplementedException();

/// <summary>
        /// <para>Unpacks (deserializes) a byte-string or Data into simple Lua values.</para>
        /// <para>This function behaves the same as Lua 5.3's string.unpack.</para>
        /// </summary>
        /// <param name="format">A string determining how the values were packed. Follows the rules of .</param>
        /// <param name="data">A Data object containing the packed (serialized) data.</param>
        /// <param name="pos">1-based index indicating where to start reading in the Data. Negative values can be used to read relative from the end of the Data object.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>v1</term><description>The first value (number, boolean, or string) that was unpacked.</description></item>
        /// <item><term></term><description>Additional unpacked values.</description></item>
        /// <item><term>index</term><description>The 1-based index of the first unread byte in the Data.</description></item>
        /// </list>
        /// </returns>
    public (object v1, object, double index) Unpack(string format, object data, double pos = 1) => throw new NotImplementedException();

}
