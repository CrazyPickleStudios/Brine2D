namespace Brine2D
{
    /// <summary>
    /// <para>Contains raw audio samples.</para>
/// <para>You can not play SoundData back directly. You must wrap a Source object around it.</para>
    /// </summary>
    // TODO: Requires Review
    public class SoundData
    {
        /// <summary>
        /// <para>Creates a new copy of the Data object.</para>
        /// </summary>
        /// <param name="clone">The new copy.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>clone</term><description>The new copy.</description></item>
        /// </list>
        /// </returns>
        public object Clone(object clone) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets an FFI pointer to the Data.</para>
        /// <para>This function should be preferred instead of Data:getPointer because the latter uses</para>
        /// <para>light userdata which can't store more all possible memory addresses on some new ARM64</para>
        /// <para>architectures, when LuaJIT is used.</para>
        /// </summary>
        /// <param name="pointer">A raw pointer to the Data, or if FFI is unavailable.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>pointer</term><description>A raw pointer to the Data, or if FFI is unavailable.</description></item>
        /// </list>
        /// </returns>
        public object GetFFIPointer(object pointer) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets a pointer to the Data. Can be used with libraries such as LuaJIT's FFI.</para>
        /// </summary>
        /// <param name="userdata">A raw pointer to the Data.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>userdata</term><description>A raw pointer to the Data.</description></item>
        /// </list>
        /// </returns>
        public object GetPointer(object userdata) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the Data's size in bytes.</para>
        /// </summary>
        /// <param name="size">The size of the Data in bytes.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>size</term><description>The size of the Data in bytes.</description></item>
        /// </list>
        /// </returns>
        public double GetSize(double size) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the full Data as a string.</para>
        /// </summary>
        /// <param name="data">The raw data.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>data</term><description>The raw data.</description></item>
        /// </list>
        /// </returns>
        public string GetString(string data) => throw new NotImplementedException();
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
        /// <summary>
        /// <para>Returns the number of bits per sample.</para>
        /// </summary>
        /// <param name="bitdepth">Either 8, or 16.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>bitdepth</term><description>Either 8, or 16.</description></item>
        /// </list>
        /// </returns>
        public double GetBitDepth(double bitdepth) => throw new NotImplementedException();
        /// <summary>
        /// <para>Returns the number of channels in the SoundData.</para>
        /// </summary>
        /// <param name="channels">1 for mono, 2 for stereo.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>channels</term><description>1 for mono, 2 for stereo.</description></item>
        /// </list>
        /// </returns>
        public double GetChannelCount(double channels) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the duration of the sound data.</para>
        /// </summary>
        /// <param name="duration">The duration of the sound data in seconds.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>duration</term><description>The duration of the sound data in seconds.</description></item>
        /// </list>
        /// </returns>
        public double GetDuration(double duration) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the value of the sample-point at the specified position. For stereo SoundData objects, the data from the left and right channels are interleaved in that order.</para>
        /// </summary>
        /// <param name="i">An integer value specifying the position of the sample (starting at 0).</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>sample</term><description>The normalized samplepoint (range -1.0 to 1.0).</description></item>
        /// </list>
        /// </returns>
        public double GetSample(double i) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the value of the sample-point at the specified position. For stereo SoundData objects, the data from the left and right channels are interleaved in that order.</para>
        /// </summary>
        /// <param name="i">An integer value specifying the position of the sample (starting at 0).</param>
        /// <param name="channel">The index of the channel to get within the given sample.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>sample</term><description>The normalized samplepoint (range -1.0 to 1.0).</description></item>
        /// </list>
        /// </returns>
        public double GetSample(double i, double channel) => throw new NotImplementedException();
        /// <summary>
        /// <para>Returns the number of samples per channel of the SoundData.</para>
        /// </summary>
        /// <param name="count">Total number of samples.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>count</term><description>Total number of samples.</description></item>
        /// </list>
        /// </returns>
        public double GetSampleCount(double count) => throw new NotImplementedException();
        /// <summary>
        /// <para>Returns the sample rate of the SoundData.</para>
        /// </summary>
        /// <param name="rate">Number of samples per second.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>rate</term><description>Number of samples per second.</description></item>
        /// </list>
        /// </returns>
        public double GetSampleRate(double rate) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the value of the sample-point at the specified position. For stereo SoundData objects, the data from the left and right channels are interleaved in that order.</para>
        /// </summary>
        /// <param name="i">An integer value specifying the position of the sample (starting at 0).</param>
        /// <param name="sample">The normalized samplepoint (range -1.0 to 1.0).</param>
        public void SetSample(double i, double sample) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the value of the sample-point at the specified position. For stereo SoundData objects, the data from the left and right channels are interleaved in that order.</para>
        /// </summary>
        /// <param name="i">An integer value specifying the position of the sample (starting at 0).</param>
        /// <param name="channel">The index of the channel to set within the given sample (starting at 1). For stereo, 1 is left channel and 2 is right channel.</param>
        /// <param name="sample">The normalized samplepoint (range -1.0 to 1.0).</param>
        public void SetSample(double i, double channel, double sample) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the value of the sample-point at the specified position. For stereo SoundData objects, the data from the left and right channels are interleaved in that order.</para>
        /// </summary>
        public void NewQueueableSource() => throw new NotImplementedException();
    }
}
