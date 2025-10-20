using Brine2D.Common;

namespace Brine2D.Math;

/// <summary>
///     <para>A Bézier curve object that can evaluate and render Bézier curves of arbitrary degree.</para>
///     <para>For more information on Bézier curves check this great article on Wikipedia.</para>
/// </summary>
// TODO: Requires Review
public class BezierCurve : Object
{
    private readonly List<Vector2> _controlPoints;

    internal BezierCurve(List<Vector2> controlPoints)
    {
        _controlPoints = controlPoints;
    }

    /// <summary>
    ///     <para>
    ///         Evaluate Bézier curve at parameter t. The parameter must be between 0 and 1 (inclusive) and there must be at
    ///         least two control points for the curve.
    ///     </para>
    ///     <para>
    ///         This function can be used to move objects along paths or tween parameters. However it should not be used to
    ///         render the curve - see BezierCurve:render for that purpose.
    ///     </para>
    /// </summary>
    /// <param name="t">Where to evaluate the curve. Must be between 0 and 1.</param>
    /// <returns>
    ///     <list type="bullet">
    ///         <item>
    ///             <term>x</term><description>x coordinate of the curve at parameter t.</description>
    ///         </item>
    ///         <item>
    ///             <term>y</term><description>y coordinate of the curve at parameter t.</description>
    ///         </item>
    ///     </list>
    /// </returns>
    public (double x, double y) Evaluate(double t)
    {
        if (t is < 0 or > 1)
        {
            throw new Exception("Invalid evaluation parameter: must be between 0 and 1");
        }

        if (_controlPoints.Count < 2)
        {
            throw new Exception("Invalid Bezier curve: Not enough control points.");
        }

        var points = new List<Vector2>(_controlPoints);

        for (var step = 1; step < _controlPoints.Count; ++step)
        for (var i = 0; i < _controlPoints.Count - step; ++i)
        {
            points[i] = points[i] * (1 - (float)t) + points[i + 1] * (float)t;
        }

        return (points[0].X, points[0].Y);
    }

    /// <summary>
    ///     Get coordinates of the i-th control point. Indices start with 1.
    /// </summary>
    /// <param name="i">Index of the control point.</param>
    /// <returns>
    ///     <list type="bullet">
    ///         <item>
    ///             <term>x</term><description>Position of the control point along the x axis.</description>
    ///         </item>
    ///         <item>
    ///             <term>y</term><description>Position of the control point along the y axis.</description>
    ///         </item>
    ///     </list>
    /// </returns>
    public (double x, double y) GetControlPoint(int i)
    {
        if (i > 0)
        {
            i--;
        }

        if (_controlPoints.Count == 0)
        {
            throw new Exception("Curve contains no control points.");
        }

        while (i < 0)
        {
            i += _controlPoints.Count;
        }

        while (i >= _controlPoints.Count)
        {
            i -= _controlPoints.Count;
        }

        return (_controlPoints[i].X, _controlPoints[i].Y);
    }

    /// <summary>
    ///     Get coordinates of the i-th control point. Indices start with 1.
    /// </summary>
    /// <returns>The number of control points.</returns>
    public int GetControlPointCount()
    {
        return _controlPoints.Count;
    }

    /// <summary>
    ///     Get degree of the Bézier curve. The degree is equal to number-of-control-points - 1.
    /// </summary>
    /// <returns>
    ///     Degree of the Bézier curve.
    /// </returns>
    public int GetDegree()
    {
        return _controlPoints.Count - 1;
    }

    /// <summary>
    ///     <para>Get the derivative of the Bézier curve.</para>
    ///     <para>
    ///         This function can be used to rotate sprites moving along a curve in the direction of the movement and compute
    ///         the direction perpendicular to the curve at some parameter t.
    ///     </para>
    /// </summary>
    /// <returns>
    ///     The derivative curve.
    /// </returns>
    public BezierCurve GetDerivative()
    {
        if (GetDegree() < 1)
        {
            throw new Exception("Cannot derive a curve of degree < 1.");
        }

        var forwardDifferences = new List<Vector2>(_controlPoints.Count - 1);

        for (var k = 0; k < _controlPoints.Count - 1; ++k)
        {
            forwardDifferences.Add(new Vector2());
        }

        float degree = GetDegree();

        for (var i = 0; i < forwardDifferences.Count; ++i)
        {
            forwardDifferences[i] = (_controlPoints[i + 1] - _controlPoints[i]) * degree;
        }

        return new BezierCurve(forwardDifferences);
    }

