using System;

namespace Brine2D.Sound;

// TODO: Needs review
public sealed class SoundModule
{
/// <summary>
        /// <para>Creates new SoundData from a filepath, File, or Decoder. It's also possible to create SoundData with a custom sample rate, channel and bit depth.</para>
        /// <para>The sound data will be decoded to the memory in a raw format. It is recommended to create only short sounds like effects, as a 3 minute song uses 30 MB of memory this way.</para>
        /// </summary>
        /// <param name="filename">The file name of the file to load.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>soundData</term><description>A new SoundData object.</description></item>
        /// </list>
        /// </returns>
    public object NewSoundData(string filename) => throw new NotImplementedException();

/// <summary>
        /// <para>Creates new SoundData from a filepath, File, or Decoder. It's also possible to create SoundData with a custom sample rate, channel and bit depth.</para>
        /// <para>The sound data will be decoded to the memory in a raw format. It is recommended to create only short sounds like effects, as a 3 minute song uses 30 MB of memory this way.</para>
        /// </summary>
        /// <param name="file">A File pointing to an audio file.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>soundData</term><description>A new SoundData object.</description></item>
        /// </list>
        /// </returns>
    public object NewSoundData(object file) => throw new NotImplementedException();

    /// <summary>
    /// <para>Creates new SoundData from a filepath, File, or Decoder. It's also possible to create SoundData with a custom sample rate, channel and bit depth.</para>
    /// <para>The sound data will be decoded to the memory in a raw format. It is recommended to create only short sounds like effects, as a 3 minute song uses 30 MB of memory this way.</para>
    /// </summary>
    /// <param name="decoder">Decode data from this Decoder until EOF.</param>
    /// <returns>
    /// <list type="bullet">
    /// <item><term>soundData</term><description>A new SoundData object.</description></item>
    /// </list>
    /// </returns>
    // TODO: public object NewSoundData(object decoder) => throw new NotImplementedException();

    /// <summary>
    /// <para>Creates new SoundData from a filepath, File, or Decoder. It's also possible to create SoundData with a custom sample rate, channel and bit depth.</para>
    /// <para>The sound data will be decoded to the memory in a raw format. It is recommended to create only short sounds like effects, as a 3 minute song uses 30 MB of memory this way.</para>
    /// </summary>
    /// <param name="samples">Total number of samples.</param>
    /// <param name="rate">Number of samples per second</param>
    /// <param name="bits">Bits per sample (8 or 16).</param>
    /// <param name="channels">Either 1 for mono or 2 for stereo.</param>
    /// <returns>
    /// <list type="bullet">
    /// <item><term>soundData</term><description>A new SoundData object.</description></item>
    /// </list>
    /// </returns>
    public object NewSoundData(double samples, double rate = 44100, double bits = 16, double channels = 2) => throw new NotImplementedException();

}
