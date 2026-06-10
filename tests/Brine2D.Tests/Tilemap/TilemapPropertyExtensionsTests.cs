using Brine2D.Core;
using Brine2D.Tilemap;
using FluentAssertions;
using Xunit;

namespace Brine2D.Tests.Tilemap;

public sealed class TilemapPropertyExtensionsTests
{
    [Fact]
    public void GetColor_RgbHex_ReturnsParsedColor()
    {
        var props = new Dictionary<string, string> { ["tint"] = "#ff8000" };

        var color = props.GetColor("tint");

        color.R.Should().Be(255);
        color.G.Should().Be(128);
        color.B.Should().Be(0);
        color.A.Should().Be(255);
    }

    [Fact]
    public void GetColor_ArgbHex_ReturnsParsedColorWithAlpha()
    {
        var props = new Dictionary<string, string> { ["tint"] = "#80ff0000" };

        var color = props.GetColor("tint");

        color.A.Should().Be(0x80);
        color.R.Should().Be(255);
        color.G.Should().Be(0);
        color.B.Should().Be(0);
    }

    [Fact]
    public void GetColor_KeyAbsent_ReturnsDefaultValue()
    {
        var props = new Dictionary<string, string>();

        var color = props.GetColor("missing", new Color(1, 2, 3));

        color.R.Should().Be(1);
        color.G.Should().Be(2);
        color.B.Should().Be(3);
    }

    [Fact]
    public void GetColor_InvalidHex_ReturnsDefaultValue()
    {
        var props = new Dictionary<string, string> { ["color"] = "notacolor" };

        var color = props.GetColor("color", new Color(5, 5, 5));

        color.R.Should().Be(5);
    }

    [Fact]
    public void GetColor_EmptyString_ReturnsDefaultValue()
    {
        var props = new Dictionary<string, string> { ["color"] = "" };

        var color = props.GetColor("color", new Color(7, 7, 7));

        color.R.Should().Be(7);
    }

    [Fact]
    public void GetColor_WhiteRgb_ReturnsParsedWhite()
    {
        var props = new Dictionary<string, string> { ["tint"] = "#ffffff" };

        var color = props.GetColor("tint");

        color.R.Should().Be(255);
        color.G.Should().Be(255);
        color.B.Should().Be(255);
    }

    [Fact]
    public void GetColor_WhiteArgb_ReturnsParsedWhiteWithAlpha()
    {
        var props = new Dictionary<string, string> { ["tint"] = "#ffffffff" };

        var color = props.GetColor("tint");

        color.R.Should().Be(255);
        color.G.Should().Be(255);
        color.B.Should().Be(255);
        color.A.Should().Be(255);
    }
}