    /// <summary>
    ///     Gets a BezierCurve that corresponds to the specified segment of this BezierCurve.
    /// </summary>
    /// <param name="startpoint">The starting point along the curve.Must be between 0 and 1.</param>
    /// <param name="endpoint">The end of the segment. Must be between 0 and 1.</param>
    /// <returns>
    ///     A BezierCurve that corresponds to the specified segment.
    /// </returns>
    public BezierCurve GetSegment(double startpoint, double endpoint)
    {
        if (startpoint < 0 || endpoint > 1)
        {
            throw new Exception("Invalid segment parameters: must be between 0 and 1");
        }

        if (endpoint <= startpoint)
        {
            throw new Exception("Invalid segment parameters: t1 must be smaller than t2");
        }

        var points = new List<Vector2>(_controlPoints);
        var left = new List<Vector2>(points.Count);
        var right = new List<Vector2>(points.Count);

        for (var step = 1; step < points.Count; ++step)
        {
            left.Add(points[0]);

            for (var i = 0; i < points.Count - step; ++i)
            {
                points[i] += (points[i + 1] - points[i]) * (float)endpoint;
            }
        }

        left.Add(points[0]);

        var s = startpoint / endpoint;

        for (var step = 1; step < left.Count; ++step)
        {
            right.Add(left[^step]);

            for (var i = 0; i < left.Count - step; ++i)
            {
                left[i] += (left[i + 1] - left[i]) * (float)s;
            }
        }

        right.Add(left[0]);
        right.Reverse();

        return new BezierCurve(right);
    }

    /// <summary>
    ///     Insert control point as the new i-th control point. Existing control points from i onwards are pushed back by 1.
    ///     Indices start with 1. Negative indices wrap around: -1 is the last control point, -2 the one before the last, etc.
    /// </summary>
    /// <param name="x">Position of the control point along the x axis.</param>
    /// <param name="y">Position of the control point along the y axis.</param>
    /// <param name="i">Index of the control point.</param>
    public void InsertControlPoint(double x, double y, int i = -1)
    {
        if (i > 0)
        {
            i--;
        }

        if (_controlPoints.Count == 0)
        {
            i = 0;
        }

        while (i < 0)
        {
            i += _controlPoints.Count;
        }

        while (i > _controlPoints.Count)
        {
            i -= _controlPoints.Count;
        }


        _controlPoints.Insert(i, new Vector2((float)x, (float)y));
    }

    /// <summary>
    ///     Removes the specified control point.
    /// </summary>
    /// <param name="index">The index of the control point to remove.</param>
    public void RemoveControlPoint(int index)
    {
        if (_controlPoints.Count == 0)
        {
            throw new Exception("No control points to remove.");
        }

        while (index < 0)
        {
            index += _controlPoints.Count;
        }

        while (index >= _controlPoints.Count)
        {
            index -= _controlPoints.Count;
        }

        _controlPoints.RemoveAt(index);
    }

    /// <summary>
    ///     <para>Get a list of coordinates to be used with love.graphics.line.</para>
    ///     <para>
    ///         This function samples the Bézier curve using recursive subdivision. You can control the recursion depth using
    ///         the depth parameter.
    ///     </para>
    ///     <para>If you are just interested to know the position on the curve given a parameter, use BezierCurve:evaluate.</para>
    /// </summary>
    /// <param name="depth">Number of recursive subdivision steps.</param>
    /// <returns>
    ///     List of x,y-coordinate pairs of points on the curve.
    /// </returns>
    public List<(double x, double y)> Render(int depth = 5)
    {
        if (_controlPoints.Count < 2)
        {
            throw new Exception("Invalid Bezier curve: Not enough control points.");
        }

        var vertices = new List<Vector2>(_controlPoints);
        Subdivide(vertices, depth);

        var result = new List<(double x, double y)>(vertices.Count);

        foreach (var v in vertices)
        {
            result.Add((v.X, v.Y));
        }

        return result;
    }

