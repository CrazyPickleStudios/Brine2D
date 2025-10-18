namespace Brine2D
{
    /// <summary>
    /// The basic state of the system's power supply.
    /// </summary>
    // TODO: Requires Review
    public enum PowerState
    {
        /// <summary>
        /// Cannot determine power status.
        /// </summary>
        Unknown,
        /// <summary>
        /// Not plugged in, running on a battery.
        /// </summary>
        Battery,
        /// <summary>
        /// Plugged in, no battery available.
        /// </summary>
        Nobattery,
        /// <summary>
        /// Plugged in, charging battery.
        /// </summary>
        Charging,
        /// <summary>
        /// Plugged in, battery is fully charged.
        /// </summary>
        Charged,
    }
}
