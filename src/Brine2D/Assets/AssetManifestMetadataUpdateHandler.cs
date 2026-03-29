using System.Reflection.Metadata;

[assembly: MetadataUpdateHandler(typeof(Brine2D.Assets.AssetManifestMetadataUpdateHandler))]

namespace Brine2D.Assets;

internal static class AssetManifestMetadataUpdateHandler
{
    internal static void ClearCache(Type[]? _) => AssetManifest.ClearFieldCache();
}