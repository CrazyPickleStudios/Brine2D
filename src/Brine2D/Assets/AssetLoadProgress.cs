namespace Brine2D.Assets;

public record AssetLoadProgress
{
    public string CurrentAsset { get; init; } = string.Empty;
    public int FailedAssets { get; init; }
    public int LoadedAssets { get; init; }
    public float ProgressPercent => TotalAssets > 0 ? (float)LoadedAssets / TotalAssets : 0f;
    public int TotalAssets { get; init; }
}