using System.Numerics;

namespace Brine2D.Math;

// TODO: Needs review
public sealed class MathModule : Module
{
    internal MathModule()
    {
        _rng = new RandomGenerator();
    }

    /// <summary>
    ///     Converts a color from 0..255 to 0..1 range.
    /// </summary>
    /// <param name="rb">Red color component in 0..255 range.</param>
    /// <param name="gb">Green color component in 0..255 range.</param>
    /// <param name="bb">Blue color component in 0..255 range.</param>
    /// <param name="ab">Alpha color component in 0..255 range.</param>
    /// <returns>
    ///     <list type="bullet">
    ///         <item>
    ///             <term>r</term><description>Red color component in 0..1 range.</description>
    ///         </item>
    ///         <item>
    ///             <term>g</term><description>Green color component in 0..1 range.</description>
    ///         </item>
    ///         <item>
    ///             <term>b</term><description>Blue color component in 0..1 range.</description>
    ///         </item>
    ///         <item>
    ///             <term>a</term>
    ///             <description>Alpha color component in 0..1 range or nil if alpha is not specified.</description>
    ///         </item>
    ///     </list>
    /// </returns>
    public (double r, double g, double b, double? a) ColorFromBytes(double rb, double gb, double bb, double? ab = null)
    {
        var r = Clamp01(SysMath.Floor(rb + 0.5) / 255);
        var g = Clamp01(SysMath.Floor(gb + 0.5) / 255);
        var b = Clamp01(SysMath.Floor(bb + 0.5) / 255);

        // TODO: It would be more idiomatic to return 1.
        double? a = ab != null ? Clamp01(SysMath.Floor(ab.Value + 0.5) / 255) : null;

        return (r, g, b, a);
    }

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
    public (double rb, double gb, double bb, double? ab) ColorToBytes(double r, double g, double b, double? a = null)
    {
        var rb = SysMath.Floor(Clamp01(r) * 255 + 0.5);
        var gb = SysMath.Floor(Clamp01(g) * 255 + 0.5);
        var bb = SysMath.Floor(Clamp01(b) * 255 + 0.5);

        // TODO: It would be more idiomatic to return 1.
        double? ab = a != null ? SysMath.Floor(Clamp01(a.Value) * 255 + 0.5) : null;

        return (rb, gb, bb, ab);
    }

    private static double Clamp01(double x)
    {
        return SysMath.Clamp(x, 0.0, 1.0);
    }

    /// <summary>
    /// Converts a color from gamma-space (sRGB) to linear-space (RGB). This is useful when doing gamma-correct rendering and you need to do math in linear RGB in the few cases where LÖVE doesn't handle conversions automatically.
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
    public (double lr, double lg, double lb) GammaToLinear(double r, double g, double b)
    {
        return (GammaToLinear(r), GammaToLinear(g), GammaToLinear(b));
    }

    /// <summary>
    /// Converts a color from gamma-space (sRGB) to linear-space (RGB). This is useful when doing gamma-correct rendering and you need to do math in linear RGB in the few cases where LÖVE doesn't handle conversions automatically.
    /// </summary>
    /// <param name="color">An array with the red, green, and blue channels of the sRGB color to convert.</param>
    /// <returns>
    /// <list type="bullet">
    /// <item><term>lr</term><description>The red channel of the converted color in linear RGB space.</description></item>
    /// <item><term>lg</term><description>The green channel of the converted color in linear RGB space.</description></item>
    /// <item><term>lb</term><description>The blue channel of the converted color in linear RGB space.</description></item>
    /// </list>
    /// </returns>
    public (double lr, double lg, double lb) GammaToLinear(double[] color)
    {
        return GammaToLinear(color[0], color[1], color[2]);
    }

    /// <summary>
    /// Converts a color from gamma-space (sRGB) to linear-space (RGB). This is useful when doing gamma-correct rendering and you need to do math in linear RGB in the few cases where LÖVE doesn't handle conversions automatically.
    /// </summary>
    /// <param name="c">The value of a color channel in sRGB space to convert.</param>
    /// <returns>The value of the color channel in linear RGB space.
    /// </returns>
    public double GammaToLinear(double c)
    {
        double v = Clamp01(c);
        if (v <= 0.04045)
            return v / 12.92;
        return SysMath.Pow((v + 0.055) / 1.055, 2.4);
    }

    private readonly RandomGenerator _rng;

