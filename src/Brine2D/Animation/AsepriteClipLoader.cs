using System.Runtime.CompilerServices;
using Brine2D.Rendering;
using Microsoft.Extensions.Logging;

namespace Brine2D.Animation;

/// <summary>
/// Loads <see cref="AnimationClip"/>s from Aseprite JSON sprite-sheet exports.
/// Supports both <c>JSON Array</c> and <c>JSON Hash</c> data formats
/// (<c>File → Export Sprite Sheet</c>).
/// </summary>
/// <remarks>
/// <para>
/// Each <c>meta.frameTags</c> entry becomes one <see cref="AnimationClip"/> named after the tag.
/// Frame durations are taken directly from the per-frame <c>duration</c> field (milliseconds).
/// </para>
/// <para>
/// Tag directions map as follows:
/// <list type="table">
///   <item><term>forward</term><description><see cref="PlaybackMode.Loop"/></description></item>
///   <item><term>reverse</term><description>
///     <see cref="PlaybackMode.Loop"/>; frames stored in reverse order so normal forward playback
///     produces the correct visual.
///   </description></item>
///   <item><term>pingpong</term><description><see cref="PlaybackMode.PingPong"/></description></item>
///   <item><term>pingpong_reverse</term><description>
///     <see cref="PlaybackMode.PingPong"/>; <see cref="PingPongReverseTag"/> stored in
///     <see cref="AnimationClip.UserData"/>. Use <see cref="ConfigureAnimator"/> to apply automatically.
///   </description></item>
/// </list>
/// </para>
/// <para>
/// If the JSON has no <c>frameTags</c> (or an empty array), a single clip named after
/// <paramref name="defaultClipName"/> is created from all frames.
/// </para>
/// <para>
/// Aseprite slice data (<c>meta.slices</c>) is mapped to <see cref="SpriteFrame"/> hit boxes.
/// Any slice whose name matches <see cref="HitBoxSliceName"/> (default <c>"hitbox"</c>) is
/// stored as the primary <see cref="SpriteFrame.HitBox"/>. All other slices are stored as
/// additional named boxes via <see cref="SpriteFrame.SetHitBox"/> using the slice name as the
/// key, so slices named <c>"hurtbox"</c>, <c>"head"</c>, etc. are preserved automatically.
/// The Aseprite per-slice <c>pivot</c> field, when present, is mapped to
/// <see cref="SpriteFrame.Origin"/> (normalised by the original canvas frame size from
/// <c>sourceSize</c>, so trimmed exports produce correct pivot positions).
/// </para>
/// <para>
/// When Aseprite's <b>Trim</b> export option is active, frames carry <c>spriteSourceSize</c>
/// (the sub-rect of the original canvas covered by trimmed pixels) and <c>sourceSize</c>
/// (the original untrimmed canvas size). The loader maps the trim offset to
/// <see cref="SpriteFrame.DrawOffset"/> so that trimmed sprites render at the correct canvas
/// position without any changes to the atlas <c>frame</c> rect.
/// </para>
/// <para>
/// The tag-level <c>repeat</c> field is mapped to <see cref="AnimationClip.RepeatCount"/>
/// on <see cref="PlaybackMode.Loop"/> and <see cref="PlaybackMode.PingPong"/> clips.
/// </para>
/// <para>
/// The tag-level <c>data</c> field (Aseprite <b>Tag User Data</b>) is forwarded to
/// <see cref="AnimationClip.UserData"/> as a <c>string</c>, unless <see cref="AnimationClip.UserData"/>
/// has already been set by the direction logic (e.g. <see cref="PingPongReverseTag"/>).
/// </para>
/// <para>
/// The per-frame <c>data</c> field (Aseprite <b>Frame User Data</b>, set via
/// <c>right-click → Frame Properties</c>) is mapped to <see cref="SpriteFrame.UserData"/> as a
/// <c>string</c>. Frames without a <c>data</c> field, or with an empty string, leave
/// <see cref="SpriteFrame.UserData"/> as <c>null</c>.
/// </para>
/// </remarks>
public partial class AsepriteClipLoader
{
    /// <summary>
    /// Value placed in <see cref="AnimationClip.UserData"/> for <c>pingpong_reverse</c> tags.
    /// </summary>
    public const string PingPongReverseTag = "pingpong_reverse";

    /// <summary>
    /// Name of the Aseprite slice that is mapped to <see cref="SpriteFrame.HitBox"/>.
    /// Defaults to <c>"hitbox"</c>. All other slices are still loaded as named boxes.
    /// </summary>
    public string HitBoxSliceName { get; set; } = "hitbox";

