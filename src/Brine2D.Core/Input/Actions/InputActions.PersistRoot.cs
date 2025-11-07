using System.Text.Json.Serialization;

namespace Brine2D.Core.Input.Actions;

public sealed partial class InputActions<TAction, TAxis>
{
    /// <summary>
    ///     Root persistence container for input bindings.
    ///     Holds action and axis binding maps and the schema <see cref="Version" />.
    /// </summary>
    private sealed class PersistRoot
    {
        /// <summary>
        ///     Map of logical action names to their serialized bindings.
        /// </summary>
        [JsonPropertyName("actions")]
        public Dictionary<string, List<PersistActionBinding>>? Actions { get; set; }

        /// <summary>
        ///     Map of logical axis names to their serialized bindings.
        /// </summary>
        [JsonPropertyName("axes")]
        public Dictionary<string, List<PersistAxisBinding>>? Axes { get; set; }

        /// <summary>
        ///     Persistence schema version for migration/compatibility.
        /// </summary>
        [JsonPropertyName("version")]
        public int Version { get; set; }
    }
}