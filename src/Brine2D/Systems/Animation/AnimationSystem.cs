using System.Numerics;
using Brine2D.Animation;
using Brine2D.Core;
using Brine2D.ECS;
using Brine2D.ECS.Query;
using Brine2D.ECS.Systems;
using Brine2D.Systems.Rendering;

namespace Brine2D.Systems.Animation;

/// <summary>
/// Ticks every <see cref="AnimatorComponent"/>: evaluates the state machine, advances the
/// animator, and writes the current frame's data to the entity's <see cref="SpriteComponent"/>
/// (if present). Additional <see cref="AnimationLayer"/>s are then applied in ascending priority
/// order, overwriting whichever properties their <see cref="AnimationLayerMask"/> allows.
/// Cross-fades initiated via <see cref="SpriteAnimator.PlayWithCrossFade"/> render ghost frames
/// via <see cref="SpriteComponent.CrossFadeGhosts"/> so that <see cref="SpriteRenderingSystem"/>
/// can issue one draw call per concurrent blend. Multiple simultaneous fades (e.g. base + layer)
/// are fully supported.
/// </summary>
/// <remarks>
/// When <see cref="Brine2D.Core.GameTime.IsTimeClamped"/> is <c>true</c> (i.e. the game loop
/// detected an unusually large delta), the animation system ticks with zero delta. State machine
/// transitions and the default-state kickoff still execute normally, but no playback time advances
/// and no frame-skip occurs. Animations do not catch up; this is intentional to prevent one-shot
/// events from firing incorrectly on a stalled frame.
/// <para>
/// Blend tree evaluation order: when both <see cref="AnimatorComponent.BlendSelector1D"/> and
/// <see cref="AnimatorComponent.BlendSelector2D"/> are set, only <see cref="AnimatorComponent.BlendSelector1D"/>
/// is evaluated. Set only the tree you want active, or clear the other to swap between them.
/// The same rule applies per-layer.
/// </para>
/// <para>
/// Blend trees are evaluated <em>before</em> the animator is ticked so that a clip selected
/// by the tree on the same frame it changes is advanced correctly with no one-frame lag.
/// </para>
/// </remarks>
public class AnimationSystem : UpdateSystemBase
{
    public override int UpdateOrder => SystemUpdateOrder.Animation;

    private CachedEntityQuery<AnimatorComponent>? _query;
    private IEntityWorld? _queryWorld;

    public override void Update(IEntityWorld world, GameTime gameTime)
    {
        if (_queryWorld != world)
        {
            _query?.Dispose();
            _query = null;
            _queryWorld = world;
        }

        _query ??= world.CreateCachedQuery<AnimatorComponent>().Build();

        var delta = gameTime.IsTimeClamped ? 0f : (float)gameTime.DeltaTime;

        foreach (var (entity, animatorComp) in _query)
        {
            animatorComp.StateMachine.Update(delta);

            if (animatorComp.BlendSelector1D != null)
                animatorComp.BlendSelector1D.Evaluate();
            else
                animatorComp.BlendSelector2D?.Evaluate();

            var animator = animatorComp.Animator;
            bool fadeWasActive = animator.CrossFadeAlpha < 1f;

            animator.Update(delta);

            var sprite = entity.GetComponent<SpriteComponent>();
            if (sprite == null || gameTime.IsTimeClamped)
                continue;

            var preFadeTintAlpha = sprite.Tint.A / 255f;

            sprite.CrossFadeGhosts.Clear();

            float composedAlpha = 1f;

            if (animator.CrossFadeAlpha >= 1f)
            {
                if (fadeWasActive)
                {
                    sprite.Tint = sprite.Tint.WithAlpha(animator.CrossFadeBaseAlpha);
                    preFadeTintAlpha = animator.CrossFadeBaseAlpha;
                }
                animator.CrossFadeBaseAlpha = preFadeTintAlpha;
            }

            var frame = animator.CurrentFrame;
            if (frame != null)
            {
                composedAlpha *= CollectCrossFadeGhost(sprite, animator, AnimationLayerMask.All);
                ApplyFrame(sprite, frame, animator.CurrentAnimation, AnimationLayerMask.All, weight: 1f, AnimationLayerBlendMode.Override);
            }

            foreach (var layer in animatorComp.Layers)
            {
                if (!layer.IsEnabled)
                    continue;

                layer.StateMachine.Update(delta);

                if (layer.BlendSelector1D != null)
                    layer.BlendSelector1D.Evaluate();
                else
                    layer.BlendSelector2D?.Evaluate();

                bool layerFadeWasActive = layer.Animator.CrossFadeAlpha < 1f;
                layer.Animator.Update(delta);

                if (layer.Animator.CrossFadeAlpha >= 1f)
                {
                    if (layerFadeWasActive)
                    {
                        sprite.Tint = sprite.Tint.WithAlpha(layer.Animator.CrossFadeBaseAlpha);
                        preFadeTintAlpha = layer.Animator.CrossFadeBaseAlpha;
                    }
                    layer.Animator.CrossFadeBaseAlpha = preFadeTintAlpha;
                }

                var layerFrame = layer.Animator.CurrentFrame;
                if (layerFrame == null)
                    continue;

                composedAlpha *= CollectCrossFadeGhost(sprite, layer.Animator, layer.Mask);
                ApplyFrame(sprite, layerFrame, layer.Animator.CurrentAnimation, layer.Mask, layer.Weight, layer.BlendMode);
            }

            if (composedAlpha < 1f)
                sprite.Tint = sprite.Tint.WithAlpha(sprite.Tint.A / 255f * composedAlpha);
        }
    }

