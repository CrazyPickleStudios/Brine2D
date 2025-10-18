namespace Brine2D
{
    /// <summary>
    /// Types of waveforms for ringmodulator effect.
    /// </summary>
    // TODO: Requires Review
    public enum EffectWaveform
    {
        /// <summary>
        /// A sawtooth wave, also known as a ramp wave. Named for its linear rise, and (near-)instantaneous fall along time.
        /// </summary>
        Sawtooth,
        /// <summary>
        /// A sine wave. Follows a trigonometric sine function.
        /// </summary>
        Sine,
        /// <summary>
        /// A square wave. Switches between high and low states (near-)instantaneously.
        /// </summary>
        Square,
        /// <summary>
        /// A triangle wave. Follows a linear rise and fall that repeats periodically.
        /// </summary>
        Triangle,
    }
}
