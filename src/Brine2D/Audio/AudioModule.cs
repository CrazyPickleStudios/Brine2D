using System;

namespace Brine2D.Audio;

// TODO: Needs review
public sealed class AudioModule
{
/// <summary>
        /// <para>Gets a list of the names of the currently enabled effects.</para>
        /// </summary>
        /// <param name="effects">The list of the names of the currently enabled effects.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>effects</term><description>The list of the names of the currently enabled effects.</description></item>
        /// </list>
        /// </returns>
    public object GetActiveEffects(object effects) => throw new NotImplementedException();

/// <summary>
        /// <para>Gets the current number of simultaneously playing sources.</para>
        /// </summary>
        /// <param name="count">The current number of simultaneously playing sources.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>count</term><description>The current number of simultaneously playing sources.</description></item>
        /// </list>
        /// </returns>
    public double GetActiveSourceCount(double count) => throw new NotImplementedException();

/// <summary>
        /// <para>Returns the distance attenuation model.</para>
        /// </summary>
        /// <param name="model">The current distance model. The default is 'inverseclamped'.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>model</term><description>The current distance model. The default is 'inverseclamped'.</description></item>
        /// </list>
        /// </returns>
    public DistanceModel GetDistanceModel(DistanceModel model) => throw new NotImplementedException();

/// <summary>
        /// <para>Gets the current global scale factor for velocity-based doppler effects.</para>
        /// </summary>
        /// <param name="scale">The current doppler scale factor.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>scale</term><description>The current doppler scale factor.</description></item>
        /// </list>
        /// </returns>
    public double GetDopplerScale(double scale) => throw new NotImplementedException();

/// <summary>
        /// <para>Gets the settings associated with an effect.</para>
        /// </summary>
        /// <param name="name">The name of the effect.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>settings</term><description>The settings associated with the effect.</description></item>
        /// </list>
        /// </returns>
    public object GetEffect(string name) => throw new NotImplementedException();

/// <summary>
        /// <para>Gets the maximum number of active effects supported by the system.</para>
        /// </summary>
        /// <param name="maximum">The maximum number of active effects.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>maximum</term><description>The maximum number of active effects.</description></item>
        /// </list>
        /// </returns>
    public double GetMaxSceneEffects(double maximum) => throw new NotImplementedException();

/// <summary>
        /// <para>Gets the maximum number of active Effects in a single Source object, that the system can support.</para>
        /// </summary>
        /// <param name="maximum">The maximum number of active Effects per Source.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>maximum</term><description>The maximum number of active Effects per Source.</description></item>
        /// </list>
        /// </returns>
    public double GetMaxSourceEffects(double maximum) => throw new NotImplementedException();

/// <summary>
        /// <para>Returns the orientation of the listener.</para>
        /// </summary>
        /// <param name="fx">Forward vector of the listener orientation.</param>
        /// <param name="ux">Up vector of the listener orientation.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>fx</term><description>Forward vector of the listener orientation.</description></item>
        /// <item><term>ux</term><description>Up vector of the listener orientation.</description></item>
        /// </list>
        /// </returns>
    public (double fx, double ux) GetOrientation(double fx, double ux) => throw new NotImplementedException();

/// <summary>
        /// <para>Returns the position of the listener. Please note that positional audio only works for mono (i.e. non-stereo) sources.</para>
        /// </summary>
        /// <param name="x">The X position of the listener.</param>
        /// <param name="y">The Y position of the listener.</param>
        /// <param name="z">The Z position of the listener.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>x</term><description>The X position of the listener.</description></item>
        /// <item><term>y</term><description>The Y position of the listener.</description></item>
        /// <item><term>z</term><description>The Z position of the listener.</description></item>
        /// </list>
        /// </returns>
    public (double x, double y, double z) GetPosition(double x, double y, double z) => throw new NotImplementedException();

/// <summary>
        /// <para>Gets a list of RecordingDevices on the system.</para>
        /// <para>The first device in the list is the user's default recording device. The list may be empty if there are no microphones connected to the system.</para>
        /// <para>Audio recording is currently not supported on iOS.</para>
        /// </summary>
        /// <param name="devices">The list of connected .</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>devices</term><description>The list of connected .</description></item>
        /// </list>
        /// </returns>
    public object GetRecordingDevices(object devices) => throw new NotImplementedException();

/// <summary>
        /// <para>Gets the current number of simultaneously playing sources.</para>
        /// </summary>
        /// <param name="numSources">The current number of simultaneously playing sources.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>numSources</term><description>The current number of simultaneously playing sources.</description></item>
        /// </list>
        /// </returns>
    public double GetSourceCount(double numSources) => throw new NotImplementedException();

/// <summary>
        /// <para>Returns the velocity of the listener.</para>
        /// </summary>
        /// <param name="x">The X velocity of the listener.</param>
        /// <param name="y">The Y velocity of the listener.</param>
        /// <param name="z">The Z velocity of the listener.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>x</term><description>The X velocity of the listener.</description></item>
        /// <item><term>y</term><description>The Y velocity of the listener.</description></item>
        /// <item><term>z</term><description>The Z velocity of the listener.</description></item>
        /// </list>
        /// </returns>
    public (double x, double y, double z) GetVelocity(double x, double y, double z) => throw new NotImplementedException();

/// <summary>
        /// <para>Returns the master volume.</para>
        /// </summary>
        /// <param name="volume">The current master volume</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>volume</term><description>The current master volume</description></item>
        /// </list>
        /// </returns>
    public double GetVolume(double volume) => throw new NotImplementedException();

/// <summary>
        /// <para>Gets whether audio effects are supported in the system.</para>
        /// </summary>
        /// <param name="supported">True if effects are supported, false otherwise.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>supported</term><description>True if effects are supported, false otherwise.</description></item>
        /// </list>
        /// </returns>
    public bool IsEffectsSupported(bool supported) => throw new NotImplementedException();

/// <summary>
        /// <para>Creates a new Source usable for real-time generated sound playback with Source:queue.</para>
        /// <para>Queueable sources allow SoundData objects to be played seamlessly one after another, without the need to wait for an update cycle.</para>
        /// </summary>
        /// <param name="samplerate">Number of samples per second when playing.</param>
        /// <param name="bitdepth">Bits per sample (8 or 16).</param>
        /// <param name="channels">1 for mono or 2 for stereo.</param>
        /// <param name="buffercount">The number of buffers that can be queued up at any given time with . Cannot be greater than 64. A sensible default (~8) is chosen if no value is specified.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>source</term><description>The new Source usable with .</description></item>
        /// </list>
        /// </returns>
    public object NewQueueableSource(double samplerate, double bitdepth, double channels, double buffercount = 8) => throw new NotImplementedException();

/// <summary>
        /// <para>Sets the distance attenuation model.</para>
        /// </summary>
        /// <param name="model">The new distance model.</param>
    public void SetDistanceModel(DistanceModel model) => throw new NotImplementedException();

/// <summary>
        /// <para>Sets a global scale factor for velocity-based doppler effects. The default scale value is 1.</para>
        /// </summary>
        /// <param name="scale">The new doppler scale factor. The scale must be greater than 0.</param>
    public void SetDopplerScale(double scale) => throw new NotImplementedException();

/// <summary>
        /// <para>Defines an effect that can be applied to a Source.</para>
        /// <para>Not all systems support audio effects. Use love.audio.isEffectsSupported to check.</para>
        /// </summary>
        /// <param name="name">The name of the effect.</param>
        /// <param name="settings">
        /// The settings to use for this effect, with the following fields:
        /// <list type="bullet">
        /// <item><term>type</term><description>EffectType: The type of effect to use.</description></item>
        /// <item><term>volume</term><description>number: The volume of the effect.</description></item>
        /// <item><term></term><description>number ...: Effect-specific settings. See for available effects and their corresponding settings.</description></item>
        /// </list>
        /// </param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>success</term><description>Whether the effect was successfully created.</description></item>
        /// </list>
        /// </returns>
    public bool SetEffect(string name, object settings) => throw new NotImplementedException();

/// <summary>
        /// <para>Defines an effect that can be applied to a Source.</para>
        /// <para>Not all systems support audio effects. Use love.audio.isEffectsSupported to check.</para>
        /// </summary>
        /// <param name="name">The name of the effect.</param>
        /// <param name="enabled">If false and the given effect name was previously set, disables the effect.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>success</term><description>Whether the effect was successfully disabled.</description></item>
        /// </list>
        /// </returns>
    public bool SetEffect(string name, bool enabled = true) => throw new NotImplementedException();

/// <summary>
        /// <para>Sets whether the system should mix the audio with the system's audio.</para>
        /// </summary>
        /// <param name="mix">True to enable mixing, false to disable it.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>success</term><description>True if the change succeeded, false otherwise.</description></item>
        /// </list>
        /// </returns>
    public bool SetMixWithSystem(bool mix) => throw new NotImplementedException();

/// <summary>
        /// <para>Sets the orientation of the listener.</para>
        /// </summary>
        /// <param name="fx">Forward vector of the listener orientation.</param>
        /// <param name="ux">Up vector of the listener orientation.</param>
    public void SetOrientation(double fx, double ux) => throw new NotImplementedException();

/// <summary>
        /// <para>Sets the position of the listener, which determines how sounds play.</para>
        /// </summary>
        /// <param name="x">The x position of the listener.</param>
        /// <param name="y">The y position of the listener.</param>
        /// <param name="z">The z position of the listener.</param>
    public void SetPosition(double x, double y, double z) => throw new NotImplementedException();

/// <summary>
        /// <para>Sets the velocity of the listener.</para>
        /// </summary>
        /// <param name="x">The X velocity of the listener.</param>
        /// <param name="y">The Y velocity of the listener.</param>
        /// <param name="z">The Z velocity of the listener.</param>
    public void SetVelocity(double x, double y, double z) => throw new NotImplementedException();

/// <summary>
        /// <para>Sets the master volume.</para>
        /// </summary>
        /// <param name="volume">1.0 is max and 0.0 is off.</param>
    public void SetVolume(double volume) => throw new NotImplementedException();

}
