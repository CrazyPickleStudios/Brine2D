namespace Brine2D
{
    /// <summary>
    /// Different types of audio effects.
    /// </summary>
    // TODO: Requires Review
    public enum EffectType
    {
        /// <summary>
        /// Plays multiple copies of the sound with slight pitch and time variation. Used to make sounds sound "fuller" or "thicker".
        /// </summary>
        Chorus,
        /// <summary>
        /// Decreases the dynamic range of the sound, making the loud and quiet parts closer in volume, producing a more uniform amplitude throughout time.
        /// </summary>
        Compressor,
        /// <summary>
        /// Alters the sound by amplifying it until it clips, shearing off parts of the signal, leading to a compressed and distorted sound.
        /// </summary>
        Distortion,
        /// <summary>
        /// Decaying feedback based effect, on the order of seconds. Also known as delay; causes the sound to repeat at regular intervals at a decreasing volume.
        /// </summary>
        Echo,
        /// <summary>
        /// Adjust the frequency components of the sound using a 4-band (low-shelf, two band-pass and a high-shelf) equalizer.
        /// </summary>
        Equalizer,
        /// <summary>
        /// Plays two copies of the sound; while varying the phase, or equivalently delaying one of them, by amounts on the order of milliseconds, resulting in phasing sounds.
        /// </summary>
        Flanger,
        /// <summary>
        /// Decaying feedback based effect, on the order of milliseconds. Used to simulate the reflection off of the surroundings.
        /// </summary>
        Reverb,
        /// <summary>
        /// An implementation of amplitude modulation; multiplies the source signal with a simple waveform, to produce either volume changes, or inharmonic overtones.
        /// </summary>
        Ringmodulator,
    }
}
