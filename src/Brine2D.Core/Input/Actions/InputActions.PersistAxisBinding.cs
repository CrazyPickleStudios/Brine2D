using System.Text.Json.Serialization;

namespace Brine2D.Core.Input.Actions;

public sealed partial class InputActions<TAction, TAxis>
{
    /// <summary>
    ///     Persistence DTO for an axis binding. This model is serialized to/from JSON and
    ///     supports multiple binding kinds (e.g., Gamepad axis, Mouse axis, Composite 2-key axis).
    ///     All properties are optional because only a subset applies depending on the binding <see cref="Type" />.
    /// </summary>
    /// <remarks>
    ///     JSON field names are controlled via <see cref="JsonPropertyNameAttribute" /> to keep a stable save format.
    ///     No validation or transformation is performed here; consumers should validate ranges (e.g., deadzone 0..1).
    /// </remarks>
    private sealed class PersistAxisBinding
    {
        /// <summary>
        ///     Identifier/name of the gamepad axis (e.g., LeftStickX, LeftStickY, RightTrigger).
        /// </summary>
        [JsonPropertyName("axis")]
        public string? Axis { get; set; }

        /// <summary>
        ///     Response curve applied to the axis value. Typically 1 = linear; other values can ease in/out.
        /// </summary>
        [JsonPropertyName("curve")]
        public float? Curve { get; set; }

        /// <summary>
        ///     Deadzone threshold applied to the axis in the range [0..1].
        ///     Values within the deadzone are treated as zero.
        /// </summary>
        [JsonPropertyName("deadzone")]
        public float? Deadzone { get; set; }

        /// <summary>
        ///     When true, the axis output is inverted.
        /// </summary>
        [JsonPropertyName("invert")]
        public bool? Invert { get; set; }

        /// <summary>
        ///     Identifier/name of the mouse axis (e.g., X, Y, Wheel).
        /// </summary>
        [JsonPropertyName("mouseAxis")]
        public string? MouseAxis { get; set; }

        /// <summary>
        ///     Key identifier for the negative direction (e.g., A, LeftArrow).
        /// </summary>
        [JsonPropertyName("negative")]
        public string? Negative { get; set; }

        /// <summary>
        ///     Optional gamepad index/pad number if applicable (e.g., 0 = first pad).
        /// </summary>
        [JsonPropertyName("pad")]
        public int? Pad { get; set; }

        /// <summary>
        ///     Key identifier for the positive direction (e.g., D, RightArrow).
        /// </summary>
        [JsonPropertyName("positive")]
        public string? Positive { get; set; }

        /// <summary>
        ///     Scalar multiplier applied to the resulting axis value.
        /// </summary>
        [JsonPropertyName("scale")]
        public float? Scale { get; set; }

        /// <summary>
        ///     Sensitivity factor for input that supports it (e.g., mouse delta, digital smoothing).
        /// </summary>
        [JsonPropertyName("sensitivity")]
        public float? Sensitivity { get; set; }

        /// <summary>
        ///     Binding kind discriminator (e.g., "GamepadAxis", "MouseAxis", "Composite2Keys").
        ///     Determines which subset of properties is meaningful.
        /// </summary>
        [JsonPropertyName("type")]
        public string? Type { get; set; }
    }
}