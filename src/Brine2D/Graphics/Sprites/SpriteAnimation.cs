using System.Drawing;
using Brine2D.Engine;

namespace Brine2D.Graphics.Sprites;

public sealed class SpriteAnimation
{
    private readonly string[] _frames;
    private readonly double[] _durations;
    private readonly bool _loop;
    private int _index;
    private double _accum;

    public SpriteAnimation(string[] frames, double[] durations, bool loop = true)
    {
        if (frames.Length == 0 || frames.Length != durations.Length)
        {
            throw new ArgumentException("Frames and durations must have equal, non-zero length.");
        }

        _frames = frames;
        _durations = durations;
        _loop = loop;
        _index = 0;
        _accum = 0;
    }

    public string CurrentFrame => _frames[_index];

    public void Update(double deltaSeconds)
    {
        _accum += deltaSeconds;
        var duration = _durations[_index];

        if (_accum >= duration)
        {
            _accum -= duration;
            _index++;
            if (_index >= _frames.Length)
            {
                _index = _loop ? 0 : _frames.Length - 1;
            }
        }
    }

    public void Reset()
    {
        _index = 0;
        _accum = 0;
    }

    public void Draw(SpriteAtlas atlas, RectangleF dst, SpriteBatch batch, Color? tint = null, float rotation = 0f, PointF? origin = null)
    {
        batch.Draw(atlas, _frames[_index], dst, tint, rotation, origin);
    }
}