    /// <summary>
    /// <para>Gets the seed of the random number generator.</para>
    /// <para>The seed is split into two numbers due to Lua's use of doubles for all number values - doubles can't accurately represent integer  values above 2^53, but the seed can be an integer value up to 2^64.</para>
    /// </summary>
    /// <returns>
    /// <list type="bullet">
    /// <item><term>low</term><description>Integer number representing the lower 32 bits of the random number generator's 64 bit seed value.</description></item>
    /// <item><term>high</term><description>Integer number representing the higher 32 bits of the random number generator's 64 bit seed value.</description></item>
    /// </list>
    /// </returns>
    public (double low, double high) GetRandomSeed()
    {
        return _rng.GetSeed();
    }

    /// <summary>
    /// <para>Gets the current state of the random number generator. This returns an opaque implementation-dependent string which is only useful for later use with love.math.setRandomState or RandomGenerator:setState.</para>
    /// <para>This is different from love.math.getRandomSeed in that getRandomState gets the random number generator's current state, whereas getRandomSeed gets the previously set seed number.</para>
    /// </summary>
    /// <returns>The current state of the random number generator, represented as a string.
    /// </returns>
    public string GetRandomState()
    {
        return _rng.GetState();
    }

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
    public bool IsConvex(IEnumerable<double> vertices) => throw new NotImplementedException();

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
    public bool IsConvex(double x1, double y1, double x2, double y2, double x3, double y3, params double[] vertices) =>
        throw new NotImplementedException();

    /// <summary>
    /// <para>Converts a color from linear-space (RGB) to gamma-space (sRGB). This is useful when storing linear RGB color values in an image, because the linear RGB color space has less precision than sRGB for dark colors, which can result in noticeable color banding when drawing.</para>
    /// <para>In general, colors chosen based on what they look like on-screen are already in gamma-space and should not be double-converted. Colors calculated using math are often in the linear RGB space.</para>
    /// <para>Read more about gamma-correct rendering here, here, and here.</para>
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
    public (double cr, double cg, double cb) LinearToGamma(double lr, double lg, double lb)
    {
        return (LinearToGamma(lr), LinearToGamma(lg), LinearToGamma(lb));
    }

    /// <summary>
    /// <para>Converts a color from linear-space (RGB) to gamma-space (sRGB). This is useful when storing linear RGB color values in an image, because the linear RGB color space has less precision than sRGB for dark colors, which can result in noticeable color banding when drawing.</para>
    /// <para>In general, colors chosen based on what they look like on-screen are already in gamma-space and should not be double-converted. Colors calculated using math are often in the linear RGB space.</para>
    /// <para>Read more about gamma-correct rendering here, here, and here.</para>
    /// </summary>
    /// <param name="color">An array with the red, green, and blue channels of the linear RGB color to convert.</param>
    /// <returns>
    /// <list type="bullet">
    /// <item><term>cr</term><description>The red channel of the converted color in gamma sRGB space.</description></item>
    /// <item><term>cg</term><description>The green channel of the converted color in gamma sRGB space.</description></item>
    /// <item><term>cb</term><description>The blue channel of the converted color in gamma sRGB space.</description></item>
    /// </list>
    /// </returns>
    public (double cr, double cg, double cb) LinearToGamma(double[] color)
    {
        return LinearToGamma(color[0], color[1], color[2]);
    }

    /// <summary>
    /// <para>Converts a color from linear-space (RGB) to gamma-space (sRGB). This is useful when storing linear RGB color values in an image, because the linear RGB color space has less precision than sRGB for dark colors, which can result in noticeable color banding when drawing.</para>
    /// <para>In general, colors chosen based on what they look like on-screen are already in gamma-space and should not be double-converted. Colors calculated using math are often in the linear RGB space.</para>
    /// <para>Read more about gamma-correct rendering here, here, and here.</para>
    /// </summary>
    /// <param name="lc">The value of a color channel in linear RGB space to convert.</param>
    /// <returns>The value of the color channel in gamma sRGB space.
    /// </returns>
    public double LinearToGamma(double lc)
    {
        double v = Clamp01(lc);

        if (v <= 0.0031308)
            return v * 12.92;

        return 1.055 * SysMath.Pow(v, 1.0 / 2.4) - 0.055;
    }

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
    public object NewBezierCurve(double[] vertices)
    {
        var points = new List<Vector2>(vertices.Length / 2);
        for (int i = 0; i < vertices.Count(); i += 2)
        {
            points.Add(new Vector2((float)vertices[i], (float)vertices[i + 1]));
        }

        return points.ToArray();
    }

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
    public object NewBezierCurve(double x1, double y1, double x2, double y2, double x3, double y3,
        params double[] vertices)
    {
        var points = new List<Vector2>(3)
        {
            new Vector2((float)x1, (float)y1),
            new Vector2((float)x2, (float)y2),
            new Vector2((float)x3, (float)y3)
        };

        for (int i = 0; i < vertices.Count(); i += 2)
        {
            points.Add(new Vector2((float)vertices[i], (float)vertices[i + 1]));
        }

        return new BezierCurve(points);
    }

