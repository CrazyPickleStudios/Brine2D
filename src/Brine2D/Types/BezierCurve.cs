namespace Brine2D
{
    /// <summary>
    /// <para>A Bézier curve object that can evaluate and render Bézier curves of arbitrary degree.</para>
/// <para>For more information on Bézier curves check this great article on Wikipedia.</para>
    /// </summary>
    // TODO: Requires Review
    public class BezierCurve
    {
        /// <summary>
        /// <para>Evaluate Bézier curve at parameter t. The parameter must be between 0 and 1 (inclusive) and there must be at least two control points for the curve.</para>
        /// <para>This function can be used to move objects along paths or tween parameters. However it should not be used to render the curve - see BezierCurve:render for that purpose.</para>
        /// </summary>
        /// <param name="t">Where to evaluate the curve. Must be between 0 and 1.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>x</term><description>x coordinate of the curve at parameter t.</description></item>
        /// <item><term>y</term><description>y coordinate of the curve at parameter t.</description></item>
        /// </list>
        /// </returns>
        public (double x, double y) Evaluate(double t) => throw new NotImplementedException();
        /// <summary>
        /// <para>Evaluate Bézier curve at parameter t. The parameter must be between 0 and 1 (inclusive) and there must be at least two control points for the curve.</para>
        /// <para>This function can be used to move objects along paths or tween parameters. However it should not be used to render the curve - see BezierCurve:render for that purpose.</para>
        /// </summary>
        public void NewBezierCurve() => throw new NotImplementedException();
        /// <summary>
        /// <para>Get coordinates of the i-th control point. Indices start with 1.</para>
        /// </summary>
        /// <param name="i">Index of the control point.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>x</term><description>Position of the control point along the x axis.</description></item>
        /// <item><term>y</term><description>Position of the control point along the y axis.</description></item>
        /// </list>
        /// </returns>
        public (double x, double y) GetControlPoint(double i) => throw new NotImplementedException();
        /// <summary>
        /// <para>Get coordinates of the i-th control point. Indices start with 1.</para>
        /// </summary>
        public void GetControlPointCount() => throw new NotImplementedException();
        /// <summary>
        /// <para>Get the number of control points in the Bézier curve.</para>
        /// </summary>
        /// <param name="count">The number of control points.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>count</term><description>The number of control points.</description></item>
        /// </list>
        /// </returns>
        public double GetControlPointCount(double count) => throw new NotImplementedException();
        /// <summary>
        /// <para>Get degree of the Bézier curve. The degree is equal to number-of-control-points - 1.</para>
        /// </summary>
        /// <param name="degree">Degree of the Bézier curve.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>degree</term><description>Degree of the Bézier curve.</description></item>
        /// </list>
        /// </returns>
        public double GetDegree(double degree) => throw new NotImplementedException();
        /// <summary>
        /// <para>Get the derivative of the Bézier curve.</para>
        /// <para>This function can be used to rotate sprites moving along a curve in the direction of the movement and compute the direction perpendicular to the curve at some parameter t.</para>
        /// </summary>
        /// <param name="derivative">The derivative curve.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>derivative</term><description>The derivative curve.</description></item>
        /// </list>
        /// </returns>
        public object GetDerivative(object derivative) => throw new NotImplementedException();
        /// <summary>
        /// <para>Get the derivative of the Bézier curve.</para>
        /// <para>This function can be used to rotate sprites moving along a curve in the direction of the movement and compute the direction perpendicular to the curve at some parameter t.</para>
        /// </summary>
        // TODO: public void NewBezierCurve() => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets a BezierCurve that corresponds to the specified segment of this BezierCurve.</para>
        /// </summary>
        /// <param name="startpoint">The starting point along the curve. Must be between 0 and 1.</param>
        /// <param name="endpoint">The end of the segment. Must be between 0 and 1.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>curve</term><description>A BezierCurve that corresponds to the specified segment.</description></item>
        /// </list>
        /// </returns>
        public object GetSegment(double startpoint, double endpoint) => throw new NotImplementedException();
        /// <summary>
        /// <para>Insert control point as the new i-th control point. Existing control points from i onwards are pushed back by 1. Indices start with 1. Negative indices wrap around: -1 is the last control point, -2 the one before the last, etc.</para>
        /// </summary>
        /// <param name="x">Position of the control point along the x axis.</param>
        /// <param name="y">Position of the control point along the y axis.</param>
        /// <param name="i">Index of the control point.</param>
        public void InsertControlPoint(double x, double y, double i = -1) => throw new NotImplementedException();
        /// <summary>
        /// <para>Removes the specified control point.</para>
        /// </summary>
        /// <param name="index">The index of the control point to remove.</param>
        public void RemoveControlPoint(double index) => throw new NotImplementedException();
        /// <summary>
        /// <para>Get a list of coordinates to be used with love.graphics.line.</para>
        /// <para>This function samples the Bézier curve using recursive subdivision. You can control the recursion depth using the depth parameter.</para>
        /// <para>If you are just interested to know the position on the curve given a parameter, use BezierCurve:evaluate.</para>
        /// </summary>
        /// <param name="depth">Number of recursive subdivision steps.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>coordinates</term><description>List of x,y-coordinate pairs of points on the curve.</description></item>
        /// </list>
        /// </returns>
        public object Render(double depth = 5) => throw new NotImplementedException();
        /// <summary>
        /// <para>Get a list of coordinates to be used with love.graphics.line.</para>
        /// <para>This function samples the Bézier curve using recursive subdivision. You can control the recursion depth using the depth parameter.</para>
        /// <para>If you are just interested to know the position on the curve given a parameter, use BezierCurve:evaluate.</para>
        /// </summary>
        // TODO: public void NewBezierCurve() => throw new NotImplementedException();
        /// <summary>
        /// <para>Get a list of coordinates on a specific part of the curve, to be used with love.graphics.line.</para>
        /// <para>This function samples the Bézier curve using recursive subdivision. You can control the recursion depth using the depth parameter.</para>
        /// <para>If you just need to know the position on the curve given a parameter, use BezierCurve:evaluate.</para>
        /// </summary>
        /// <param name="startpoint">The starting point along the curve. Must be between 0 and 1.</param>
        /// <param name="endpoint">The end of the segment to render. Must be between 0 and 1.</param>
        /// <param name="depth">Number of recursive subdivision steps.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>coordinates</term><description>List of x,y-coordinate pairs of points on the specified part of the curve.</description></item>
        /// </list>
        /// </returns>
        public object RenderSegment(double startpoint, double endpoint, double depth = 5) => throw new NotImplementedException();
        /// <summary>
        /// <para>Get a list of coordinates on a specific part of the curve, to be used with love.graphics.line.</para>
        /// <para>This function samples the Bézier curve using recursive subdivision. You can control the recursion depth using the depth parameter.</para>
        /// <para>If you just need to know the position on the curve given a parameter, use BezierCurve:evaluate.</para>
        /// </summary>
        // TODO: public void NewBezierCurve() => throw new NotImplementedException();
        /// <summary>
        /// <para>Rotate the Bézier curve by an angle.</para>
        /// </summary>
        /// <param name="angle">Rotation angle in radians.</param>
        /// <param name="ox">X coordinate of the rotation center.</param>
        /// <param name="oy">Y coordinate of the rotation center.</param>
        public void Rotate(double angle, double ox = 0, double oy = 0) => throw new NotImplementedException();
        /// <summary>
        /// <para>Scale the Bézier curve by a factor.</para>
        /// </summary>
        /// <param name="s">Scale factor.</param>
        /// <param name="ox">X coordinate of the scaling center.</param>
        /// <param name="oy">Y coordinate of the scaling center.</param>
        public void Scale(double s, double ox = 0, double oy = 0) => throw new NotImplementedException();
        /// <summary>
        /// <para>Set coordinates of the i-th control point. Indices start with 1.</para>
        /// </summary>
        /// <param name="i">Index of the control point.</param>
        /// <param name="x">Position of the control point along the x axis.</param>
        /// <param name="y">Position of the control point along the y axis.</param>
        public void SetControlPoint(double i, double x, double y) => throw new NotImplementedException();
        /// <summary>
        /// <para>Move the Bézier curve by an offset.</para>
        /// </summary>
        /// <param name="dx">Offset along the x axis.</param>
        /// <param name="dy">Offset along the y axis.</param>
        public void Translate(double dx, double dy) => throw new NotImplementedException();
        /// <summary>
        /// <para>Destroys the object's Lua reference. The object will be completely deleted if it's not referenced by any other LÖVE object or thread.</para>
        /// <para>This method can be used to immediately clean up resources without waiting for Lua's garbage collector.</para>
        /// </summary>
        /// <param name="success">True if the object was released by this call, false if it had been previously released.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>success</term><description>True if the object was released by this call, false if it had been previously released.</description></item>
        /// </list>
        /// </returns>
        public bool Release(bool success) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the type of the object as a string.</para>
        /// </summary>
        /// <param name="type">The type as a string.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>type</term><description>The type as a string.</description></item>
        /// </list>
        /// </returns>
        public string Type(string type) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the type of the object as a string.</para>
        /// </summary>
        // TODO: public void NewImage() => throw new NotImplementedException();
        /// <summary>
        /// <para>Checks whether an object is of a certain type. If the object has the type with the specified name in its hierarchy, this function will return true.</para>
        /// </summary>
        /// <param name="name">The name of the type to check for.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>b</term><description>True if the object is of the specified type, false otherwise.</description></item>
        /// </list>
        /// </returns>
        public bool TypeOf(string name) => throw new NotImplementedException();
        /// <summary>
        /// <para>Checks whether an object is of a certain type. If the object has the type with the specified name in its hierarchy, this function will return true.</para>
        /// </summary>
        // TODO: public void NewImage() => throw new NotImplementedException();
    }
}