    /// <summary>
    /// Name of the Aseprite slice whose pivot data is mapped to <see cref="SpriteFrame.Origin"/>.
    /// Defaults to <c>"hitbox"</c> (same slice as <see cref="HitBoxSliceName"/>), which matches
    /// the common Aseprite workflow of placing a pivot on the primary hitbox slice.
    /// Set to a different slice name when you use a dedicated pivot slice (e.g. <c>"pivot"</c>).
    /// When multiple slices carry pivot data for the same frame, only the slice matching this
    /// name is used; others are logged as warnings and ignored.
    /// </summary>
    public string PivotSliceName { get; set; } = "hitbox";

    private readonly ILogger<AsepriteClipLoader>? _logger;

    // Tracks which animators have already had the pingpong_reverse callbacks subscribed
    // for a given clip, so ConfigureAnimator is safe to call multiple times and safe when
    // the same clip instance is shared across multiple animators.
    // Outer CWT: keyed weakly on AnimationClip — entry disappears when clip is collected.
    // Inner CWT: keyed weakly on SpriteAnimator — entry disappears when animator is collected,
    //            preventing the table from rooting disposed animators.
    private static readonly ConditionalWeakTable<AnimationClip, ConditionalWeakTable<SpriteAnimator, object?>> _pingPongSubscriptions = new();

    public AsepriteClipLoader(ILogger<AsepriteClipLoader>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Loads all tagged animation clips from an Aseprite JSON export file.
    /// </summary>
    public async Task<IReadOnlyList<AnimationClip>> LoadAsync(
        string path,
        ITexture? texture = null,
        string? texturePath = null,
        string defaultClipName = "default",
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Aseprite JSON file not found: {path}", path);

        _logger?.LogInformation("Loading Aseprite clips from: {Path}", path);
        var json = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
        return ParseJson(json, path, texture, texturePath, defaultClipName);
    }

    /// <summary>
    /// Loads all tagged animation clips from a <see cref="Stream"/> containing Aseprite JSON.
    /// </summary>
    public async Task<IReadOnlyList<AnimationClip>> LoadAsync(
        Stream stream,
        ITexture? texture = null,
        string? texturePath = null,
        string sourceName = "<stream>",
        string defaultClipName = "default",
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        _logger?.LogInformation("Loading Aseprite clips from stream: {Source}", sourceName);
        using var reader = new StreamReader(stream, leaveOpen: true);
        var json = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        return ParseJson(json, sourceName, texture, texturePath, defaultClipName);
    }

    /// <summary>
    /// Synchronously loads all tagged animation clips from an Aseprite JSON export file.
    /// </summary>
    public IReadOnlyList<AnimationClip> Load(
        string path,
        ITexture? texture = null,
        string? texturePath = null,
        string defaultClipName = "default")
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Aseprite JSON file not found: {path}", path);

        _logger?.LogInformation("Loading Aseprite clips from: {Path}", path);
        var json = File.ReadAllText(path);
        return ParseJson(json, path, texture, texturePath, defaultClipName);
    }

    /// <summary>
    /// Synchronously loads all tagged animation clips from a <see cref="Stream"/> containing Aseprite JSON.
    /// </summary>
    public IReadOnlyList<AnimationClip> Load(
        Stream stream,
        ITexture? texture = null,
        string? texturePath = null,
        string sourceName = "<stream>",
        string defaultClipName = "default")
    {
        ArgumentNullException.ThrowIfNull(stream);
        _logger?.LogInformation("Loading Aseprite clips from stream: {Source}", sourceName);
        using var reader = new StreamReader(stream, leaveOpen: true);
        var json = reader.ReadToEnd();
        return ParseJson(json, sourceName, texture, texturePath, defaultClipName);
    }

    /// <summary>
    /// Loads all tagged animation clips from a UTF-8 encoded Aseprite JSON payload in memory.
    /// </summary>
    public Task<IReadOnlyList<AnimationClip>> LoadAsync(
        ReadOnlyMemory<byte> utf8Json,
        ITexture? texture = null,
        string? texturePath = null,
        string sourceName = "<memory>",
        string defaultClipName = "default",
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _logger?.LogInformation("Loading Aseprite clips from memory: {Source}", sourceName);
        var json = System.Text.Encoding.UTF8.GetString(utf8Json.Span);
        return Task.FromResult(ParseJson(json, sourceName, texture, texturePath, defaultClipName));
    }