    /// <summary>
    /// Creates a new RandomGenerator object which is completely independent of other RandomGenerator objects and random functions.
    /// </summary>
    /// <returns>The new Random Number Generator object.
    /// </returns>
    public RandomGenerator NewRandomGenerator()
    {
        return new RandomGenerator();
    }

    /// <summary>
    /// Creates a new RandomGenerator object which is completely independent of other RandomGenerator objects and random functions.
    /// </summary>
    /// <param name="seed">The initial seed number to use for this object.</param>
    /// <returns>The new Random Number Generator object.
    /// </returns>
    public RandomGenerator NewRandomGenerator(long seed)
    {
        var result = new RandomGenerator();
        result.SetSeed(seed);
        return result;
    }

    /// <summary>
    /// Creates a new RandomGenerator object which is completely independent of other RandomGenerator objects and random functions.
    /// </summary>
    /// <param name="low">The lower 32 bits of the seed number to use for this object.</param>
    /// <param name="high">The higher 32 bits of the seed number to use for this object.</param>
    /// <returns>The new Random Number Generator object.
    /// </returns>
    public RandomGenerator NewRandomGenerator(int low, int high)
    {
        var result = new RandomGenerator();
        result.SetSeed(low, high);
        return result;
    }

    /// <summary>
    /// Creates a Transform with no transformations applied. Call methods on the returned object to apply transformations.
    /// </summary>
    /// <returns>The new Transform object.
    /// </returns>
    public Transform NewTransform()
    {
        return new Transform();
    }

    /// <summary>
    /// Creates a Transform with the specified transformation applied on creation.
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
    /// <returns>The new Transform object.
    /// </returns>
    public Transform NewTransform
    (
        float x,
        float y,
        float angle = 0,
        float sx = 1,
        float? sy = null,
        float ox = 0,
        float oy = 0,
        float kx = 0,
        float ky = 0
    )
    {
        return new Transform(x, y, angle, sx, sy ?? sx, ox, oy, kx, ky);
    }

    /// <summary>
    /// Generates Simplex noise from 1 dimension.
    /// </summary>
    /// <remarks>The return value might be constant if only integer arguments are used. Avoid solely passing in integers, to get varying return values.</remarks>
    /// <param name="x">The number used to generate the noise value.</param>
    /// <returns>The noise value in the range of [0, 1].
    /// </returns>
    public double Noise(double x) => throw new NotImplementedException();

    /// <summary>
    /// Generates Simplex noise from 2 dimensions.
    /// </summary>
    /// <param name="x">The first value of the 2-dimensional vector used to generate the noise value.</param>
    /// <param name="y">The second value of the 2-dimensional vector used to generate the noise value.</param>
    /// <returns>The noise value in the range of [0, 1].
    /// </returns>
    public double Noise(double x, double y) => throw new NotImplementedException();

    /// <summary>
    /// Generates Perlin noise from 3 dimensions.
    /// </summary>
    /// <param name="x">The first value of the 3-dimensional vector used to generate the noise value.</param>
    /// <param name="y">The second value of the 3-dimensional vector used to generate the noise value.</param>
    /// <param name="z">The third value of the 3-dimensional vector used to generate the noise value.</param>
    /// <returns>The noise value in the range of [0, 1].
    /// </returns>
    public double Noise(double x, double y, double z) => throw new NotImplementedException();

    /// <summary>
    /// Generates Perlin noise from 4 dimensions.
    /// </summary>
    /// <param name="x">The first value of the 4-dimensional vector used to generate the noise value.</param>
    /// <param name="y">The second value of the 4-dimensional vector used to generate the noise value.</param>
    /// <param name="z">The third value of the 4-dimensional vector used to generate the noise value.</param>
    /// <param name="w">The fourth value of the 4-dimensional vector used to generate the noise value.</param>
    /// <returns>The noise value in the range of [0, 1].
    /// </returns>
    public double Noise(double x, double y, double z, double w) => throw new NotImplementedException();

    /// <summary>
    /// Get uniformly distributed pseudo-random real number between 0 inclusive to 1 exclusive.
    /// </summary>
    /// <remarks>
    ///As with RandomGenerator and math.random, this function is not a cryptographically secure random number generator, and thus should not be used for cryptography!	
    /// </remarks>
    /// <returns>The pseudo-random number.</returns>
    public double Random() => throw new NotImplementedException();

