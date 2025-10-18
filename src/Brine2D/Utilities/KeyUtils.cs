namespace Brine2D;

public static class KeyUtil
{
    public const int KEY_SCANCODE_MASK = 1 << 30;

    public static int LoveKey(int scancode)
    {
        return scancode | KEY_SCANCODE_MASK;
    }
}