namespace Brine2D
{
    /// <summary>
    /// The different distance models.
    /// </summary>
    // TODO: Requires Review
    public enum DistanceModel
    {
        /// <summary>
        /// Sources do not get attenuated.
        /// </summary>
        None,
        /// <summary>
        /// Inverse distance attenuation.
        /// </summary>
        Inverse,
        /// <summary>
        /// Inverse distance attenuation. Gain is clamped. In version 0.9.2 and older this is named inverse clamped.
        /// </summary>
        Inverseclamped,
        /// <summary>
        /// Linear attenuation.
        /// </summary>
        Linear,
        /// <summary>
        /// Linear attenuation. Gain is clamped. In version 0.9.2 and older this is named linear clamped.
        /// </summary>
        Linearclamped,
        /// <summary>
        /// Exponential attenuation.
        /// </summary>
        Exponent,
        /// <summary>
        /// Exponential attenuation. Gain is clamped. In version 0.9.2 and older this is named exponent clamped.
        /// </summary>
        Exponentclamped,
    }
}
