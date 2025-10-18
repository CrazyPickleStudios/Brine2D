using System;

namespace Brine2D;

// TODO: Needs review
public sealed class MathModule
{
    /// <summary>
    /// <para>Converts a color from 0..255 to 0..1 range.</para>
    /// </summary>
    /// <param name="rb">Red color component in 0..255 range.</param>
    /// <param name="gb">Green color component in 0..255 range.</param>
    /// <param name="bb">Blue color component in 0..255 range.</param>
    /// <param name="ab">Alpha color component in 0..255 range.</param>
    /// <returns>
    /// <list type="bullet">
    /// <item><term>r</term><description>Red color component in 0..1 range.</description></item>
    /// <item><term>g</term><description>Green color component in 0..1 range.</description></item>
    /// <item><term>b</term><description>Blue color component in 0..1 range.</description></item>
    /// <item><term>a</term><description>Alpha color component in 0..1 range or nil if alpha is not specified.</description></item>
    /// </list>
    /// </returns>
    // TODO: public (double r, double g, double b, double a) ColorFromBytes(double rb, double gb, double bb, double ab = null) => throw new NotImplementedException();

    /// <summary>
    /// <para>Converts a color from 0..255 to 0..1 range.</para>
    /// </summary>
    // TODO: public void ColorFromBytes() => throw new NotImplementedException();

    /// <summary>
    /// <para>Converts a color from 0..255 to 0..1 range.</para>
    /// </summary>
    public void ColorFromBytes() => throw new NotImplementedException();

    /// <summary>
    /// <para>Converts a color from 0..1 to 0..255 range.</para>
    /// </summary>
    /// <param name="r">Red color component.</param>
    /// <param name="g">Green color component.</param>
    /// <param name="b">Blue color component.</param>
    /// <param name="a">Alpha color component.</param>
    /// <returns>
    /// <list type="bullet">
    /// <item><term>rb</term><description>Red color component in 0..255 range.</description></item>
    /// <item><term>gb</term><description>Green color component in 0..255 range.</description></item>
    /// <item><term>bb</term><description>Blue color component in 0..255 range.</description></item>
    /// <item><term>ab</term><description>Alpha color component in 0..255 range or nil if alpha is not specified.</description></item>
    /// </list>
    /// </returns>
    // TODO: public (double rb, double gb, double bb, double ab) ColorToBytes(double r, double g, double b, double a = null) => throw new NotImplementedException();

    /// <summary>
    /// <para>Converts a color from 0..1 to 0..255 range.</para>
    /// </summary>
    public void ColorToBytes() => throw new NotImplementedException();

    /// <summary>
    /// <para>Compresses a string or data using a specific compression algorithm.</para>
    /// </summary>
    /// <param name="rawstring">The raw (un-compressed) string to compress.</param>
    /// <param name="format">The format to use when compressing the string.</param>
    /// <param name="level">The level of compression to use, between 0 and 9. -1 indicates the default level. The meaning of this argument depends on the compression format being used.</param>
    /// <returns>
    /// <list type="bullet">
    /// <item><term>compressedData</term><description>A new Data object containing the compressed version of the string.</description></item>
    /// </list>
    /// </returns>
    // TODO: public object Compress(string rawstring, object format = "lz4", double level = -1) => throw new NotImplementedException();

    /// <summary>
    /// <para>Compresses a string or data using a specific compression algorithm.</para>
    /// </summary>
    /// <param name="data">A Data object containing the raw (un-compressed) data to compress.</param>
    /// <param name="format">The format to use when compressing the data.</param>
    /// <param name="level">The level of compression to use, between 0 and 9. -1 indicates the default level. The meaning of this argument depends on the compression format being used.</param>
    /// <returns>
    /// <list type="bullet">
    /// <item><term>compressedData</term><description>A new Data object containing the compressed version of the raw data.</description></item>
    /// </list>
    /// </returns>
    // TODO: public object Compress(object data, object format = "lz4", double level = -1) => throw new NotImplementedException();