    /// <summary>
    /// Builds the ghost entry for any active cross-fade on <paramref name="animator"/> and returns
    /// this fader's contribution to the composed incoming-alpha: <c>baseAlpha * crossFadeAlpha</c>
    /// when a fade is active, or <c>1f</c> when no fade is in progress.
    /// </summary>
    /// <remarks>
    /// When the layer mask does not include <see cref="AnimationLayerMask.SourceRect"/> (e.g. a
    /// tint-only layer), the ghost falls back to <see cref="SpriteComponent.SourceRect"/> so that
    /// tint cross-fades render correctly even without a frame rect from the layer.
    /// </remarks>
    private static float CollectCrossFadeGhost(SpriteComponent sprite, SpriteAnimator animator, AnimationLayerMask mask)
    {
        var alpha = animator.CrossFadeAlpha;
        if (alpha >= 1f || animator.CrossFadeOutgoingFrame == null)
            return 1f;

        if ((mask & (AnimationLayerMask.SourceRect | AnimationLayerMask.Tint)) == 0)
            return 1f;

        var outgoingFrame = animator.CrossFadeOutgoingFrame;
        var outgoingClip = animator.CrossFadeOutgoingClip;

        var ghost = new CrossFadeGhost
        {
            BaseAlpha = animator.CrossFadeBaseAlpha,
            SourceRect = (mask & AnimationLayerMask.SourceRect) != 0
                ? outgoingFrame.SourceRect
                : sprite.SourceRect,
            DrawOffset = (mask & AnimationLayerMask.SourceRect) != 0
                ? outgoingFrame.DrawOffset
                : sprite.Offset,
            Origin = (mask & AnimationLayerMask.Origin) != 0
                ? outgoingFrame.Origin
                : sprite.Origin,
            FlipX = (mask & AnimationLayerMask.FlipX) != 0 ? (outgoingFrame.FlipX ?? sprite.FlipX) : sprite.FlipX,
            FlipY = (mask & AnimationLayerMask.FlipY) != 0 ? (outgoingFrame.FlipY ?? sprite.FlipY) : sprite.FlipY,
            Tint = (mask & AnimationLayerMask.Tint) != 0
                ? (outgoingFrame.Tint ?? outgoingClip?.ClipTint ?? sprite.Tint)
                : sprite.Tint,
            Alpha = 1f - alpha,
            Texture = outgoingFrame.Texture ?? outgoingClip?.Texture ?? sprite.Texture,
        };

        ghost.TexturePath = ghost.Texture == null
            ? (outgoingFrame.TexturePath ?? outgoingClip?.TexturePath ?? sprite.TexturePath)
            : null;

        sprite.CrossFadeGhosts.Add(ghost);

        return ghost.BaseAlpha * alpha;
    }

