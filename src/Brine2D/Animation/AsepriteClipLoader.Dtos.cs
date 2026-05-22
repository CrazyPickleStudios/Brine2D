namespace Brine2D.Animation;

public partial class AsepriteClipLoader
{
    private enum AsepriteDirection { Forward, Reverse, PingPong, PingPongReverse }

    private sealed class AsepriteFrame
    {
        public AsepriteRect Frame { get; set; } = new();
        public int Duration { get; set; }
        public string? Data { get; set; }
        public AsepriteRect? SpriteSourceSize { get; set; }
        public AsepriteSize? SourceSize { get; set; }
    }

    private sealed class AsepriteRect
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int W { get; set; }
        public int H { get; set; }
    }

    private sealed class AsepriteSize
    {
        public int W { get; set; }
        public int H { get; set; }
    }

    private sealed class AsepriteMeta
    {
        public List<AsepriteFrameTag>? FrameTags { get; set; }
        public List<AsepriteSlice>? Slices { get; set; }
    }

    private sealed class AsepriteFrameTag
    {
        public string Name { get; set; } = string.Empty;
        public int From { get; set; }
        public int To { get; set; }
        public string? Direction { get; set; }
        public int Repeat { get; set; }
        public string? Data { get; set; }
        public string? Color { get; set; }
    }

    private sealed class AsepriteSlice
    {
        public string Name { get; set; } = string.Empty;
        public List<AsepriteSliceKey>? Keys { get; set; }
    }

    private sealed class AsepriteSliceKey
    {
        public int Frame { get; set; }
        public AsepriteRect Bounds { get; set; } = new();
        public (float x, float y)? Pivot { get; set; }
    }
}