    /// <summary>
    /// <para>Decompresses a CompressedData or previously compressed string or Data object.</para>
    /// </summary>
    /// <param name="compressedData">The compressed data to decompress.</param>
    /// <returns>
    /// <list type="bullet">
    /// <item><term>rawstring</term><description>A string containing the raw decompressed data.</description></item>
    /// </list>
    /// </returns>
    public string Decompress(object compressedData) => throw new NotImplementedException();

/// <summary>
        /// <para>Decompresses a CompressedData or previously compressed string or Data object.</para>
        /// </summary>
        /// <param name="compressedstring">A string containing data previously compressed with .</param>
        /// <param name="format">The format that was used to compress the given string.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>rawstring</term><description>A string containing the raw decompressed data.</description></item>
        /// </list>
        /// </returns>
    public string Decompress(string compressedstring, object format) => throw new NotImplementedException();

/// <summary>
        /// <para>Decompresses a CompressedData or previously compressed string or Data object.</para>
        /// </summary>
        /// <param name="data">A Data object containing data previously compressed with .</param>
        /// <param name="format">The format that was used to compress the given data.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>rawstring</term><description>A string containing the raw decompressed data.</description></item>
        /// </list>
        /// </returns>
    public string Decompress(object data, object format) => throw new NotImplementedException();

/// <summary>
        /// <para>Converts a color from gamma-space (sRGB) to linear-space (RGB). This is useful when doing gamma-correct rendering and you need to do math in linear RGB in the few cases where LÖVE doesn't handle conversions automatically.</para>
        /// <para>Read more about gamma-correct rendering here, here, and here.</para>
        /// <para>In versions prior to 11.0, color component values were within the range of 0 to 255 instead of 0 to 1.</para>
        /// </summary>
        /// <param name="r">The red channel of the sRGB color to convert.</param>
        /// <param name="g">The green channel of the sRGB color to convert.</param>
        /// <param name="b">The blue channel of the sRGB color to convert.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>lr</term><description>The red channel of the converted color in linear RGB space.</description></item>
        /// <item><term>lg</term><description>The green channel of the converted color in linear RGB space.</description></item>
        /// <item><term>lb</term><description>The blue channel of the converted color in linear RGB space.</description></item>
        /// </list>
        /// </returns>
    public (double lr, double lg, double lb) GammaToLinear(double r, double g, double b) => throw new NotImplementedException();

/// <summary>
        /// <para>Converts a color from gamma-space (sRGB) to linear-space (RGB). This is useful when doing gamma-correct rendering and you need to do math in linear RGB in the few cases where LÖVE doesn't handle conversions automatically.</para>
        /// <para>Read more about gamma-correct rendering here, here, and here.</para>
        /// <para>In versions prior to 11.0, color component values were within the range of 0 to 255 instead of 0 to 1.</para>
        /// </summary>
        /// <param name="color">An array with the red, green, and blue channels of the sRGB color to convert.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>lr</term><description>The red channel of the converted color in linear RGB space.</description></item>
        /// <item><term>lg</term><description>The green channel of the converted color in linear RGB space.</description></item>
        /// <item><term>lb</term><description>The blue channel of the converted color in linear RGB space.</description></item>
        /// </list>
        /// </returns>
    public (double lr, double lg, double lb) GammaToLinear(object color) => throw new NotImplementedException();

/// <summary>
        /// <para>Converts a color from gamma-space (sRGB) to linear-space (RGB). This is useful when doing gamma-correct rendering and you need to do math in linear RGB in the few cases where LÖVE doesn't handle conversions automatically.</para>
        /// <para>Read more about gamma-correct rendering here, here, and here.</para>
        /// <para>In versions prior to 11.0, color component values were within the range of 0 to 255 instead of 0 to 1.</para>
        /// </summary>
        /// <param name="c">The value of a color channel in sRGB space to convert.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>lc</term><description>The value of the color channel in linear RGB space.</description></item>
        /// </list>
        /// </returns>
    public double GammaToLinear(double c) => throw new NotImplementedException();

