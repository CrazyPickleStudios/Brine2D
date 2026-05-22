using Brine2D.Core;
using Brine2D.Rendering;

namespace Brine2D.Animation;

/// <summary>
/// Immutable snapshot of an <see cref="AnimationClip"/>'s mutable runtime configuration.
/// Captures <see cref="AnimationClip.PlaybackMode"/>, <see cref="AnimationClip.RepeatCount"/>,
/// <see cref="AnimationClip.TexturePath"/>, <see cref="AnimationClip.Texture"/>,
/// <see cref="AnimationClip.UserData"/>, and <see cref="AnimationClip.ClipTint"/>.
/// Frame lists and events are not captured; use <see cref="AnimationClip.Clone"/> for a full
/// structural copy.
/// </summary>
/// <seealso cref="AnimationClip.CaptureSnapshot"/>
/// <seealso cref="AnimationClip.RestoreSnapshot"/>
public sealed record AnimationClipSnapshot(
    PlaybackMode PlaybackMode,
    int RepeatCount,
    string? TexturePath,
    ITexture? Texture,
    object? UserData,
    Color? ClipTint);