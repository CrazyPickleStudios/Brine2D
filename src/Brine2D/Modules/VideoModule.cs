namespace Brine2D;

/// <summary>
///     <para>This module is responsible for decoding, controlling, and streaming video files.</para>
///     <para>It can't draw the videos, see love.graphics.newVideo and Video objects for that.</para>
/// </summary>
public sealed class VideoModule
{
    /// <summary>
    ///     <para>
    ///         Creates a new VideoStream. Currently only Ogg Theora video files are supported. VideoStreams can't draw
    ///         videos, see love.graphics.newVideo for that.
    ///     </para>
    /// </summary>
    /// <remarks></remarks>
    /// <param name="filename">The file path to the Ogg Theora video file.</param>
    /// <returns>
    ///     A new VideoStream.
    /// </returns>
    public VideoStream NewVideoStream(string filename)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     <para>
    ///         Creates a new VideoStream. Currently only Ogg Theora video files are supported. VideoStreams can't draw
    ///         videos, see love.graphics.newVideo for that.
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     Löve only provides the basic functionality provided by libtheora. If your video plays fine in a video player
    ///     but not in Löve, try different encoding options.
    /// </remarks>
    /// <param name="file">The object containing the Ogg Theora video.</param>
    /// <returns>
    ///     A new VideoStream.
    /// </returns>
    public VideoStream NewVideoStream(object file)
    {
        throw new NotImplementedException();
    }
}