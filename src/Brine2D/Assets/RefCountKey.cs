using Brine2D.Rendering;

namespace Brine2D.Assets;

/// <summary>
/// Value-type ref-counting key that avoids boxing the per-asset-type cache keys
/// (tuples, strings) into <see langword="object"/>.
/// </summary>
internal readonly record struct RefCountKey
{
    /// <summary>The asset category this key identifies.</summary>
    public AssetType Kind { get; }

    /// <summary>Normalized asset path (lowercased, forward-slash separated).</summary>
    public string Path { get; }

    /// <summary>
    /// Type-specific discriminator: <see cref="TextureScaleMode"/> for textures,
    /// point size for fonts, <c>0</c> otherwise.
    /// </summary>
    public int Discriminator { get; }

    private RefCountKey(AssetType kind, string path, int discriminator)
    {
        ArgumentOutOfRangeException.ThrowIfEqual((int)kind, (int)AssetType.None, nameof(kind));
        Kind = kind;
        Path = path;
        Discriminator = discriminator;
    }

    public static RefCountKey ForTexture(string path, TextureScaleMode scale)
        => new(AssetType.Texture, path, (int)scale);

    public static RefCountKey ForSound(string path)
        => new(AssetType.Sound, path, 0);

    public static RefCountKey ForMusic(string path)
        => new(AssetType.Music, path, 0);

    public static RefCountKey ForFont(string path, int size)
        => new(AssetType.Font, path, size);

    public override string ToString() => Kind switch
    {
        AssetType.Texture => $"Texture({Path}, {(TextureScaleMode)Discriminator})",
        AssetType.Font    => $"Font({Path}, size={Discriminator})",
        _                 => $"{Kind}({Path})"
    };
}