using System.Text.Json.Serialization;

namespace Brine2D.Core.Input.Actions;

public sealed partial class InputActions<TAction, TAxis>
{
    /// <summary>
    ///     Represents configuration for repeating input actions, including whether repeating is enabled,
    ///     the initial delay before repeating starts, and the interval between repeats.
    /// </summary>
    private readonly struct RepeatConfig
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="RepeatConfig" /> struct.
        /// </summary>
        /// <param name="enabled">Whether repeating is enabled.</param>
        /// <param name="initialDelay">Initial delay in seconds before repeating begins.</param>
        /// <param name="interval">Interval in seconds between repeated activations.</param>
        public RepeatConfig(bool enabled, double initialDelay, double interval)
        {
            Enabled = enabled;
            InitialDelay = initialDelay;
            Interval = interval;
        }

        /// <summary>
        ///     Gets a value indicating whether repeating is enabled.
        /// </summary>
        [JsonPropertyName("enabled")]
        public bool Enabled { get; }

        /// <summary>
        ///     Gets the initial delay in seconds before repeating begins.
        /// </summary>
        [JsonPropertyName("initialDelay")]
        public double InitialDelay { get; }

        /// <summary>
        ///     Gets the repeat interval in seconds between repeated activations.
        /// </summary>
        [JsonPropertyName("interval")]
        public double Interval { get; }
    }
}