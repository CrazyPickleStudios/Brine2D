namespace Brine2D.Video
{
    /// <summary>
    /// <para>A drawable video.</para>
    /// </summary>
    // TODO: Requires Review
    public class Video
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
        /// <para>Gets the width and height of the Video in pixels.</para>
        /// </summary>
        /// <param name="width">The width of the Video.</param>
        /// <param name="height">The height of the Video.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>width</term><description>The width of the Video.</description></item>
        /// <item><term>height</term><description>The height of the Video.</description></item>
        /// </list>
        /// </returns>
        public (double width, double height) GetDimensions(double width, double height) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the scaling filters used when drawing the Video.</para>
        /// </summary>
        /// <param name="min">The filter mode used when scaling the Video down.</param>
        /// <param name="mag">The filter mode used when scaling the Video up.</param>
        /// <param name="anisotropy">Maximum amount of anisotropic filtering used.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>min</term><description>The filter mode used when scaling the Video down.</description></item>
        /// <item><term>mag</term><description>The filter mode used when scaling the Video up.</description></item>
        /// <item><term>anisotropy</term><description>Maximum amount of anisotropic filtering used.</description></item>
        /// </list>
        /// </returns>
        public (FilterMode min, FilterMode mag, double anisotropy) GetFilter(FilterMode min, FilterMode mag, double anisotropy = 1) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the height of the Video in pixels.</para>
        /// </summary>
        /// <param name="height">The height of the Video.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>height</term><description>The height of the Video.</description></item>
        /// </list>
        /// </returns>
        public double GetHeight(double height) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the audio Source used for playing back the video's audio. May return nil if the video has no audio, or if Video:setSource is called with a nil argument.</para>
        /// </summary>
        /// <param name="source">The audio Source used for audio playback, or nil if the video has no audio.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>source</term><description>The audio Source used for audio playback, or nil if the video has no audio.</description></item>
        /// </list>
        /// </returns>
        public object GetSource(object source) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the VideoStream object used for decoding and controlling the video.</para>
        /// </summary>
        /// <param name="stream">The VideoStream used for decoding and controlling the video.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>stream</term><description>The VideoStream used for decoding and controlling the video.</description></item>
        /// </list>
        /// </returns>
        public object GetStream(object stream) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the width of the Video in pixels.</para>
        /// </summary>
        /// <param name="width">The width of the Video.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>width</term><description>The width of the Video.</description></item>
        /// </list>
        /// </returns>
        public double GetWidth(double width) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets whether the Video is currently playing.</para>
        /// </summary>
        /// <param name="playing">Whether the video is playing.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>playing</term><description>Whether the video is playing.</description></item>
        /// </list>
        /// </returns>
        public bool IsPlaying(bool playing) => throw new NotImplementedException();
        /// <summary>
        /// <para>Pauses the Video.</para>
        /// </summary>
        public void Pause() => throw new NotImplementedException();
        /// <summary>
        /// <para>Starts playing the Video. In order for the video to appear onscreen it must be drawn with love.graphics.draw.</para>
        /// </summary>
        public void Play() => throw new NotImplementedException();
        /// <summary>
        /// <para>Rewinds the Video to the beginning.</para>
        /// </summary>
        public void Rewind() => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the current playback position of the Video.</para>
        /// </summary>
        /// <param name="offset">The time in seconds since the beginning of the Video.</param>
        public void Seek(double offset) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the scaling filters used when drawing the Video.</para>
        /// </summary>
        /// <param name="min">The filter mode used when scaling the Video down.</param>
        /// <param name="mag">The filter mode used when scaling the Video up.</param>
        /// <param name="anisotropy">Maximum amount of anisotropic filtering used.</param>
        public void SetFilter(FilterMode min, FilterMode mag, double anisotropy = 1) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the audio Source used for playing back the video's audio. The audio Source also controls playback speed and synchronization.</para>
        /// </summary>
        /// <param name="source">The audio Source used for audio playback, or nil to disable audio synchronization.</param>
        public void SetSource(object source = null) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the current playback position of the Video.</para>
        /// </summary>
        /// <param name="seconds">The time in seconds since the beginning of the Video.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>seconds</term><description>The time in seconds since the beginning of the Video.</description></item>
        /// </list>
        /// </returns>
        public double Tell(double seconds) => throw new NotImplementedException();
    }
}
