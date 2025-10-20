namespace Brine2D.Common;

internal struct Vector2
{
    public float X, Y;

    public Vector2()

    {
        X = 0.0f;
        Y = 0.0f;
    }

    public Vector2(float x, float y)
    {
        this.X = x;
        this.Y = y;
    }

    public Vector2(Vector2 v)
    {
        X = v.X;
        Y = v.Y;
    }

    public float GetLength()
    {
        return MathF.Sqrt(X * X + Y * Y);
    }

    public float GetLengthSquare()
    {
        return X * X + Y * Y;
    }

    public void Normalize(float length = 1.0f)
    {
        var lengthCurrent = GetLength();
        
        if (lengthCurrent > 0)
        {
            var m = length / lengthCurrent;
            X *= m;
            Y *= m;
        }
    }

    public Vector2 GetNormal()
    {
        return new Vector2(-Y, X);
    }

    private Vector2 GetNormal(float scale)
    {
        return new Vector2(-Y * scale, X * scale);
    }

    public static float Dot(Vector2 a, Vector2 b)
    {
        return a.X * b.X + a.Y * b.Y;
    }

    public static float Cross(Vector2 a, Vector2 b)
    {
        return a.X * b.Y - a.Y * b.X;
    }

    public static Vector2 operator +(Vector2 a, Vector2 b)
    {
        return new Vector2(a.X + b.X, a.Y + b.Y);
    }

    public static Vector2 operator -(Vector2 a, Vector2 b)
    {
        return new Vector2(a.X - b.X, a.Y - b.Y);
    }

    public static Vector2 operator *(Vector2 v, float s)
    {
        return new Vector2(v.X * s, v.Y * s);
    }

    public static Vector2 operator /(Vector2 v, float s)
    {
        var invs = 1.0f / s;
        return new Vector2(v.X * invs, v.Y * invs);
    }

    public static Vector2 operator -(Vector2 v)
    {
        return new Vector2(-v.X, -v.Y);
    }

    public static bool operator ==(Vector2 a, Vector2 b)
    {
        return a.X == b.X && a.Y == b.Y;
    }

    public static bool operator !=(Vector2 a, Vector2 b)
    {
        return a.X != b.X || a.Y != b.Y;
    }
} // Vector2