    /// <summary>
    /// <para>Converts a color from gamma-space (sRGB) to linear-space (RGB). This is useful when doing gamma-correct rendering and you need to do math in linear RGB in the few cases where LÖVE doesn't handle conversions automatically.</para>
    /// <para>Read more about gamma-correct rendering here, here, and here.</para>
    /// <para>In versions prior to 11.0, color component values were within the range of 0 to 255 instead of 0 to 1.</para>
    /// </summary>
    // TODO: public void GammaToLinear() => throw new NotImplementedException();

    /// <summary>
    /// <para>Gets the seed of the random number generator.</para>
    /// <para>The seed is split into two numbers due to Lua's use of doubles for all number values - doubles can't accurately represent integer  values above 2^53, but the seed can be an integer value up to 2^64.</para>
    /// </summary>
    /// <param name="low">Integer number representing the lower 32 bits of the random number generator's 64 bit seed value.</param>
    /// <param name="high">Integer number representing the higher 32 bits of the random number generator's 64 bit seed value.</param>
    /// <returns>
    /// <list type="bullet">
    /// <item><term>low</term><description>Integer number representing the lower 32 bits of the random number generator's 64 bit seed value.</description></item>
    /// <item><term>high</term><description>Integer number representing the higher 32 bits of the random number generator's 64 bit seed value.</description></item>
    /// </list>
    /// </returns>
    public (double low, double high) GetRandomSeed(double low, double high) => throw new NotImplementedException();

/// <summary>
        /// <para>Gets the current state of the random number generator. This returns an opaque implementation-dependent string which is only useful for later use with love.math.setRandomState or RandomGenerator:setState.</para>
        /// <para>This is different from love.math.getRandomSeed in that getRandomState gets the random number generator's current state, whereas getRandomSeed gets the previously set seed number.</para>
        /// </summary>
        /// <param name="state">The current state of the random number generator, represented as a string.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>state</term><description>The current state of the random number generator, represented as a string.</description></item>
        /// </list>
        /// </returns>
    public string GetRandomState(string state) => throw new NotImplementedException();

/// <summary>
        /// <para>Checks whether a polygon is convex.</para>
        /// <para>PolygonShapes in love.physics, some forms of Meshes, and polygons drawn with love.graphics.polygon must be simple convex polygons.</para>
        /// </summary>
        /// <param name="vertices">The vertices of the polygon as a table in the form of .</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>convex</term><description>Whether the given polygon is convex.</description></item>
        /// </list>
        /// </returns>
    public bool IsConvex(object vertices) => throw new NotImplementedException();

/// <summary>
        /// <para>Checks whether a polygon is convex.</para>
        /// <para>PolygonShapes in love.physics, some forms of Meshes, and polygons drawn with love.graphics.polygon must be simple convex polygons.</para>
        /// </summary>
        /// <param name="x1">The position of the first vertex of the polygon on the x-axis.</param>
        /// <param name="y1">The position of the first vertex of the polygon on the y-axis.</param>
        /// <param name="x2">The position of the second vertex of the polygon on the x-axis.</param>
        /// <param name="y2">The position of the second vertex of the polygon on the y-axis.</param>
        /// <param name="x3">The position of the third vertex of the polygon on the x-axis.</param>
        /// <param name="y3">The position of the third vertex of the polygon on the y-axis.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>convex</term><description>Whether the given polygon is convex.</description></item>
        /// </list>
        /// </returns>
    public bool IsConvex(double x1, double y1, double x2, double y2, double x3, double y3) => throw new NotImplementedException();

/// <summary>
        /// <para>Converts a color from linear-space (RGB) to gamma-space (sRGB). This is useful when storing linear RGB color values in an image, because the linear RGB color space has less precision than sRGB for dark colors, which can result in noticeable color banding when drawing.</para>
        /// <para>In general, colors chosen based on what they look like on-screen are already in gamma-space and should not be double-converted. Colors calculated using math are often in the linear RGB space.</para>
        /// <para>Read more about gamma-correct rendering here, here, and here.</para>
        /// <para>In versions prior to 11.0, color component values were within the range of 0 to 255 instead of 0 to 1.</para>
        /// </summary>
        /// <param name="lr">The red channel of the linear RGB color to convert.</param>
        /// <param name="lg">The green channel of the linear RGB color to convert.</param>
        /// <param name="lb">The blue channel of the linear RGB color to convert.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>cr</term><description>The red channel of the converted color in gamma sRGB space.</description></item>
        /// <item><term>cg</term><description>The green channel of the converted color in gamma sRGB space.</description></item>
        /// <item><term>cb</term><description>The blue channel of the converted color in gamma sRGB space.</description></item>
        /// </list>
        /// </returns>
    public (double cr, double cg, double cb) LinearToGamma(double lr, double lg, double lb) => throw new NotImplementedException();

/// <summary>
        /// <para>Converts a color from linear-space (RGB) to gamma-space (sRGB). This is useful when storing linear RGB color values in an image, because the linear RGB color space has less precision than sRGB for dark colors, which can result in noticeable color banding when drawing.</para>
        /// <para>In general, colors chosen based on what they look like on-screen are already in gamma-space and should not be double-converted. Colors calculated using math are often in the linear RGB space.</para>
        /// <para>Read more about gamma-correct rendering here, here, and here.</para>
        /// <para>In versions prior to 11.0, color component values were within the range of 0 to 255 instead of 0 to 1.</para>
        /// </summary>
        /// <param name="color">An array with the red, green, and blue channels of the linear RGB color to convert.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>cr</term><description>The red channel of the converted color in gamma sRGB space.</description></item>
        /// <item><term>cg</term><description>The green channel of the converted color in gamma sRGB space.</description></item>
        /// <item><term>cb</term><description>The blue channel of the converted color in gamma sRGB space.</description></item>
        /// </list>
        /// </returns>
    public (double cr, double cg, double cb) LinearToGamma(object color) => throw new NotImplementedException();

/// <summary>
        /// <para>Converts a color from linear-space (RGB) to gamma-space (sRGB). This is useful when storing linear RGB color values in an image, because the linear RGB color space has less precision than sRGB for dark colors, which can result in noticeable color banding when drawing.</para>
        /// <para>In general, colors chosen based on what they look like on-screen are already in gamma-space and should not be double-converted. Colors calculated using math are often in the linear RGB space.</para>
        /// <para>Read more about gamma-correct rendering here, here, and here.</para>
        /// <para>In versions prior to 11.0, color component values were within the range of 0 to 255 instead of 0 to 1.</para>
        /// </summary>
        /// <param name="lc">The value of a color channel in linear RGB space to convert.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>c</term><description>The value of the color channel in gamma sRGB space.</description></item>
        /// </list>
        /// </returns>
    public double LinearToGamma(double lc) => throw new NotImplementedException();

/// <summary>
        /// <para>Converts a color from linear-space (RGB) to gamma-space (sRGB). This is useful when storing linear RGB color values in an image, because the linear RGB color space has less precision than sRGB for dark colors, which can result in noticeable color banding when drawing.</para>
        /// <para>In general, colors chosen based on what they look like on-screen are already in gamma-space and should not be double-converted. Colors calculated using math are often in the linear RGB space.</para>
        /// <para>Read more about gamma-correct rendering here, here, and here.</para>
        /// <para>In versions prior to 11.0, color component values were within the range of 0 to 255 instead of 0 to 1.</para>
        /// </summary>
    public void GammaToLinear() => throw new NotImplementedException();

/// <summary>
        /// <para>Creates a new BezierCurve object.</para>
        /// <para>The number of vertices in the control polygon determines the degree of the curve, e.g. three vertices define a quadratic (degree 2) Bézier curve, four vertices define a cubic (degree 3) Bézier curve, etc.</para>
        /// </summary>
        /// <param name="vertices">The vertices of the control polygon as a table in the form of .</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>curve</term><description>A Bézier curve object.</description></item>
        /// </list>
        /// </returns>
    public object NewBezierCurve(object vertices) => throw new NotImplementedException();

/// <summary>
        /// <para>Creates a new BezierCurve object.</para>
        /// <para>The number of vertices in the control polygon determines the degree of the curve, e.g. three vertices define a quadratic (degree 2) Bézier curve, four vertices define a cubic (degree 3) Bézier curve, etc.</para>
        /// </summary>
        /// <param name="x1">The position of the first vertex of the control polygon on the x-axis.</param>
        /// <param name="y1">The position of the first vertex of the control polygon on the y-axis.</param>
        /// <param name="x2">The position of the second vertex of the control polygon on the x-axis.</param>
        /// <param name="y2">The position of the second vertex of the control polygon on the y-axis.</param>
        /// <param name="x3">The position of the third vertex of the control polygon on the x-axis.</param>
        /// <param name="y3">The position of the third vertex of the control polygon on the y-axis.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>curve</term><description>A Bézier curve object.</description></item>
        /// </list>
        /// </returns>
    public object NewBezierCurve(double x1, double y1, double x2, double y2, double x3, double y3) => throw new NotImplementedException();

/// <summary>
        /// <para>Creates a new RandomGenerator object which is completely independent of other RandomGenerator objects and random functions.</para>
        /// </summary>
        /// <param name="rng">The new Random Number Generator object.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>rng</term><description>The new Random Number Generator object.</description></item>
        /// </list>
        /// </returns>
    public object NewRandomGenerator(object rng) => throw new NotImplementedException();

/// <summary>
        /// <para>Creates a new RandomGenerator object which is completely independent of other RandomGenerator objects and random functions.</para>
        /// </summary>
        /// <param name="seed">The initial seed number to use for this object.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>rng</term><description>The new Random Number Generator object.</description></item>
        /// </list>
        /// </returns>
    public object NewRandomGenerator(double seed) => throw new NotImplementedException();

/// <summary>
        /// <para>Creates a new RandomGenerator object which is completely independent of other RandomGenerator objects and random functions.</para>
        /// </summary>
        /// <param name="low">The lower 32 bits of the seed number to use for this object.</param>
        /// <param name="high">The higher 32 bits of the seed number to use for this object.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>rng</term><description>The new Random Number Generator object.</description></item>
        /// </list>
        /// </returns>
    public object NewRandomGenerator(double low, double high) => throw new NotImplementedException();

/// <summary>
        /// <para>Creates a new RandomGenerator object which is completely independent of other RandomGenerator objects and random functions.</para>
        /// </summary>
    public void NewRandomGenerator() => throw new NotImplementedException();

/// <summary>
        /// <para>Creates a new Transform object.</para>
        /// </summary>
        /// <param name="transform">The new Transform object.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>transform</term><description>The new Transform object.</description></item>
        /// </list>
        /// </returns>
    public object NewTransform(object transform) => throw new NotImplementedException();

