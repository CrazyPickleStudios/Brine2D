using System;

namespace Brine2D;

// TODO: Needs review
public sealed class SystemModule
{
/// <summary>
        /// <para>Gets text from the clipboard.</para>
        /// </summary>
        /// <param name="text">The text currently held in the system's clipboard.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>text</term><description>The text currently held in the system's clipboard.</description></item>
        /// </list>
        /// </returns>
    public string GetClipboardText(string text) => throw new NotImplementedException();

/// <summary>
        /// <para>Gets the current operating system. In general, LÖVE abstracts away the need to know the current operating system, but there are a few cases where it can be useful (especially in combination with os.execute.)</para>
        /// </summary>
        /// <param name="osString">The current operating system. , , , or .</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>osString</term><description>The current operating system. , , , or .</description></item>
        /// </list>
        /// </returns>
    public string GetOS(string osString) => throw new NotImplementedException();

    /// <summary>
    /// <para>Gets information about the system's power supply.</para>
    /// </summary>
    /// <param name="state">The basic state of the power supply.</param>
    /// <param name="percent">Percentage of battery life left, between 0 and 100. nil if the value can't be determined or there's no battery.</param>
    /// <param name="seconds">Seconds of battery life left. nil if the value can't be determined or there's no battery.</param>
    /// <returns>
    /// <list type="bullet">
    /// <item><term>state</term><description>The basic state of the power supply.</description></item>
    /// <item><term>percent</term><description>Percentage of battery life left, between 0 and 100. nil if the value can't be determined or there's no battery.</description></item>
    /// <item><term>seconds</term><description>Seconds of battery life left. nil if the value can't be determined or there's no battery.</description></item>
    /// </list>
    /// </returns>
    // TODO: public (PowerState state, double percent, double seconds) GetPowerInfo(PowerState state, double percent = null, double seconds = null) => throw new NotImplementedException();

    /// <summary>
    /// <para>Gets information about the system's power supply.</para>
    /// </summary>
    public void GetPowerInfo() => throw new NotImplementedException();

/// <summary>
        /// <para>Gets the amount of logical processors in the system.</para>
        /// </summary>
        /// <param name="processorCount">Amount of logical processors.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>processorCount</term><description>Amount of logical processors.</description></item>
        /// </list>
        /// </returns>
    public double GetProcessorCount(double processorCount) => throw new NotImplementedException();

/// <summary>
        /// <para>Gets whether another application on the system is playing music in the background.</para>
        /// <para>Currently this is implemented on iOS and Android, and will always return false on other operating systems. The t.audio.mixwithsystem flag in love.conf can be used to configure whether background audio / music from other apps should play while LÖVE is open.</para>
        /// </summary>
        /// <param name="backgroundmusic">True if the user is playing music in the background via another app, false otherwise.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>backgroundmusic</term><description>True if the user is playing music in the background via another app, false otherwise.</description></item>
        /// </list>
        /// </returns>
    public bool HasBackgroundMusic(bool backgroundmusic) => throw new NotImplementedException();

/// <summary>
        /// <para>Opens a URL with the user's web or file browser.</para>
        /// </summary>
        /// <param name="url">The URL to open. Must be formatted as a proper URL.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>success</term><description>Whether the URL was opened successfully.</description></item>
        /// </list>
        /// </returns>
    public bool OpenURL(string url) => throw new NotImplementedException();

/// <summary>
        /// <para>Opens a URL with the user's web or file browser.</para>
        /// </summary>
    public void OpenURL() => throw new NotImplementedException();

/// <summary>
        /// <para>Puts text in the clipboard.</para>
        /// </summary>
        /// <param name="text">The new text to hold in the system's clipboard.</param>
    public void SetClipboardText(string text) => throw new NotImplementedException();

/// <summary>
        /// <para>Puts text in the clipboard.</para>
        /// </summary>
    public void Print() => throw new NotImplementedException();

/// <summary>
        /// <para>Causes the device to vibrate, if possible. Currently this will only work on Android and iOS devices that have a built-in vibration motor.</para>
        /// </summary>
        /// <param name="seconds">The duration to vibrate for. If called on an iOS device, it will always vibrate for 0.5 seconds due to limitations in the iOS system APIs.</param>
    public void Vibrate(double seconds = 0.5) => throw new NotImplementedException();

}
