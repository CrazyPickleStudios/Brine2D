namespace Brine2D.Assets;

/// <summary>
/// Progress snapshot reported during asset preloading.
/// Value type to avoid per-report heap allocations on the preload path.
/// </summary>
/// <remarks>
/// <c>default(AssetLoadProgress)</c> zeroes all fields (<see cref="LastCompletedAsset"/> will be
/// <see langword="null"/>). Always construct via object initializer.
/// </remarks>
public readonly record struct AssetLoadProgress
{
    /// <summary>Path of the most recently completed asset (success or failure).</summary>
    public string? LastCompletedAsset { get; init; }

    /// <summary>Number of assets that threw during loading.</summary>
    public int FailedAssets { get; init; }

    /// <summary>Number of assets that loaded successfully.</summary>
    public int SucceededAssets { get; init; }

    /// <summary>
    /// Overall completion ratio (0.0–1.0) including both successful and failed assets.
    /// Reaches 1.0 when all assets have been attempted, regardless of outcome.
    /// Use <see cref="SuccessRatio"/> for the success-only ratio.
    /// </summary>
    public float ProgressRatio => TotalAssets > 0 ? (float)(SucceededAssets + FailedAssets) / TotalAssets : 0f;

    /// <summary>
    /// Ratio (0.0–1.0) of successfully loaded assets to total. Will be less than
    /// <see cref="ProgressRatio"/> when any assets have failed.
    /// </summary>
    public float SuccessRatio => TotalAssets > 0 ? (float)SucceededAssets / TotalAssets : 0f;

    /// <summary>Total number of assets in the manifest being preloaded.</summary>
    public int TotalAssets { get; init; }
}