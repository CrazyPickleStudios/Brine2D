namespace Brine2D
{
    /// <summary>
    /// <para>Object containing a coordinate system transformation.</para>
/// <para>The love.graphics module has several functions and function variants which accept Transform objects.</para>
    /// </summary>
    // TODO: Requires Review
    public class Transform
    {
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
        /// <summary>
        /// <para>Applies the given other Transform object to this one.</para>
        /// <para>This effectively multiplies this Transform's internal transformation matrix with the other Transform's (i.e. self * other), and stores the result in this object.</para>
        /// </summary>
        /// <param name="other">The other Transform object to apply to this Transform.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>transform</term><description>The Transform object the method was called on. Allows easily chaining Transform methods.</description></item>
        /// </list>
        /// </returns>
        public object Apply(object other) => throw new NotImplementedException();
        /// <summary>
        /// <para>Creates a new copy of this Transform.</para>
        /// </summary>
        /// <param name="clone">The copy of this Transform.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>clone</term><description>The copy of this Transform.</description></item>
        /// </list>
        /// </returns>
        public object Clone(object clone) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the internal 4x4 transformation matrix stored by this Transform. The matrix is returned in row-major order.</para>
        /// </summary>
        /// <param name="e11">The first column of the first row of the matrix.</param>
        /// <param name="e12">The second column of the first row of the matrix.</param>
        /// <param name="">Additional matrix elements.</param>
        /// <param name="e44">The fourth column of the fourth row of the matrix.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>e11</term><description>The first column of the first row of the matrix.</description></item>
        /// <item><term>e12</term><description>The second column of the first row of the matrix.</description></item>
        /// <item><term></term><description>Additional matrix elements.</description></item>
        /// <item><term>e44</term><description>The fourth column of the fourth row of the matrix.</description></item>
        /// </list>
        /// </returns>
        // TODO: public (double e11, double e12, object, double e44) GetMatrix(double e11, double e12, object, double e44) => throw new NotImplementedException();
        /// <summary>
        /// <para>Creates a new Transform containing the inverse of this Transform.</para>
        /// </summary>
        /// <param name="inverse">A new Transform object representing the inverse of this Transform's matrix.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>inverse</term><description>A new Transform object representing the inverse of this Transform's matrix.</description></item>
        /// </list>
        /// </returns>
        public object Inverse(object inverse) => throw new NotImplementedException();
        /// <summary>
        /// <para>Applies the reverse of the Transform object's transformation to the given 2D position.</para>
        /// <para>This effectively converts the given position from the local coordinate space of the Transform into global coordinates.</para>
        /// <para>One use of this method can be to convert a screen-space mouse position into global world coordinates, if the given Transform has transformations applied that are used for a camera system in-game.</para>
        /// </summary>
        /// <param name="localX">The x component of the position with the transform applied.</param>
        /// <param name="localY">The y component of the position with the transform applied.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>globalX</term><description>The x component of the position in global coordinates.</description></item>
        /// <item><term>globalY</term><description>The y component of the position in global coordinates.</description></item>
        /// </list>
        /// </returns>
        public (double globalX, double globalY) InverseTransformPoint(double localX, double localY) => throw new NotImplementedException();
        /// <summary>
        /// <para>Checks whether the Transform is an affine transformation.</para>
        /// </summary>
        /// <param name="affine">if the transform object is an affine transformation, otherwise.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>affine</term><description>if the transform object is an affine transformation, otherwise.</description></item>
        /// </list>
        /// </returns>
        public bool IsAffine2DTransform(bool affine) => throw new NotImplementedException();
        /// <summary>
        /// <para>Resets the Transform to an identity state. All previously applied transformations are erased.</para>
        /// </summary>
        /// <param name="transform">The Transform object the method was called on. Allows easily chaining Transform methods.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>transform</term><description>The Transform object the method was called on. Allows easily chaining Transform methods.</description></item>
        /// </list>
        /// </returns>
        public object Reset(object transform) => throw new NotImplementedException();
        /// <summary>
        /// <para>Applies a rotation to the Transform's coordinate system. This method does not reset any previously applied transformations.</para>
        /// </summary>
        /// <param name="angle">The relative angle in radians to rotate this Transform by.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>transform</term><description>The Transform object the method was called on. Allows easily chaining Transform methods.</description></item>
        /// </list>
        /// </returns>
        public object Rotate(double angle) => throw new NotImplementedException();
        /// <summary>
        /// <para>Scales the Transform's coordinate system. This method does not reset any previously applied transformations.</para>
        /// </summary>
        /// <param name="sx">The relative scale factor along the x-axis.</param>
        /// <param name="sy">The relative scale factor along the y-axis.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>transform</term><description>The Transform object the method was called on. Allows easily chaining Transform methods.</description></item>
        /// </list>
        /// </returns>
        // TODO: public object Scale(double sx, double sy = sx) => throw new NotImplementedException();
        /// <summary>
        /// <para>Directly sets the Transform's internal 4x4 transformation matrix.</para>
        /// </summary>
        /// <param name="e11">The first column of the first row of the matrix.</param>
        /// <param name="e12">The second column of the first row of the matrix.</param>
        /// <param name="">Additional matrix elements.</param>
        /// <param name="e44">The fourth column of the fourth row of the matrix.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>transform</term><description>The Transform object the method was called on. Allows easily chaining Transform methods.</description></item>
        /// </list>
        /// </returns>
        // TODO: public object SetMatrix(double e11, double e12, object, double e44) => throw new NotImplementedException();
        /// <summary>
        /// <para>Directly sets the Transform's internal 4x4 transformation matrix.</para>
        /// </summary>
        /// <param name="layout">How to interpret the matrix element arguments (row-major or column-major).</param>
        /// <param name="e11">The first column of the first row of the matrix.</param>
        /// <param name="e12">The second column of the first row or the first column of the second row of the matrix, depending on the specified layout.</param>
        /// <param name="">Additional matrix elements.</param>
        /// <param name="e44">The fourth column of the fourth row of the matrix.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>transform</term><description>The Transform object the method was called on. Allows easily chaining Transform methods.</description></item>
        /// </list>
        /// </returns>
        // TODO: public object SetMatrix(object layout, double e11, double e12, object, double e44) => throw new NotImplementedException();
        /// <summary>
        /// <para>Directly sets the Transform's internal 4x4 transformation matrix.</para>
        /// </summary>
        /// <param name="layout">How to interpret the matrix element arguments (row-major or column-major).</param>
        /// <param name="matrix">A flat table containing the 16 matrix elements.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>transform</term><description>The Transform object the method was called on. Allows easily chaining Transform methods.</description></item>
        /// </list>
        /// </returns>
        // TODO: public object SetMatrix(object layout, object matrix) => throw new NotImplementedException();
        /// <summary>
        /// <para>Directly sets the Transform's internal 4x4 transformation matrix.</para>
        /// </summary>
        /// <param name="layout">How to interpret the matrix element arguments (row-major or column-major).</param>
        /// <param name="matrix">A table of 4 tables, with each sub-table containing 4 matrix elements.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>transform</term><description>The Transform object the method was called on. Allows easily chaining Transform methods.</description></item>
        /// </list>
        /// </returns>
        public object SetMatrix(object layout, object matrix) => throw new NotImplementedException();
        /// <summary>
        /// <para>Resets the Transform to the specified transformation parameters.</para>
        /// </summary>
        /// <param name="x">The position of the Transform on the x-axis.</param>
        /// <param name="y">The position of the Transform on the y-axis.</param>
        /// <param name="angle">The orientation of the Transform in radians.</param>
        /// <param name="sx">Scale factor on the x-axis.</param>
        /// <param name="sy">Scale factor on the y-axis.</param>
        /// <param name="ox">Origin offset on the x-axis.</param>
        /// <param name="oy">Origin offset on the y-axis.</param>
        /// <param name="kx">Shearing / skew factor on the x-axis.</param>
        /// <param name="ky">Shearing / skew factor on the y-axis.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>transform</term><description>The Transform object the method was called on. Allows easily chaining Transform methods.</description></item>
        /// </list>
        /// </returns>
        // TODO: public object SetTransformation(double x, double y, double angle = 0, double sx = 1, double sy = sx, double ox = 0, double oy = 0, double kx = 0, double ky = 0) => throw new NotImplementedException();
        /// <summary>
        /// <para>Applies a shear factor (skew) to the Transform's coordinate system. This method does not reset any previously applied transformations.</para>
        /// </summary>
        /// <param name="kx">The shear factor along the x-axis.</param>
        /// <param name="ky">The shear factor along the y-axis.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>transform</term><description>The Transform object the method was called on. Allows easily chaining Transform methods.</description></item>
        /// </list>
        /// </returns>
        public object Shear(double kx, double ky) => throw new NotImplementedException();
        /// <summary>
        /// <para>Applies the Transform object's transformation to the given 2D position.</para>
        /// <para>This effectively converts the given position from global coordinates into the local coordinate space of the Transform.</para>
        /// </summary>
        /// <param name="globalX">The x component of the position in global coordinates.</param>
        /// <param name="globalY">The y component of the position in global coordinates.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>localX</term><description>The x component of the position with the transform applied.</description></item>
        /// <item><term>localY</term><description>The y component of the position with the transform applied.</description></item>
        /// </list>
        /// </returns>
        public (double localX, double localY) TransformPoint(double globalX, double globalY) => throw new NotImplementedException();
        /// <summary>
        /// <para>Applies a translation to the Transform's coordinate system. This method does not reset any previously applied transformations.</para>
        /// </summary>
        /// <param name="dx">The relative translation along the x-axis.</param>
        /// <param name="dy">The relative translation along the y-axis.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>transform</term><description>The Transform object the method was called on. Allows easily chaining Transform methods.</description></item>
        /// </list>
        /// </returns>
        public object Translate(double dx, double dy) => throw new NotImplementedException();
    }
}