    /// <summary>
    ///     <para>Get a list of coordinates on a specific part of the curve, to be used with love.graphics.line.</para>
    ///     <para>
    ///         This function samples the Bézier curve using recursive subdivision. You can control the recursion depth using
    ///         the depth parameter.
    ///     </para>
    ///     <para>If you just need to know the position on the curve given a parameter, use BezierCurve:evaluate.</para>
    /// </summary>
    /// <param name="startpoint">The starting point along the curve. Must be between 0 and 1.</param>
    /// <param name="endpoint">The end of the segment to render. Must be between 0 and 1.</param>
    /// <param name="depth">Number of recursive subdivision steps.</param>
    /// <returns>
    ///     List of x,y-coordinate pairs of points on the specified part of the curve.
    /// </returns>
    public List<(double x, double y)> RenderSegment(double startpoint, double endpoint, int depth = 5)
    {
        if (_controlPoints.Count < 2)
        {
            throw new Exception("Invalid Bezier curve: Not enough control points.");
        }

        var vertices = new List<Vector2>(_controlPoints);
        Subdivide(vertices, depth);

        var result = new List<(double x, double y)>(vertices.Count);

        foreach (var v in vertices)
        {
            result.Add((v.X, v.Y));
        }

        if (startpoint == endpoint)
        {
            result.Clear();
        }
        else if (startpoint < endpoint)
        {
            var start_idx = (int)(startpoint * result.Count);
            var end_idx = (int)(endpoint * result.Count + 0.5);
            return result.GetRange(start_idx, end_idx - start_idx);
        }
        else if (startpoint > endpoint)
        {
            var start_idx = (int)(endpoint * result.Count + 0.5);
            var end_idx = (int)(startpoint * result.Count);
            return result.GetRange(start_idx, end_idx - start_idx);
        }

        return result;
    }

    /// <summary>
    ///     Rotate the Bézier curve by an angle.
    /// </summary>
    /// <param name="angle">Rotation angle in radians.</param>
    /// <param name="ox">X coordinate of the rotation center.</param>
    /// <param name="oy">Y coordinate of the rotation center.</param>
    public void Rotate(double angle, double ox = 0, double oy = 0)
    {
        float c = (float)SysMath.Cos(angle), s = (float)SysMath.Sin(angle);
        var center = new Vector2((float)ox, (float)oy);

        for (var i = 0; i < _controlPoints.Count; ++i)
        {
            var v = _controlPoints[i] - center;
            var rotated = new Vector2(c * v.X - s * v.Y + center.X, s * v.X + c * v.Y + center.Y);
            _controlPoints[i] = rotated;
        }
    }

    /// <summary>
    ///     Scale the Bézier curve by a factor.
    /// </summary>
    /// <param name="s">Scale factor.</param>
    /// <param name="ox">X coordinate of the scaling center.</param>
    /// <param name="oy">Y coordinate of the scaling center.</param>
    public void Scale(double s, double ox = 0, double oy = 0)
    {
        var center = new Vector2((float)ox, (float)oy);

        for (var i = 0; i < _controlPoints.Count; ++i)
        {
            _controlPoints[i] = (_controlPoints[i] - center) * (float)s + center;
        }
    }

    /// <summary>
    ///     Set coordinates of the i-th control point. Indices start with 1.
    /// </summary>
    /// <param name="i">Index of the control point.</param>
    /// <param name="x">Position of the control point along the x axis.</param>
    /// <param name="y">Position of the control point along the y axis.</param>
    public void SetControlPoint(int i, double x, double y)
    {
        if (i > 0)
        {
            i--;
        }

        var point = new Vector2((float)x, (float)y);

        if (_controlPoints.Count == 0)
        {
            i = 0;
        }

        while (i < 0)
        {
            i += _controlPoints.Count;
        }

        while (i > _controlPoints.Count)
        {
            i -= _controlPoints.Count;
        }

        _controlPoints.Insert(i, point);
    }

    /// <summary>
    ///     Move the Bézier curve by an offset.
    /// </summary>
    /// <param name="dx">Offset along the x axis.</param>
    /// <param name="dy">Offset along the y axis.</param>
    public void Translate(double dx, double dy)
    {
        for (var i = 0; i < _controlPoints.Count; ++i)
        {
            _controlPoints[i] += new Vector2((float)dx, (float)dy);
        }
    }

    private void Subdivide(List<Vector2> points, int k)
    {
        if (k <= 0)
        {
            return;
        }

        var left = new List<Vector2>(points.Count);
        var right = new List<Vector2>(points.Count);

        for (var step = 1; step < points.Count; ++step)
        {
            left.Add(points[0]);
            right.Add(points[^step]);
            for (var i = 0; i < points.Count - step; ++i)
            {
                points[i] = (points[i] + points[i + 1]) * .5f;
            }
        }

        left.Add(points[0]);
        right.Add(points[0]);

        Subdivide(left, k - 1);
        Subdivide(right, k - 1);

        var newSize = left.Count + right.Count - 2;
        var merged = new List<Vector2>(newSize);

        for (var i = 0; i < left.Count - 1; ++i)
        {
            merged.Add(left[i]);
        }

        for (var i = 1; i < right.Count; ++i)
        {
            merged.Add(right[right.Count - i - 1]);
        }

        points.Clear();
        points.AddRange(merged);
    }
}