    /// <summary>
    /// <para>Creates a new Transform object.</para>
    /// </summary>
    /// <param name="x">The position of the new Transform on the x-axis.</param>
    /// <param name="y">The position of the new Transform on the y-axis.</param>
    /// <param name="angle">The orientation of the new Transform in radians.</param>
    /// <param name="sx">Scale factor on the x-axis.</param>
    /// <param name="sy">Scale factor on the y-axis.</param>
    /// <param name="ox">Origin offset on the x-axis.</param>
    /// <param name="oy">Origin offset on the y-axis.</param>
    /// <param name="kx">Shearing / skew factor on the x-axis.</param>
    /// <param name="ky">Shearing / skew factor on the y-axis.</param>
    /// <returns>
    /// <list type="bullet">
    /// <item><term>transform</term><description>The new Transform object.</description></item>
    /// </list>
    /// </returns>
    // TODO: public object NewTransform(double x, double y, double angle = 0, double sx = 1, double sy = sx, double ox = 0, double oy = 0, double kx = 0, double ky = 0) => throw new NotImplementedException();

    /// <summary>
    /// <para>Generates a Simplex or Perlin noise value in 1-4 dimensions. The return value will always be the same, given the same arguments.</para>
    /// <para>Simplex noise is closely related to Perlin noise. It is widely used for procedural content generation.</para>
    /// <para>There are many webpages which discuss Perlin and Simplex noise in detail.</para>
    /// </summary>
    /// <param name="x">The number used to generate the noise value.</param>
    /// <returns>
    /// <list type="bullet">
    /// <item><term>value</term><description>The noise value in the range of [0, 1].</description></item>
    /// </list>
    /// </returns>
    public double Noise(double x) => throw new NotImplementedException();

/// <summary>
        /// <para>Generates a Simplex or Perlin noise value in 1-4 dimensions. The return value will always be the same, given the same arguments.</para>
        /// <para>Simplex noise is closely related to Perlin noise. It is widely used for procedural content generation.</para>
        /// <para>There are many webpages which discuss Perlin and Simplex noise in detail.</para>
        /// </summary>
        /// <param name="x">The first value of the 2-dimensional vector used to generate the noise value.</param>
        /// <param name="y">The second value of the 2-dimensional vector used to generate the noise value.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>value</term><description>The noise value in the range of [0, 1].</description></item>
        /// </list>
        /// </returns>
    public double Noise(double x, double y) => throw new NotImplementedException();

/// <summary>
        /// <para>Generates a Simplex or Perlin noise value in 1-4 dimensions. The return value will always be the same, given the same arguments.</para>
        /// <para>Simplex noise is closely related to Perlin noise. It is widely used for procedural content generation.</para>
        /// <para>There are many webpages which discuss Perlin and Simplex noise in detail.</para>
        /// </summary>
        /// <param name="x">The first value of the 3-dimensional vector used to generate the noise value.</param>
        /// <param name="y">The second value of the 3-dimensional vector used to generate the noise value.</param>
        /// <param name="z">The third value of the 3-dimensional vector used to generate the noise value.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>value</term><description>The noise value in the range of [0, 1].</description></item>
        /// </list>
        /// </returns>
    public double Noise(double x, double y, double z) => throw new NotImplementedException();

/// <summary>
        /// <para>Generates a Simplex or Perlin noise value in 1-4 dimensions. The return value will always be the same, given the same arguments.</para>
        /// <para>Simplex noise is closely related to Perlin noise. It is widely used for procedural content generation.</para>
        /// <para>There are many webpages which discuss Perlin and Simplex noise in detail.</para>
        /// </summary>
        /// <param name="x">The first value of the 4-dimensional vector used to generate the noise value.</param>
        /// <param name="y">The second value of the 4-dimensional vector used to generate the noise value.</param>
        /// <param name="z">The third value of the 4-dimensional vector used to generate the noise value.</param>
        /// <param name="w">The fourth value of the 4-dimensional vector used to generate the noise value.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>value</term><description>The noise value in the range of [0, 1].</description></item>
        /// </list>
        /// </returns>
    public double Noise(double x, double y, double z, double w) => throw new NotImplementedException();

