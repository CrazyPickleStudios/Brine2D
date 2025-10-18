namespace Brine2D
{
    /// <summary>
    /// <para>Represents an audio input device capable of recording sounds.</para>
    /// </summary>
    // TODO: Requires Review
    public class RecordingDevice
    {
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
        /// <para>Gets the number of bits per sample in the data currently being recorded.</para>
        /// </summary>
        /// <param name="bits">The number of bits per sample in the data that's currently being recorded.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>bits</term><description>The number of bits per sample in the data that's currently being recorded.</description></item>
        /// </list>
        /// </returns>
        public double GetBitDepth(double bits) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the number of channels currently being recorded (mono or stereo).</para>
        /// </summary>
        /// <param name="channels">The number of channels being recorded (1 for mono, 2 for stereo).</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>channels</term><description>The number of channels being recorded (1 for mono, 2 for stereo).</description></item>
        /// </list>
        /// </returns>
        public double GetChannelCount(double channels) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets all recorded audio SoundData stored in the device's internal ring buffer.</para>
        /// <para>The internal ring buffer is cleared when this function is called, so calling it again will only get audio recorded after the previous call. If the device's internal ring buffer completely fills up before this function is called, the oldest data that doesn't fit into the buffer will be lost.</para>
        /// </summary>
        /// <param name="data">The recorded audio data, or nil if the device isn't recording.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>data</term><description>The recorded audio data, or nil if the device isn't recording.</description></item>
        /// </list>
        /// </returns>
        public object GetData(object data = null) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the name of the recording device.</para>
        /// </summary>
        /// <param name="name">The name of the device.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>name</term><description>The name of the device.</description></item>
        /// </list>
        /// </returns>
        public string GetName(string name) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the number of currently recorded samples.</para>
        /// </summary>
        /// <param name="samples">The number of samples that have been recorded so far.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>samples</term><description>The number of samples that have been recorded so far.</description></item>
        /// </list>
        /// </returns>
        public double GetSampleCount(double samples) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the number of samples per second currently being recorded.</para>
        /// </summary>
        /// <param name="rate">The number of samples being recorded per second (sample rate).</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>rate</term><description>The number of samples being recorded per second (sample rate).</description></item>
        /// </list>
        /// </returns>
        public double GetSampleRate(double rate) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets whether the device is currently recording.</para>
        /// </summary>
        /// <param name="recording">True if the is , false otherwise.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>recording</term><description>True if the is , false otherwise.</description></item>
        /// </list>
        /// </returns>
        public bool IsRecording(bool recording) => throw new NotImplementedException();
        /// <summary>
        /// <para>Begins recording audio using this device.</para>
        /// </summary>
        /// <param name="samplecount">The maximum number of samples to store in an internal ring buffer when recording. clears the internal buffer when called.</param>
        /// <param name="samplerate">The number of samples per second to store when recording.</param>
        /// <param name="bitdepth">The number of bits per sample.</param>
        /// <param name="channels">Whether to record in mono or stereo. Most microphones don't support more than 1 channel.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>success</term><description>True if the device successfully began recording using the specified parameters, false if not.</description></item>
        /// </list>
        /// </returns>
        public bool Start(double samplecount, double samplerate = 8000, double bitdepth = 16, double channels = 1) => throw new NotImplementedException();
        /// <summary>
        /// <para>Stops recording audio from this device. Any sound data currently in the device's buffer will be returned.</para>
        /// </summary>
        /// <param name="data">The sound data currently in the device's buffer, or nil if the device wasn't recording.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>data</term><description>The sound data currently in the device's buffer, or nil if the device wasn't recording.</description></item>
        /// </list>
        /// </returns>
        public object Stop(object data = null) => throw new NotImplementedException();
    }
}
