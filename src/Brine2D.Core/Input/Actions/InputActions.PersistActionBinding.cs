using System.Text.Json.Serialization;

namespace Brine2D.Core.Input.Actions;

public sealed partial class InputActions<TAction, TAxis>
{
    /// <summary>
    ///     Persistence model for a single input binding. Captures the serialized form of
    ///     different input sources (keyboard, mouse, gamepad, axis/trigger) and thresholds.
    /// </summary>
    private sealed class PersistActionBinding
    {
        /// <summary>
        ///     Logical axis identifier used as a trigger (e.g., "LeftTrigger", "RightTrigger", "MouseWheel").
        /// </summary>
        [JsonPropertyName("axis")]
        public string? Axis { get; set; }

        /// <summary>
        ///     Gamepad button or analog trigger identifier (e.g., "A", "B", "LeftTrigger").
        /// </summary>
        [JsonPropertyName("button")]
        public string? Button { get; set; }

        /// <summary>
        ///     Primary key for a key chord (e.g., "K", "Space", "Escape").
        /// </summary>
        [JsonPropertyName("key")]
        public string? Key { get; set; }

        /// <summary>
        ///     Modifier keys for a key chord (e.g., "Ctrl", "Ctrl+Shift"). Separator is implementation-defined.
        /// </summary>
        [JsonPropertyName("mods")]
        public string? Mods { get; set; }

        /// <summary>
        ///     Mouse button identifier (e.g., "Left", "Right", "Middle", "X1", "X2").
        /// </summary>
        [JsonPropertyName("mouseButton")]
        public string? MouseButton { get; set; }

        /// <summary>
        ///     Gamepad index (pad number). Null indicates any/unspecified pad.
        /// </summary>
        [JsonPropertyName("pad")]
        public int? Pad { get; set; }

        /// <summary>
        ///     Press threshold for analog inputs in range [0, 1]. Considered pressed at or above this value.
        /// </summary>
        [JsonPropertyName("press")]
        public float? Press { get; set; }

        /// <summary>
        ///     Release threshold for analog inputs in range [0, 1]. Considered released at or below this value.
        /// </summary>
        [JsonPropertyName("release")]
        public float? Release { get; set; }

        /// <summary>
        ///     Binding discriminator (e.g., "KeyChord", "MouseButton", "GamepadButton", "Trigger", "Axis").
        /// </summary>
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        /// <summary>
        ///     Additional required inputs used with the primary binding (e.g., chord components or qualifiers).
        /// </summary>
        [JsonPropertyName("with")]
        public List<string>? With { get; set; }
    }
}