using System.Numerics;
using System.Runtime.CompilerServices;

namespace Brine2D.Rendering.TextureAtlas;

/// <summary>
///     Extension methods for SpriteBatcher to work seamlessly with texture atlases.
///     Import this namespace to enable convenient atlas drawing methods.
/// </summary>
public static class SpriteBatcherAtlasExtensions
{
    /// <param name="batcher">The sprite batcher.</param>
    extension(SpriteBatcher batcher)
    {
        /// <summary>
        ///     Draws a sprite from a texture atlas.
        ///     Automatically uses the correct source rectangle from the atlas region.
        /// </summary>
        /// <param name="region">The atlas region to draw.</param>
        /// <param name="position">World position of the sprite.</param>
        /// <param name="scale">Scale to apply to the sprite (default: 1,1).</param>
        /// <param name="rotation">Rotation in radians (default: 0).</param>
        /// <param name="origin">Origin point for rotation/scaling (0-1 range, default: center).</param>
        /// <param name="tint">Color tint to apply (default: white).</param>
        /// <param name="layer">Rendering layer (default: 0).</param>
        /// <remarks>
        ///     This is the fastest way to draw from an atlas. For best performance in tight loops,
        ///     cache the <see cref="AtlasRegion"/> reference and reuse it.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawAtlasRegion
        (
            AtlasRegion region,
            Vector2 position,
            Vector2? scale = null,
            float rotation = 0f,
            Vector2? origin = null,
            Color? tint = null,
            int layer = 0
        )
        {
            batcher.Draw(
                region.AtlasTexture,
                position,
                region.SourceRect,
                scale ?? Vector2.One,
                rotation,
                origin ?? new Vector2(0.5f, 0.5f),
                tint ?? Color.White,
                layer);
        }

        /// <summary>
        ///     Draws a sprite from a texture atlas by name.
        ///     Convenience method that looks up the region and draws it in one call.
        /// </summary>
        /// <param name="atlas">The texture atlas.</param>
        /// <param name="regionName">Name of the region to draw.</param>
        /// <param name="position">World position of the sprite.</param>
        /// <param name="scale">Scale to apply to the sprite (default: 1,1).</param>
        /// <param name="rotation">Rotation in radians (default: 0).</param>
        /// <param name="origin">Origin point for rotation/scaling (0-1 range, default: center).</param>
        /// <param name="tint">Color tint to apply (default: white).</param>
        /// <param name="layer">Rendering layer (default: 0).</param>
        /// <returns>True if the region was found and drawn; false otherwise.</returns>
        /// <remarks>
        ///     <para>
        ///         This method performs a dictionary lookup by name each time it's called.
        ///         For optimal performance in tight loops, cache the region first:
        ///     </para>
        ///     <code>
        ///         var enemyRegion = atlas.GetRegion("enemy");
        /// 
        ///         foreach (var position in enemyPositions)
        ///         {
        ///             batcher.DrawAtlasRegion(enemyRegion, position);
        ///         }
        ///     </code>
        ///     <para>
        ///         Use this method for one-off draws, prototyping, or when drawing different sprites each frame.
        ///     </para>
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool DrawFromAtlas
        (
            ITextureAtlas atlas,
            string regionName,
            Vector2 position,
            Vector2? scale = null,
            float rotation = 0f,
            Vector2? origin = null,
            Color? tint = null,
            int layer = 0
        )
        {
            if (!atlas.TryGetRegion(regionName, out var region) || region == null)
            {
                return false;
            }

            batcher.DrawAtlasRegion(region, position, scale, rotation, origin, tint, layer);
            return true;
        }

        /// <summary>
        ///     Draws a sprite from a texture atlas collection by name.
        ///     Convenience method that looks up the region automatically across all atlases.
        /// </summary>
        /// <param name="atlasCollection">The texture atlas collection.</param>
        /// <param name="regionName">Name of the region to draw.</param>
        /// <param name="position">World position of the sprite.</param>
        /// <param name="scale">Scale to apply to the sprite (default: 1,1).</param>
        /// <param name="rotation">Rotation in radians (default: 0).</param>
        /// <param name="origin">Origin point for rotation/scaling (0-1 range, default: center).</param>
        /// <param name="tint">Color tint to apply (default: white).</param>
        /// <param name="layer">Rendering layer (default: 0).</param>
        /// <returns>True if the region was found and drawn; false otherwise.</returns>
        /// <remarks>
        ///     <para>
        ///         This method performs a dictionary lookup by name each time it's called.
        ///         For optimal performance in tight loops, cache the region first:
        ///     </para>
        ///     <code>
        ///         var enemyRegion = atlasCollection.GetRegion("enemy");
        /// 
        ///         foreach (var position in enemyPositions)
        ///         {
        ///             batcher.DrawAtlasRegion(enemyRegion, position);
        ///         }
        ///     </code>
        ///     <para>
        ///         Use this method for one-off draws, prototyping, or when drawing different sprites each frame.
        ///     </para>
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool DrawFromAtlas
        (
            ITextureAtlasCollection atlasCollection,
            string regionName,
            Vector2 position,
            Vector2? scale = null,
            float rotation = 0f,
            Vector2? origin = null,
            Color? tint = null,
            int layer = 0
        )
        {
            if (!atlasCollection.TryGetRegion(regionName, out var region) || region == null)
            {
                return false;
            }

            batcher.DrawAtlasRegion(region, position, scale, rotation, origin, tint, layer);
            return true;
        }
    }
}