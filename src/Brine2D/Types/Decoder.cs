namespace Brine2D
{
    /// <summary>
    /// <para>An object which can gradually decode a sound file.</para>
    /// </summary>
    // TODO: Requires Review
    public class Decoder
    {
        /// <summary>
        /// <para>Creates a new copy of current decoder.</para>
        /// <para>The new decoder will start decoding from the beginning of the audio stream.</para>
        /// </summary>
        /// <param name="decoder">New copy of the decoder.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>decoder</term><description>New copy of the decoder.</description></item>
        /// </list>
        /// </returns>
        public object Clone(object decoder) => throw new NotImplementedException();
        /// <summary>
        /// <para>Decodes the audio and returns a SoundData object containing the decoded audio data.</para>
        /// </summary>
        /// <param name="soundData">Decoded audio data or nil if the decoder reached the end of the file.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>soundData</term><description>Decoded audio data or nil if the decoder reached the end of the file.</description></item>
        /// </list>
        /// </returns>
        public object Decode(object soundData = null) => throw new NotImplementedException();
        /// <summary>
        /// <para>Returns the number of bits per sample.</para>
        /// </summary>
        /// <param name="bitDepth">Either 8, or 16.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>bitDepth</term><description>Either 8, or 16.</description></item>
        /// </list>
        /// </returns>
        public double GetBitDepth(double bitDepth) => throw new NotImplementedException();
        /// <summary>
        /// <para>Returns the number of channels in the stream.</para>
        /// </summary>
        /// <param name="channels">1 for mono, 2 for stereo.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>channels</term><description>1 for mono, 2 for stereo.</description></item>
        /// </list>
        /// </returns>
        public double GetChannelCount(double channels) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the duration of the sound file. It may not always be sample-accurate, and it may return -1 if the duration cannot be determined at all.</para>
        /// </summary>
        /// <param name="duration">The duration of the sound file in seconds, or -1 if it cannot be determined.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>duration</term><description>The duration of the sound file in seconds, or -1 if it cannot be determined.</description></item>
        /// </list>
        /// </returns>
        public double GetDuration(double duration) => throw new NotImplementedException();
        /// <summary>
        /// <para>Returns the sample rate of the Decoder.</para>
        /// </summary>
        /// <param name="rate">Number of samples per second.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>rate</term><description>Number of samples per second.</description></item>
        /// </list>
        /// </returns>
        public double GetSampleRate(double rate) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the currently playing position of the Decoder.</para>
        /// </summary>
        /// <param name="offset">The position to seek to, in seconds.</param>
        public void Seek(double offset) => throw new NotImplementedException();
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
