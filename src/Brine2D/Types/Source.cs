namespace Brine2D
{
    /// <summary>
    /// <para>A Source represents audio you can play back.</para>
/// <para>You can do interesting things with Sources, like set the volume, pitch, and its position relative to the listener. Please note that positional audio only works for mono (i.e. non-stereo) sources.</para>
/// <para>The source is internally referenced as long as it is playing.</para>
/// <para>The Source controls (play/pause/stop) act according to the following state table.</para>
/// <para>And for fans of flowcharts (note: omitted calls have no effect, stopping always rewinds).</para>
    /// </summary>
    // TODO: Requires Review
    public class Source
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
        /// <para>Creates an identical copy of the Source in the stopped state.</para>
        /// <para>Static Sources will use significantly less memory and take much less time to be created if Source:clone is used to create them instead of love.audio.newSource, so this method should be preferred when making multiple Sources which play the same sound.</para>
        /// </summary>
        /// <param name="source">The new identical copy of this Source.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>source</term><description>The new identical copy of this Source.</description></item>
        /// </list>
        /// </returns>
        public object Clone(object source) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets a list of the Source's active effect names.</para>
        /// </summary>
        /// <param name="effects">A list of the source's active effect names.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>effects</term><description>A list of the source's active effect names.</description></item>
        /// </list>
        /// </returns>
        public object GetActiveEffects(object effects) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the amount of air absorption applied to the Source.</para>
        /// <para>By default the value is set to 0 which means that air absorption effects are disabled. A value of 1 will apply high frequency attenuation to the Source at a rate of 0.05 dB per meter.</para>
        /// </summary>
        /// <param name="amount">The amount of air absorption applied to the Source.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>amount</term><description>The amount of air absorption applied to the Source.</description></item>
        /// </list>
        /// </returns>
        public double GetAirAbsorption(double amount) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the reference and maximum attenuation distances of the Source. The values, combined with the current DistanceModel, affect how the Source's volume attenuates based on distance from the listener.</para>
        /// </summary>
        /// <param name="ref">The current reference attenuation distance. If the current is clamped, this is the minimum distance before the Source is no longer attenuated.</param>
        /// <param name="max">The current maximum attenuation distance.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>ref</term><description>The current reference attenuation distance. If the current is clamped, this is the minimum distance before the Source is no longer attenuated.</description></item>
        /// <item><term>max</term><description>The current maximum attenuation distance.</description></item>
        /// </list>
        /// </returns>
        // TODO: public (double ref, double max) GetAttenuationDistances(double ref, double max) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the number of channels in the Source. Only 1-channel (mono) Sources can use directional and positional effects.</para>
        /// </summary>
        /// <param name="channels">1 for mono, 2 for stereo.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>channels</term><description>1 for mono, 2 for stereo.</description></item>
        /// </list>
        /// </returns>
        public double GetChannelCount(double channels) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the Source's directional volume cones. Together with Source:setDirection, the cone angles allow for the Source's volume to vary depending on its direction.</para>
        /// </summary>
        /// <param name="innerAngle">The inner angle from the Source's direction, in radians. The Source will play at normal volume if the listener is inside the cone defined by this angle.</param>
        /// <param name="outerAngle">The outer angle from the Source's direction, in radians. The Source will play at a volume between the normal and outer volumes, if the listener is in between the cones defined by the inner and outer angles.</param>
        /// <param name="outerVolume">The Source's volume when the listener is outside both the inner and outer cone angles.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>innerAngle</term><description>The inner angle from the Source's direction, in radians. The Source will play at normal volume if the listener is inside the cone defined by this angle.</description></item>
        /// <item><term>outerAngle</term><description>The outer angle from the Source's direction, in radians. The Source will play at a volume between the normal and outer volumes, if the listener is in between the cones defined by the inner and outer angles.</description></item>
        /// <item><term>outerVolume</term><description>The Source's volume when the listener is outside both the inner and outer cone angles.</description></item>
        /// </list>
        /// </returns>
        public (double innerAngle, double outerAngle, double outerVolume) GetCone(double innerAngle, double outerAngle, double outerVolume) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the direction of the Source.</para>
        /// </summary>
        /// <param name="x">The X part of the direction vector.</param>
        /// <param name="y">The Y part of the direction vector.</param>
        /// <param name="z">The Z part of the direction vector.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>x</term><description>The X part of the direction vector.</description></item>
        /// <item><term>y</term><description>The Y part of the direction vector.</description></item>
        /// <item><term>z</term><description>The Z part of the direction vector.</description></item>
        /// </list>
        /// </returns>
        public (double x, double y, double z) GetDirection(double x, double y, double z) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the duration of the Source. For streaming Sources it may not always be sample-accurate, and may return -1 if the duration cannot be determined at all.</para>
        /// </summary>
        /// <param name="unit">The time unit for the return value.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>duration</term><description>The duration of the Source, or -1 if it cannot be determined.</description></item>
        /// </list>
        /// </returns>
        // TODO: public double GetDuration(TimeUnit unit = "seconds") => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the filter settings associated to a specific effect.</para>
        /// <para>This function returns nil if the effect was applied with no filter settings associated to it.</para>
        /// </summary>
        /// <param name="name">The name of the effect.</param>
        /// <param name="filtersettings">An optional empty table that will be filled with the filter settings.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>filtersettings</term><description>
        /// The settings for the filter associated to this effect, or nil if the effect is not present in this Source or has no filter associated. The table has the following fields:
        /// <list type="bullet">
        /// <item><term>volume</term><description>number: The overall volume of the audio.</description></item>
        /// <item><term>highgain</term><description>number: Volume of high-frequency audio. Only applies to low-pass and band-pass filters.</description></item>
        /// <item><term>lowgain</term><description>number: Volume of low-frequency audio. Only applies to high-pass and band-pass filters.</description></item>
        /// </list>
        /// </description></item>
        /// </list>
        /// </returns>
        // TODO:  public object GetEffect(string name, object filtersettings = {}) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the filter settings currently applied to the Source.</para>
        /// </summary>
        /// <param name="settings">
        /// The filter settings to use for this Source, or nil if the Source has no active filter. The table has the following fields:
        /// <list type="bullet">
        /// <item><term>type</term><description>FilterType: The type of filter to use.</description></item>
        /// <item><term>volume</term><description>number: The overall volume of the audio.</description></item>
        /// <item><term>highgain</term><description>number: Volume of high-frequency audio. Only applies to low-pass and band-pass filters.</description></item>
        /// <item><term>lowgain</term><description>number: Volume of low-frequency audio. Only applies to high-pass and band-pass filters.</description></item>
        /// </list>
        /// </param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>settings</term><description>
        /// The filter settings to use for this Source, or nil if the Source has no active filter. The table has the following fields:
        /// <list type="bullet">
        /// <item><term>type</term><description>FilterType: The type of filter to use.</description></item>
        /// <item><term>volume</term><description>number: The overall volume of the audio.</description></item>
        /// <item><term>highgain</term><description>number: Volume of high-frequency audio. Only applies to low-pass and band-pass filters.</description></item>
        /// <item><term>lowgain</term><description>number: Volume of low-frequency audio. Only applies to high-pass and band-pass filters.</description></item>
        /// </list>
        /// </description></item>
        /// </list>
        /// </returns>
        public object GetFilter(object settings = null) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the number of free buffer slots in a queueable Source. If the queueable Source is playing, this value will increase up to the amount the Source was created with. If the queueable Source is stopped, it will process all of its internal buffers first, in which case this function will always return the amount it was created with.</para>
        /// </summary>
        /// <param name="buffers">How many more SoundData objects can be queued up.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>buffers</term><description>How many more SoundData objects can be queued up.</description></item>
        /// </list>
        /// </returns>
        public double GetFreeBufferCount(double buffers) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the current pitch of the Source.</para>
        /// </summary>
        /// <param name="pitch">The pitch, where 1.0 is normal.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>pitch</term><description>The pitch, where 1.0 is normal.</description></item>
        /// </list>
        /// </returns>
        public double GetPitch(double pitch) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the position of the Source.</para>
        /// </summary>
        /// <param name="x">The X position of the Source.</param>
        /// <param name="y">The Y position of the Source.</param>
        /// <param name="z">The Z position of the Source.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>x</term><description>The X position of the Source.</description></item>
        /// <item><term>y</term><description>The Y position of the Source.</description></item>
        /// <item><term>z</term><description>The Z position of the Source.</description></item>
        /// </list>
        /// </returns>
        public (double x, double y, double z) GetPosition(double x, double y, double z) => throw new NotImplementedException();
        /// <summary>
        /// <para>Returns the rolloff factor of the source.</para>
        /// </summary>
        /// <param name="rolloff">The rolloff factor.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>rolloff</term><description>The rolloff factor.</description></item>
        /// </list>
        /// </returns>
        public double GetRolloff(double rolloff) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the type of the Source.</para>
        /// </summary>
        /// <param name="sourcetype">The type of the source.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>sourcetype</term><description>The type of the source.</description></item>
        /// </list>
        /// </returns>
        public SourceType GetType(SourceType sourcetype) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the velocity of the Source.</para>
        /// </summary>
        /// <param name="x">The X part of the velocity vector.</param>
        /// <param name="y">The Y part of the velocity vector.</param>
        /// <param name="z">The Z part of the velocity vector.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>x</term><description>The X part of the velocity vector.</description></item>
        /// <item><term>y</term><description>The Y part of the velocity vector.</description></item>
        /// <item><term>z</term><description>The Z part of the velocity vector.</description></item>
        /// </list>
        /// </returns>
        public (double x, double y, double z) GetVelocity(double x, double y, double z) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the current volume of the Source.</para>
        /// </summary>
        /// <param name="volume">The volume of the Source, where 1.0 is normal volume.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>volume</term><description>The volume of the Source, where 1.0 is normal volume.</description></item>
        /// </list>
        /// </returns>
        public double GetVolume(double volume) => throw new NotImplementedException();
        /// <summary>
        /// <para>Returns the volume limits of the source.</para>
        /// </summary>
        /// <param name="min">The minimum volume.</param>
        /// <param name="max">The maximum volume.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>min</term><description>The minimum volume.</description></item>
        /// <item><term>max</term><description>The maximum volume.</description></item>
        /// </list>
        /// </returns>
        public (double min, double max) GetVolumeLimits(double min, double max) => throw new NotImplementedException();
        /// <summary>
        /// <para>Returns whether the Source will loop.</para>
        /// </summary>
        /// <param name="loop">True if the Source will loop, false otherwise.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>loop</term><description>True if the Source will loop, false otherwise.</description></item>
        /// </list>
        /// </returns>
        public bool IsLooping(bool loop) => throw new NotImplementedException();
        /// <summary>
        /// <para>Returns whether the Source is paused.</para>
        /// </summary>
        /// <param name="paused">True if the Source is paused, false otherwise.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>paused</term><description>True if the Source is paused, false otherwise.</description></item>
        /// </list>
        /// </returns>
        public bool IsPaused(bool paused) => throw new NotImplementedException();
        /// <summary>
        /// <para>Returns whether the Source is playing.</para>
        /// </summary>
        /// <param name="playing">True if the Source is playing, false otherwise.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>playing</term><description>True if the Source is playing, false otherwise.</description></item>
        /// </list>
        /// </returns>
        public bool IsPlaying(bool playing) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets whether the Source's position, velocity, direction, and cone angles are relative to the listener.</para>
        /// </summary>
        /// <param name="relative">True if the position, velocity, direction and cone angles are relative to the listener, false if they're absolute.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>relative</term><description>True if the position, velocity, direction and cone angles are relative to the listener, false if they're absolute.</description></item>
        /// </list>
        /// </returns>
        public bool IsRelative(bool relative) => throw new NotImplementedException();
        /// <summary>
        /// <para>Returns whether the Source is static.</para>
        /// </summary>
        /// <param name="static">True if the Source is static, false otherwise.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>static</term><description>True if the Source is static, false otherwise.</description></item>
        /// </list>
        /// </returns>
        // TODO: public bool IsStatic(bool static) => throw new NotImplementedException();
        /// <summary>
        /// <para>Pauses the Source.</para>
        /// </summary>
        public void Pause() => throw new NotImplementedException();
        /// <summary>
        /// <para>Queues SoundData for playback in a queueable Source.</para>
        /// <para>This method requires the Source to be created via love.audio.newQueueableSource.</para>
        /// </summary>
        /// <param name="soundData">The data to queue. The SoundData's sample rate, bit depth, and channel count must match the Source's.</param>
        /// <param name="offset">Starting position in bytes to queue.</param>
        /// <param name="length">Length in bytes to queue starting from specified offset.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>success</term><description>True if the data was successfully queued for playback, false if there were no to use for queueing.</description></item>
        /// </list>
        /// </returns>
        public bool Queue(object soundData, double offset, double length) => throw new NotImplementedException();
        /// <summary>
        /// <para>Queues SoundData for playback in a queueable Source.</para>
        /// <para>This method requires the Source to be created via love.audio.newQueueableSource.</para>
        /// </summary>
        /// <param name="userdata">A pointer returns from Data:getPointer</param>
        /// <param name="offset">Starting position in bytes to queue.</param>
        /// <param name="length">Length in bytes to queue starting from specified offset.</param>
        /// <param name="sampleRate">Sample rate.</param>
        /// <param name="bitDepth">Bit depth.</param>
        /// <param name="channels">Channel count.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>success</term><description>True if the data was successfully queued for playback, false if there were no to use for queueing.</description></item>
        /// </list>
        /// </returns>
        public bool Queue(object userdata, double offset, double length, double sampleRate, double bitDepth, double channels) => throw new NotImplementedException();
        /// <summary>
        /// <para>Resumes a paused Source.</para>
        /// </summary>
        public void Resume() => throw new NotImplementedException();
        /// <summary>
        /// <para>Rewinds a Source.</para>
        /// </summary>
        public void Rewind() => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the currently playing position of the Source. With the queueable source type, this method can only work on the currently playing buffer.</para>
        /// </summary>
        /// <param name="offset">The position to seek to.</param>
        /// <param name="unit">The unit of the position value.</param>
        // TODO: public void Seek(double offset, TimeUnit unit = "seconds") => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the amount of air absorption applied to the Source.</para>
        /// <para>By default the value is set to 0 which means that air absorption effects are disabled. A value of 1 will apply high frequency attenuation to the Source at a rate of 0.05 dB per meter.</para>
        /// <para>Air absorption can simulate sound transmission through foggy air, dry air, smoky atmosphere, etc. It can be used to simulate different atmospheric conditions within different locations in an area.</para>
        /// </summary>
        /// <param name="amount">The amount of air absorption applied to the Source. Must be between 0 and 10.</param>
        public void SetAirAbsorption(double amount) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the reference and maximum attenuation distances of the Source. The parameters, combined with the current DistanceModel, affect how the Source's volume attenuates based on distance.</para>
        /// <para>Distance attenuation is only applicable to Sources based on mono (rather than stereo) audio.</para>
        /// </summary>
        /// <param name="ref">The new reference attenuation distance. If the current is clamped, this is the minimum attenuation distance.</param>
        /// <param name="max">The new maximum attenuation distance.</param>
        // TODO: public void SetAttenuationDistances(double ref, double max) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the Source's directional volume cones. Together with Source:setDirection, the cone angles allow for the Source's volume to vary depending on its direction.</para>
        /// </summary>
        /// <param name="innerAngle">The inner angle from the Source's direction, in radians. The Source will play at normal volume if the listener is inside the cone defined by this angle.</param>
        /// <param name="outerAngle">The outer angle from the Source's direction, in radians. The Source will play at a volume between the normal and outer volumes, if the listener is in between the cones defined by the inner and outer angles.</param>
        /// <param name="outerVolume">The Source's volume when the listener is outside both the inner and outer cone angles.</param>
        public void SetCone(double innerAngle, double outerAngle, double outerVolume = 0) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the direction vector of the Source. A zero vector makes the source non-directional.</para>
        /// </summary>
        /// <param name="x">The X part of the direction vector.</param>
        /// <param name="y">The Y part of the direction vector.</param>
        /// <param name="z">The Z part of the direction vector.</param>
        public void SetDirection(double x, double y, double z) => throw new NotImplementedException();
        /// <summary>
        /// <para>Applies an audio effect to the Source.</para>
        /// <para>The effect must have been previously defined using love.audio.setEffect.</para>
        /// </summary>
        /// <param name="name">The name of the effect previously set up with .</param>
        /// <param name="enable">If false and the given effect name was previously enabled on this Source, disables the effect.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>success</term><description>Whether the effect was successfully applied to this Source.</description></item>
        /// </list>
        /// </returns>
        public bool SetEffect(string name, bool enable = true) => throw new NotImplementedException();
        /// <summary>
        /// <para>Applies an audio effect to the Source.</para>
        /// <para>The effect must have been previously defined using love.audio.setEffect.</para>
        /// </summary>
        /// <param name="name">The name of the effect previously set up with .</param>
        /// <param name="filtersettings">
        /// The filter settings to apply prior to the effect, with the following fields:
        /// <list type="bullet">
        /// <item><term>type</term><description>FilterType: The type of filter to use.</description></item>
        /// <item><term>volume</term><description>number: The overall audio input volume for the effect. Must be between 0 and 1. (Does not affect the dry audio output for the source.)</description></item>
        /// <item><term>highgain</term><description>number: Volume of high-frequency audio. Only applies to and filters. Must be between 0 and 1.</description></item>
        /// <item><term>lowgain</term><description>number: Volume of low-frequency audio. Only applies to and filters. Must be between 0 and 1.</description></item>
        /// </list>
        /// </param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>success</term><description>Whether the effect and filter were successfully applied to this Source.</description></item>
        /// </list>
        /// </returns>
        public bool SetEffect(string name, object filtersettings) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets a low-pass, high-pass, or band-pass filter to apply when playing the Source.</para>
        /// </summary>
        /// <param name="settings">
        /// The filter settings to use for this Source, with the following fields:
        /// <list type="bullet">
        /// <item><term>type</term><description>FilterType: The type of filter to use.</description></item>
        /// <item><term>volume</term><description>number: The overall volume of the audio. Must be between 0 and 1.</description></item>
        /// <item><term>highgain</term><description>number: Volume of high-frequency audio. Only applies to and filters. Must be between 0 and 1.</description></item>
        /// <item><term>lowgain</term><description>number: Volume of low-frequency audio. Only applies to and filters. Must be between 0 and 1.</description></item>
        /// </list>
        /// </param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>success</term><description>Whether the filter was successfully applied to the Source.</description></item>
        /// </list>
        /// </returns>
        public bool SetFilter(object settings) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets a low-pass, high-pass, or band-pass filter to apply when playing the Source.</para>
        /// </summary>
        public void SetFilter() => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets a low-pass, high-pass, or band-pass filter to apply when playing the Source.</para>
        /// </summary>
        // TODO: public void NewSource() => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets whether the Source should loop.</para>
        /// </summary>
        /// <param name="loop">True if the source should loop, false otherwise.</param>
        public void SetLooping(bool loop) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets whether the Source should loop.</para>
        /// </summary>
        // TODO: public void NewSource() => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the pitch of the Source.</para>
        /// </summary>
        /// <param name="pitch">Calculated with regard to 1 being the base pitch. Each reduction by 50 percent equals a pitch shift of -12 semitones (one octave reduction). Each doubling equals a pitch shift of 12 semitones (one octave increase). Zero is not a legal value.</param>
        public void SetPitch(double pitch) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the pitch of the Source.</para>
        /// </summary>
        // TODO: public void NewSource() => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the position of the Source. Please note that this only works for mono (i.e. non-stereo) sound files!</para>
        /// </summary>
        /// <param name="x">The X position of the Source.</param>
        /// <param name="y">The Y position of the Source.</param>
        /// <param name="z">The Z position of the Source.</param>
        public void SetPosition(double x, double y, double z) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets whether the Source's position, velocity, direction, and cone angles are relative to the listener, or absolute.</para>
        /// <para>By default, all sources are absolute and therefore relative to the origin of love's coordinate system [0, 0, 0]. Only absolute sources are affected by the position of the listener. Please note that positional audio only works for mono (i.e. non-stereo) sources.</para>
        /// </summary>
        /// <param name="enable">True to make the position, velocity, direction and cone angles relative to the listener, false to make them absolute.</param>
        public void SetRelative(bool enable = false) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the rolloff factor which affects the strength of the used distance attenuation.</para>
        /// <para>Extended information and detailed formulas can be found in the chapter "3.4. Attenuation By Distance" of OpenAL 1.1 specification.</para>
        /// </summary>
        /// <param name="rolloff">The new rolloff factor.</param>
        public void SetRolloff(double rolloff) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the velocity of the Source.</para>
        /// <para>This does not change the position of the Source, but lets the application know how it has to calculate the doppler effect.</para>
        /// </summary>
        /// <param name="x">The X part of the velocity vector.</param>
        /// <param name="y">The Y part of the velocity vector.</param>
        /// <param name="z">The Z part of the velocity vector.</param>
        public void SetVelocity(double x, double y, double z) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the current volume of the Source.</para>
        /// </summary>
        /// <param name="volume">The volume for a Source, where 1.0 is normal volume. Volume cannot be raised above 1.0.</param>
        public void SetVolume(double volume) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the current volume of the Source.</para>
        /// </summary>
        public void NewSource() => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the volume limits of the source. The limits have to be numbers from 0 to 1.</para>
        /// </summary>
        /// <param name="min">The minimum volume.</param>
        /// <param name="max">The maximum volume.</param>
        public void SetVolumeLimits(double min, double max) => throw new NotImplementedException();
        /// <summary>
        /// <para>Stops a Source.</para>
        /// </summary>
        public void Stop() => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the currently playing position of the Source. With the queueable source type, this method returns a value based only on the currently playing buffer.</para>
        /// </summary>
        /// <param name="unit">The type of unit for the return value.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>position</term><description>The currently playing position of the .</description></item>
        /// </list>
        /// </returns>
        // TODO: public double Tell(TimeUnit unit = "seconds") => throw new NotImplementedException();
    }
}
