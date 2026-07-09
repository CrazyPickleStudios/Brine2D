using System.Text.Json;
using System.Text.Json.Serialization;

namespace Brine2D.ECS.Serialization;

/// <summary>
/// Source-generated <see cref="JsonSerializerContext"/> for the built-in snapshot types.
/// Covers <see cref="WorldSnapshot"/> and <see cref="EntitySnapshot"/> (and their constituent
/// collections) without any runtime reflection, making snapshot-level serialization
/// fully compatible with NativeAOT and IL trimming.
/// </summary>
/// <remarks>
/// Used internally by <see cref="AotEntitySerializer"/>. Component types themselves are
/// covered separately through each component's <see cref="System.Text.Json.Serialization.Metadata.JsonTypeInfo{T}"/>
/// supplied via <see cref="ComponentTypeRegistry.Register{T}(System.Text.Json.Serialization.Metadata.JsonTypeInfo{T})"/>.
/// </remarks>
[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(WorldSnapshot))]
[JsonSerializable(typeof(EntitySnapshot))]
[JsonSerializable(typeof(List<EntitySnapshot>))]
[JsonSerializable(typeof(Dictionary<string, JsonElement>))]
internal sealed partial class Brine2DSnapshotContext : JsonSerializerContext { }