    private static void ApplyFrame(SpriteComponent sprite, SpriteFrame frame, AnimationClip? clip, AnimationLayerMask mask, float weight, AnimationLayerBlendMode blendMode)
    {
        if (weight <= 0f)
            return;

        if (blendMode == AnimationLayerBlendMode.Additive)
        {
            ApplyFrameAdditive(sprite, frame, clip, mask, weight);
            return;
        }

        if ((mask & AnimationLayerMask.SourceRect) != 0 && weight >= 0.5f)
        {
            sprite.SourceRect = frame.SourceRect;
            sprite.Offset = frame.DrawOffset;
        }

        if ((mask & AnimationLayerMask.Origin) != 0)
            sprite.Origin = weight >= 1f ? frame.Origin : Vector2.Lerp(sprite.Origin, frame.Origin, weight);

        if ((mask & AnimationLayerMask.FlipX) != 0 && frame.FlipX.HasValue)
            sprite.FlipX = weight >= 0.5f ? frame.FlipX.Value : sprite.FlipX;

        if ((mask & AnimationLayerMask.FlipY) != 0 && frame.FlipY.HasValue)
            sprite.FlipY = weight >= 0.5f ? frame.FlipY.Value : sprite.FlipY;

        if ((mask & AnimationLayerMask.Tint) != 0)
        {
            var effectiveTint = frame.Tint ?? clip?.ClipTint;
            if (effectiveTint.HasValue)
                sprite.Tint = weight >= 1f ? effectiveTint.Value : Color.Lerp(sprite.Tint, effectiveTint.Value, weight);
        }

        if ((mask & AnimationLayerMask.Texture) != 0 && weight >= 0.5f)
        {
            var texturePath = frame.TexturePath ?? clip?.TexturePath;
            var texture = frame.Texture ?? clip?.Texture;
            if (texturePath != null)
                sprite.TexturePath = texturePath;
            if (texture != null)
                sprite.Texture = texture;
        }
    }

    private static void ApplyFrameAdditive(SpriteComponent sprite, SpriteFrame frame, AnimationClip? clip, AnimationLayerMask mask, float weight)
    {
        if ((mask & AnimationLayerMask.SourceRect) != 0 && weight >= 0.5f)
        {
            sprite.SourceRect = frame.SourceRect;
            sprite.Offset = frame.DrawOffset;
        }

        if ((mask & AnimationLayerMask.Origin) != 0)
            sprite.Origin = weight >= 1f ? frame.Origin : Vector2.Lerp(sprite.Origin, frame.Origin, weight);

        if ((mask & AnimationLayerMask.FlipX) != 0 && frame.FlipX.HasValue)
            sprite.FlipX = weight >= 0.5f ? frame.FlipX.Value : sprite.FlipX;

        if ((mask & AnimationLayerMask.FlipY) != 0 && frame.FlipY.HasValue)
            sprite.FlipY = weight >= 0.5f ? frame.FlipY.Value : sprite.FlipY;

        if ((mask & AnimationLayerMask.Tint) != 0)
        {
            var effectiveTint = frame.Tint ?? clip?.ClipTint;
            if (effectiveTint.HasValue)
            {
                var layerTint = effectiveTint.Value;
                var cur = sprite.Tint;
                sprite.Tint = new Color(
                    (byte)Math.Min(255, cur.R + (int)(layerTint.R * weight)),
                    (byte)Math.Min(255, cur.G + (int)(layerTint.G * weight)),
                    (byte)Math.Min(255, cur.B + (int)(layerTint.B * weight)),
                    (byte)Math.Clamp(cur.A + (int)((layerTint.A - cur.A) * weight), 0, 255));
            }
        }

        if ((mask & AnimationLayerMask.Texture) != 0 && weight >= 0.5f)
        {
            var texturePath = frame.TexturePath ?? clip?.TexturePath;
            var texture = frame.Texture ?? clip?.Texture;
            if (texturePath != null)
                sprite.TexturePath = texturePath;
            if (texture != null)
                sprite.Texture = texture;
        }
    }
}