using Brine2D.Audio;
using Brine2D.Rendering;
using System.Collections.Concurrent;
using System.Reflection;

namespace Brine2D.Assets;

/// <summary>
/// Base class for typed asset manifests.
/// Subclass it, declare your assets as <see cref="AssetRef{T}"/> fields, then pass
/// the manifest to <see cref="IAssetLoader.PreloadAsync"/> in your scene's OnLoadAsync.
/// </summary>
/// <example>
/// <code>
/// public class LevelAssets : AssetManifest
/// {
///     public readonly AssetRef&lt;ITexture&gt;    Tileset   = Texture("assets/images/tileset.png", TextureScaleMode.Nearest);
///     public readonly AssetRef&lt;ITexture&gt;    Player    = Texture("assets/images/player.png");
///     public readonly AssetRef&lt;ISoundEffect&gt; Jump      = Sound("assets/audio/jump.wav");
///     public readonly AssetRef&lt;IMusic&gt;       BgMusic   = Music("assets/audio/level1.ogg");
///     public readonly AssetRef&lt;Font&gt;         HUDFont   = Font("assets/fonts/hud.ttf", size: 20);
/// }
/// </code>
/// </example>
public abstract class AssetManifest
{
    // FieldInfo list cached per concrete type; reflection paid once, not per frame
    private static readonly ConcurrentDictionary<Type, FieldInfo[]> _fieldCache = new();
    
    /// <summary>Declares a texture asset.</summary>
    protected static AssetRef<ITexture> Texture(
        string path,
        TextureScaleMode scale = TextureScaleMode.Linear)
        => new(path, (loader, ct) => loader.GetOrLoadTextureAsync(path, ct));

    /// <summary>Declares a sound effect asset (short, in-memory).</summary>
    protected static AssetRef<ISoundEffect> Sound(string path)
        => new(path, (loader, ct) => loader.GetOrLoadSoundAsync(path, ct));

    /// <summary>Declares a music asset (streamed).</summary>
    protected static AssetRef<IMusic> Music(string path)
        => new(path, (loader, ct) => loader.GetOrLoadMusicAsync(path, ct));

    /// <summary>
    /// Declares a font asset.
    /// Size is part of the identity: Font("ui.ttf", 16) and Font("ui.ttf", 32)
    /// are two independent cached entries.
    /// </summary>
    protected static AssetRef<Font> Font(string path, int size)
        => new($"{path}:{size}", (loader, ct) => loader.GetOrLoadFontAsync(path, size, ct));
    
    /// <summary>
    /// Returns all <see cref="AssetRef{T}"/> fields declared on this manifest instance.
    /// FieldInfo is cached per concrete type; values are read from <c>this</c> each call.
    /// </summary>
    internal IReadOnlyList<IAssetRef> GetAll()
    {
        var fields = _fieldCache.GetOrAdd(GetType(), static t =>
            t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
             .Where(f => f.FieldType.IsGenericType &&
                         f.FieldType.GetGenericTypeDefinition() == typeof(AssetRef<>))
             .ToArray());

        var refs = new List<IAssetRef>(fields.Length);
        foreach (var field in fields)
        {
            if (field.GetValue(this) is IAssetRef assetRef)
                refs.Add(assetRef);
        }
        return refs;
    }
}