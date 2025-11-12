using Brine2D.Core.Graphics.Text;

namespace Brine2D.Core.Graphics.Text;

public static class FontExtensions
{
    public static void PrewarmDigits(this IFont font) => font.PrewarmRange('0', '9');
    public static void PrewarmUpper(this IFont font) => font.PrewarmRange('A', 'Z');
    public static void PrewarmLower(this IFont font) => font.PrewarmRange('a', 'z');
    public static void PrewarmCommonPunctuation(this IFont font) =>
        font.Prewarm(".,;:!?\"'()[]{}+-*/=<>&|^%$#@~`");
    public static void PrewarmCommonUI(this IFont font)
    {
        font.PrewarmDigits();
        font.PrewarmUpper();
        font.PrewarmLower();
        font.PrewarmCommonPunctuation();
        font.Prewarm(" "); // space
    }
}