    /// <summary>
    /// <para>Generates a Simplex or Perlin noise value in 1-4 dimensions. The return value will always be the same, given the same arguments.</para>
    /// <para>Simplex noise is closely related to Perlin noise. It is widely used for procedural content generation.</para>
    /// <para>There are many webpages which discuss Perlin and Simplex noise in detail.</para>
    /// </summary>
    // TODO: public void Random() => throw new NotImplementedException();

    /// <summary>
    /// <para>Generates a pseudo-random number in a platform independent manner. This function is seeded at startup, so you generally don't need to seed it yourself.</para>
    /// </summary>
    /// <param name="number">The pseudo-random number.</param>
    /// <returns>
    /// <list type="bullet">
    /// <item><term>number</term><description>The pseudo-random number.</description></item>
    /// </list>
    /// </returns>
    // TODO:  public double Random(double number) => throw new NotImplementedException();

    /// <summary>
    /// <para>Generates a pseudo-random number in a platform independent manner. This function is seeded at startup, so you generally don't need to seed it yourself.</para>
    /// </summary>
    /// <param name="max">The maximum possible value it should return.</param>
    /// <returns>
    /// <list type="bullet">
    /// <item><term>number</term><description>The pseudo-random integer number.</description></item>
    /// </list>
    /// </returns>
    // TODO: public double Random(double max) => throw new NotImplementedException();

