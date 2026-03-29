using Brine2D.Audio;
using Brine2D.Rendering;
using System.Diagnostics.CodeAnalysis;
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
///     public readonly AssetRef&lt;IFont&gt;        HUDFont   = Font("assets/fonts/hud.ttf", size: 20);
/// }
/// </code>
/// </example>
public abstract class AssetManifest
{
    private static readonly Lock _fieldCacheLock = new();
    private static readonly Dictionary<Type, FieldInfo[]> _fieldCache = new();
    private IReadOnlyList<IAssetRef>? _cachedRefs;
    private RefCountKey[]? _cachedUniqueKeys;

    /// <summary>Declares a texture asset.</summary>
    protected static AssetRef<ITexture> Texture(
        string path,
        TextureScaleMode scale = TextureScaleMode.Linear)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        path = AssetCache.NormalizePath(path);
        return new(path, RefCountKey.ForTexture(path, scale),
            (cache, ct) => cache.GetOrLoadTextureAsync(path, scale, ct));
    }

    /// <summary>Declares a sound effect asset (short, in-memory).</summary>
    protected static AssetRef<ISoundEffect> Sound(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        path = AssetCache.NormalizePath(path);
        return new(path, RefCountKey.ForSound(path),
            (cache, ct) => cache.GetOrLoadSoundAsync(path, ct));
    }

    /// <summary>Declares a music asset (streamed).</summary>
    protected static AssetRef<IMusic> Music(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        path = AssetCache.NormalizePath(path);
        return new(path, RefCountKey.ForMusic(path),
            (cache, ct) => cache.GetOrLoadMusicAsync(path, ct));
    }

    /// <summary>
    /// Declares a font asset.
    /// Size is part of the identity: Font("ui.ttf", 16) and Font("ui.ttf", 32)
    /// are two independent cached entries.
    /// </summary>
    protected static AssetRef<IFont> Font(string path, int size)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        path = AssetCache.NormalizePath(path);
        return new(path, RefCountKey.ForFont(path, size),
            (cache, ct) => cache.GetOrLoadFontAsync(path, size, ct));
    }

    /// <summary>
    /// Returns all <see cref="AssetRef{T}"/> fields declared on this manifest instance.
    /// FieldInfo is cached per concrete type; resolved refs are cached per instance.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2070",
        Justification = "AssetManifest subclasses are application types instantiated by user code; " +
                        "their fields and properties are not subject to member-level trimming.")]
    internal IReadOnlyList<IAssetRef> GetAll()
    {
        var cached = Volatile.Read(ref _cachedRefs);
        if (cached is not null) return cached;

        var fields = GetCachedFields(GetType());

        var refs = new IAssetRef[fields.Length];
        for (var i = 0; i < fields.Length; i++)
        {
            if (fields[i].GetValue(this) is IAssetRef assetRef)
                refs[i] = assetRef;
            else
                throw new InvalidOperationException(
                    $"AssetRef field '{fields[i].Name}' on {GetType().Name} is null. " +
                    "Initialize it with Texture(), Sound(), Music(), or Font().");
        }

        return Interlocked.CompareExchange(ref _cachedRefs, refs, null) ?? refs;
    }

    /// <summary>
    /// Returns the deduplicated set of <see cref="RefCountKey"/>s for this manifest.
    /// Computed once from <see cref="GetAll"/> and cached for the lifetime of the instance,
    /// so <see cref="AssetCache.PreloadAsync"/> and <see cref="AssetCache.Unload"/> can
    /// iterate unique keys without allocating on every call.
    /// </summary>
    internal RefCountKey[] GetUniqueKeys()
    {
        var keys = Volatile.Read(ref _cachedUniqueKeys);
        if (keys is not null) return keys;

        var refs = GetAll();
        var set = new HashSet<RefCountKey>(refs.Count);
        for (var i = 0; i < refs.Count; i++)
            set.Add(refs[i].RefKey);

        keys = [.. set];
        return Interlocked.CompareExchange(ref _cachedUniqueKeys, keys, null) ?? keys;
    }

    private static FieldInfo[] GetCachedFields(
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicFields |
            DynamicallyAccessedMemberTypes.NonPublicFields |
            DynamicallyAccessedMemberTypes.PublicProperties |
            DynamicallyAccessedMemberTypes.NonPublicProperties)]
        Type manifestType)
    {
        lock (_fieldCacheLock)
        {
            if (_fieldCache.TryGetValue(manifestType, out var existing))
                return existing;

            List<string>? strayProps = null;
            List<FieldInfo>? assetRefFields = null;
            List<string>? mutableNames = null;

            var current = manifestType;
            while (current is not null && current != typeof(AssetManifest))
            {
                var properties = current.GetProperties(
                    BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.Instance | BindingFlags.DeclaredOnly);

                foreach (var prop in properties)
                {
                    if (prop.PropertyType.IsGenericType &&
                        prop.PropertyType.GetGenericTypeDefinition() == typeof(AssetRef<>))
                    {
                        (strayProps ??= []).Add(prop.Name);
                    }
                }

                var fields = current.GetFields(
                    BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.Instance | BindingFlags.DeclaredOnly);

                foreach (var field in fields)
                {
                    if (!field.FieldType.IsGenericType ||
                        field.FieldType.GetGenericTypeDefinition() != typeof(AssetRef<>))
                        continue;

                    (assetRefFields ??= []).Add(field);

                    if (!field.IsInitOnly)
                        (mutableNames ??= []).Add(field.Name);
                }

                current = current.BaseType;
            }

            if (strayProps is not null)
            {
                throw new InvalidOperationException(
                    $"AssetManifest subclass '{manifestType.Name}' declares AssetRef<> as " +
                    $"{(strayProps.Count == 1 ? "a property" : "properties")} " +
                    $"({string.Join(", ", strayProps)}). Use readonly fields instead — " +
                    "properties allocate a new AssetRef on each access and are not discovered by PreloadAsync.");
            }

            if (mutableNames is not null)
            {
                throw new InvalidOperationException(
                    $"AssetManifest subclass '{manifestType.Name}' declares non-readonly AssetRef<> " +
                    $"{(mutableNames.Count == 1 ? "field" : "fields")} " +
                    $"({string.Join(", ", mutableNames)}). Mark them readonly to prevent " +
                    "accidental reassignment after preloading.");
            }

            FieldInfo[] result = assetRefFields is not null ? [.. assetRefFields] : [];
            _fieldCache[manifestType] = result;
            return result;
        }
    }

    internal static void ClearFieldCache()
    {
        lock (_fieldCacheLock)
        {
            _fieldCache.Clear();
        }
    }
}