    /// <summary>
    /// Synchronous overload for <see cref="LoadAsync(ReadOnlyMemory{byte},ITexture?,string?,string,string,CancellationToken)"/>.
    /// Parses a UTF-8 encoded Aseprite JSON payload already in memory.
    /// </summary>
    public IReadOnlyList<AnimationClip> Load(
        ReadOnlyMemory<byte> utf8Json,
        ITexture? texture = null,
        string? texturePath = null,
        string sourceName = "<memory>",
        string defaultClipName = "default")
    {
        _logger?.LogInformation("Loading Aseprite clips from memory: {Source}", sourceName);
        var json = System.Text.Encoding.UTF8.GetString(utf8Json.Span);
        return ParseJson(json, sourceName, texture, texturePath, defaultClipName);
    }

    /// <summary>
    /// Parses Aseprite JSON directly from a string.
    /// </summary>
    public IReadOnlyList<AnimationClip> ParseJson(
        string json,
        string sourceName = "<json>",
        ITexture? texture = null,
        string? texturePath = null,
        string defaultClipName = "default")
    {
        var frames = TryParseArrayFormat(json, sourceName)
            ?? TryParseHashFormat(json, sourceName)
            ?? throw new InvalidOperationException(
                $"Failed to parse Aseprite JSON: {sourceName}. Expected JSON Array or JSON Hash format.");

        var meta = TryParseMeta(json);
        var tags = meta?.FrameTags;
        var slices = meta?.Slices;

        var hitBoxesPerFrame = BuildNamedHitBoxLookup(slices, frames.Count);
        var pivotPerFrame = BuildPivotLookup(slices, frames.Count);

        var clips = new List<AnimationClip>();

        if (tags == null || tags.Count == 0)
        {
            var clip = BuildClip(defaultClipName, frames, 0, frames.Count - 1,
                AsepriteDirection.Forward, repeatCount: 0, texture, texturePath,
                hitBoxesPerFrame, pivotPerFrame, tagData: null, tagColor: null);
            clips.Add(clip);
            _logger?.LogDebug("No frameTags found; created single clip '{Name}' with {Count} frames",
                defaultClipName, clip.Frames.Count);
        }
        else
        {
            foreach (var tag in tags)
            {
                if (string.IsNullOrWhiteSpace(tag.Name))
                {
                    _logger?.LogWarning("Skipping unnamed frameTag at [{From},{To}]", tag.From, tag.To);
                    continue;
                }

                var direction = ParseDirection(tag.Direction);
                var clip = BuildClip(tag.Name, frames, tag.From, tag.To, direction,
                    tag.Repeat, texture, texturePath, hitBoxesPerFrame, pivotPerFrame,
                    tag.Data, tag.Color);
                clips.Add(clip);
                _logger?.LogDebug("Loaded clip '{Name}': {Count} frames, mode={Mode}, repeat={Repeat}",
                    clip.Name, clip.Frames.Count, clip.PlaybackMode, clip.RepeatCount);
            }
        }

        return clips;
    }

    /// <summary>
    /// Adds all clips in <paramref name="clips"/> to <paramref name="animator"/> and applies
    /// direction hints stored in <see cref="AnimationClip.UserData"/>.
    /// A clip with <c>UserData == <see cref="PingPongReverseTag"/></c> automatically toggles
    /// <see cref="SpriteAnimator.Reversed"/> on enter/exit, scoped per (clip, animator) pair.
    /// Safe to call multiple times — callbacks are only subscribed once per pair.
    /// </summary>
    public void ConfigureAnimator(SpriteAnimator animator, IReadOnlyList<AnimationClip> clips)
    {
        ArgumentNullException.ThrowIfNull(animator);
        ArgumentNullException.ThrowIfNull(clips);

        foreach (var clip in clips)
        {
            if (string.Equals(clip.UserData as string, PingPongReverseTag, StringComparison.Ordinal))
            {
                var subscribers = _pingPongSubscriptions.GetOrCreateValue(clip);
                if (!subscribers.TryGetValue(animator, out _))
                {
                    subscribers.Add(animator, null);

                    var weakAnimator = new WeakReference<SpriteAnimator>(animator);
                    var weakClip = new WeakReference<AnimationClip>(clip);

                    animator.OnAnimationStart += started =>
                    {
                        if (!weakAnimator.TryGetTarget(out var a)) return;
                        if (!weakClip.TryGetTarget(out var c)) return;
                        if (ReferenceEquals(started, c))
                            a.Reversed = true;
                        else if (a.Reversed)
                            a.Reversed = false;
                    };

                    animator.OnStopped += _ =>
                    {
                        if (!weakAnimator.TryGetTarget(out var a)) return;
                        if (a.Reversed)
                            a.Reversed = false;
                    };
                }
            }
            animator.AddAnimation(clip);
        }
    }
}