    /// <summary>
    /// Get a uniformly distributed pseudo-random integer between 1 inclusive to max inclusive.
    /// </summary>
    /// <remarks>
    ///As with RandomGenerator and math.random, this function is not a cryptographically secure random number generator, and thus should not be used for cryptography!	
    /// </remarks>
    /// <param name="max">The maximum possible value it should return.</param>
    /// <returns>The pseudo-random integer number.
    /// </returns>
    public double Random(int max)
    {
        return _rng.Random(max);
    }

    /// <summary>
    /// Get uniformly distributed pseudo-random integer between min inclusive to max inclusive.
    /// </summary>
    /// <remarks>
    ///As with RandomGenerator and math.random, this function is not a cryptographically secure random number generator, and thus should not be used for cryptography!	
    /// </remarks>
    /// <param name="min">The minimum possible value it should return.</param>
    /// <param name="max">The maximum possible value it should return.</param>
    /// <returns>The pseudo-random integer number.
    /// </returns>
    public double Random(int min, int max)
    {
        return _rng.Random(min, max);
    }

    /// <summary>
    /// Generates a normally-distributed pseudo-random number. This function is seeded at startup, so you generally don't need to seed it yourself.
    /// </summary>
    /// <param name="stddev">Standard deviation of the distribution.</param>
    /// <param name="mean">The mean of the distribution.</param>
    /// <returns>Normally distributed random number with variance (stddev)² and the specified mean.
    /// </returns>
    public double RandomNormal(double stddev = 1, double mean = 0)
    {
        return _rng.RandomNormal(stddev, mean);
    }

    /// <summary>
    /// Sets the seed of the random number generator using the specified integer number. 
    /// </summary>
    /// <remarks>
    /// <para>Due to Lua's use of double-precision floating point numbers, integer values above 2^53 cannot be accurately represented. Use the other variant of the function if you want to use a larger number.</para>
    /// <para>This is called internally at startup, so you generally don't need to call it yourself.</para>
    /// </remarks>
    /// <param name="seed">The integer number with which you want to seed the randomization. Must be within the range of [0, 2^53 - 1].</param>
    public void SetRandomSeed(long seed)
    {
        _rng.SetSeed(seed);
    }

    /// <summary>
    /// Combines two 32-bit integer numbers into a 64-bit integer value and sets the seed of the random number generator using the value.
    /// </summary>
    /// <param name="low">The lower 32 bits of the seed value. Must be within the range of [0, 2^32 - 1].</param>
    /// <param name="high">The higher 32 bits of the seed value. Must be within the range of [0, 2^32 - 1].</param>
    public void SetRandomSeed(int low, int high)
    {
        _rng.SetSeed(low, high);
    }

    /// <summary>
    /// <para>Sets the current state of the random number generator. The value used as an argument for this function is an opaque implementation-dependent string and should only originate from a previous call to love.math.getRandomState.</para>
    /// <para>This is different from love.math.setRandomSeed in that setRandomState directly sets the random number generator's current implementation-dependent state, whereas setRandomSeed gives it a new seed value.</para>
    /// </summary>
    /// <remarks>
    /// The effect of the state string does not depend on the current operating system.
    /// </remarks>
    /// <param name="state">The new state of the random number generator, represented as a string. This should originate from a previous call to .</param>
    public void SetRandomState(string state)
    {
        _rng.SetState(state);
    }

    /// <summary>
    /// <para>Decomposes a simple convex or concave polygon into triangles.</para>
    /// </summary>
    /// <param name="polygon">Polygon to triangulate. Must not intersect itself.</param>
    /// <returns>List of triangles the polygon is composed of, in the form of {{x1, y1, x2, y2, x3, y3}, {x1, y1, x2, y2, x3, y3}, ...}.
    /// </returns>
    public object Triangulate(double[] polygon) => throw new NotImplementedException();

    /// <summary>
    /// <para>Decomposes a simple convex or concave polygon into triangles.</para>
    /// </summary>
    /// <param name="x1">The position of the first vertex of the polygon on the x-axis.</param>
    /// <param name="y1">The position of the first vertex of the polygon on the y-axis.</param>
    /// <param name="x2">The position of the second vertex of the polygon on the x-axis.</param>
    /// <param name="y2">The position of the second vertex of the polygon on the y-axis.</param>
    /// <param name="x3">The position of the third vertex of the polygon on the x-axis.</param>
    /// <param name="y3">The position of the third vertex of the polygon on the y-axis.</param>
    /// <returns>List of triangles the polygon is composed of, in the form of {{x1, y1, x2, y2, x3, y3}, {x1, y1, x2, y2, x3, y3}, ...}.
    /// </returns>
    public double[] Triangulate(double x1, double y1, double x2, double y2, double x3, double y3, params double[] vertices) =>
        throw new NotImplementedException();
}