    /// <summary>
    /// <para>Generates a pseudo-random number in a platform independent manner. This function is seeded at startup, so you generally don't need to seed it yourself.</para>
    /// </summary>
    /// <param name="min">The minimum possible value it should return.</param>
    /// <param name="max">The maximum possible value it should return.</param>
    /// <returns>
    /// <list type="bullet">
    /// <item><term>number</term><description>The pseudo-random integer number.</description></item>
    /// </list>
    /// </returns>
    public double Random(double min, double max) => throw new NotImplementedException();

/// <summary>
        /// <para>Generates a pseudo-random number in a platform independent manner. This function is seeded at startup, so you generally don't need to seed it yourself.</para>
        /// </summary>
    public void Random() => throw new NotImplementedException();

/// <summary>
        /// <para>Generates a normally-distributed pseudo-random number. This function is seeded at startup, so you generally don't need to seed it yourself.</para>
        /// <para>While a typical uniform distribution looks like this:</para>
        /// <para>A typical normal distribution looks like this (note the values aggregating at the center, and the shape of a bell curve):</para>
        /// </summary>
        /// <param name="stddev">Standard deviation of the distribution.</param>
        /// <param name="mean">The mean of the distribution.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>number</term><description>Normally distributed random number with variance (stddev)² and the specified mean.</description></item>
        /// </list>
        /// </returns>
    public double RandomNormal(double stddev = 1, double mean = 0) => throw new NotImplementedException();

/// <summary>
        /// <para>Sets the seed of the random number generator using the specified integer number. This is called internally at startup, so you generally don't need to call it yourself.</para>
        /// </summary>
        /// <param name="seed">The integer number with which you want to seed the randomization. Must be within the range of [0, 2^53 - 1].</param>
    public void SetRandomSeed(double seed) => throw new NotImplementedException();

/// <summary>
        /// <para>Sets the seed of the random number generator using the specified integer number. This is called internally at startup, so you generally don't need to call it yourself.</para>
        /// </summary>
        /// <param name="low">The lower 32 bits of the seed value. Must be within the range of [0, 2^32 - 1].</param>
        /// <param name="high">The higher 32 bits of the seed value. Must be within the range of [0, 2^32 - 1].</param>
    public void SetRandomSeed(double low, double high) => throw new NotImplementedException();

/// <summary>
        /// <para>Sets the seed of the random number generator using the specified integer number. This is called internally at startup, so you generally don't need to call it yourself.</para>
        /// </summary>
    public void SetRandomSeed() => throw new NotImplementedException();

/// <summary>
        /// <para>Sets the current state of the random number generator. The value used as an argument for this function is an opaque implementation-dependent string and should only originate from a previous call to love.math.getRandomState.</para>
        /// <para>This is different from love.math.setRandomSeed in that setRandomState directly sets the random number generator's current implementation-dependent state, whereas setRandomSeed gives it a new seed value.</para>
        /// </summary>
        /// <param name="state">The new state of the random number generator, represented as a string. This should originate from a previous call to .</param>
    public void SetRandomState(string state) => throw new NotImplementedException();

/// <summary>
        /// <para>Decomposes a simple convex or concave polygon into triangles.</para>
        /// </summary>
        /// <param name="polygon">Polygon to triangulate. Must not intersect itself.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>triangles</term><description>List of triangles the polygon is composed of, in the form of .</description></item>
        /// </list>
        /// </returns>
    public object Triangulate(object polygon) => throw new NotImplementedException();

/// <summary>
        /// <para>Decomposes a simple convex or concave polygon into triangles.</para>
        /// </summary>
        /// <param name="x1">The position of the first vertex of the polygon on the x-axis.</param>
        /// <param name="y1">The position of the first vertex of the polygon on the y-axis.</param>
        /// <param name="x2">The position of the second vertex of the polygon on the x-axis.</param>
        /// <param name="y2">The position of the second vertex of the polygon on the y-axis.</param>
        /// <param name="x3">The position of the third vertex of the polygon on the x-axis.</param>
        /// <param name="y3">The position of the third vertex of the polygon on the y-axis.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>triangles</term><description>List of triangles the polygon is composed of, in the form of .</description></item>
        /// </list>
        /// </returns>
    public object Triangulate(double x1, double y1, double x2, double y2, double x3, double y3) => throw new NotImplementedException();

}
