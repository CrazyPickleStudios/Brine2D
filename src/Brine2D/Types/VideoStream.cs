namespace Brine2D;

/// <summary>
///     An object which decodes, streams, and controls Videos.
/// </summary>
public sealed class VideoStream : Object
{
    internal VideoStream()
    {
    }

    /// <summary>
    ///     Gets filename of video stream.
    /// </summary>
    /// <returns>
    ///     The filename of video stream.
    /// </returns>
    public string GetFilename()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     Gets whatever the video stream is playing.
    /// </summary>
    /// <returns>
    ///     Whatever video stream is playing.
    /// </returns>
    /// TODO: Is the return here right?
    public bool IsPlaying()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     Pauses video stream.
    /// </summary>
    public void Pause()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     Plays video stream.
    /// </summary>
    public void Play()
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public override bool Release()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     Rewinds video stream.
    /// </summary>
    /// <remarks>
    ///     Synonym to VideoStream:seek(0).
    /// </remarks>
    public void Rewind()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     Sets the current playback position of the Video.
    /// </summary>
    /// <param name="offset">The time in seconds since the beginning of the Video.</param>
    public void Seek(double offset)
    {
        throw new NotImplementedException();
    }

    // TODO: This is labeled as TODO on the wiki.
    public void SetSync(Source source)
    {
        throw new NotImplementedException();
    }

    // TODO: This is labeled as TODO on the wiki.
    public void SetSync(VideoStream source)
    {
        throw new NotImplementedException();
    }

    // TODO: This is labeled as TODO on the wiki.
    public void SetSync()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     Gets the current playback position of video stream.
    /// </summary>
    /// <returns>
    ///     The seconds since the beginning of the video.
    /// </returns>
    public double Tell()
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public override string Type()
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public override bool TypeOf(string name)
    {
        throw new NotImplementedException();
    }
}