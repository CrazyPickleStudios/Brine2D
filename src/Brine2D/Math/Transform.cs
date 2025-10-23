using System.Numerics;

namespace Brine2D.Math;

/// <summary>
///     <para>Object containing a coordinate system transformation.</para>
///     <para>The love.graphics module has several functions and function variants which accept Transform objects.</para>
/// </summary>
/// TODO: Really need to look at all Matrix4x4 operations to ensure correctness and efficiency.
/// Try to use built-in methods where possible.
public class Transform : Object
{
    private bool _inverseDirty;
    private Matrix4x4 _inverseMatrix;
    private Matrix4x4 _matrix;

    internal Transform()
    {
        _matrix = new Matrix4x4();
        _inverseDirty = true;
        _inverseMatrix = new Matrix4x4();
    }

    internal Transform(Matrix4x4 matrix)
    {
        _matrix = matrix;
        _inverseDirty = true;
        _inverseMatrix = new Matrix4x4();
    }

    /// <summary>
    ///     <para>Applies the given other Transform object to this one.</para>
    ///     <para>
    ///         This effectively multiplies this Transform's internal transformation matrix with the other Transform's (i.e.
    ///         self * other), and stores the result in this object.
    ///     </para>
    /// </summary>
    /// <param name="other">The other Transform object to apply to this Transform.</param>
    /// <returns>
    ///     The Transform object the method was called on. Allows easily chaining Transform methods.
    /// </returns>
    public Transform Apply(Transform other)
    {
        _matrix *= other._matrix;
        _inverseDirty = true;
        return this;
    }

    /// <summary>
    ///     Creates a new copy of this Transform.
    /// </summary>
    /// <returns>
    ///     The copy of this Transform.
    /// </returns>
    public Transform Clone()
    {
        return new Transform(_matrix);
    }

    /// <summary>
    ///     Gets the internal 4x4 transformation matrix stored by this Transform. The matrix is returned in row-major order.
    /// </summary>
    /// <returns>
    ///     <list type="bullet">
    ///         <item>
    ///             <term>e11</term><description>The first column of the first row of the matrix.</description>
    ///         </item>
    ///         <item>
    ///             <term>e12</term><description>The second column of the first row of the matrix.</description>
    ///         </item>
    ///         <item>
    ///             <term></term><description>Additional matrix elements.</description>
    ///         </item>
    ///         <item>
    ///             <term>e44</term><description>The fourth column of the fourth row of the matrix.</description>
    ///         </item>
    ///     </list>
    /// </returns>
    public (
        double e11, double e12, double e13, double e14,
        double e21, double e22, double e23, double e24,
        double e31, double e32, double e33, double e34,
        double e41, double e42, double e43, double e44) GetMatrix()
    {
        return (
            _matrix.M11, _matrix.M12, _matrix.M13, _matrix.M14,
            _matrix.M21, _matrix.M22, _matrix.M23, _matrix.M24,
            _matrix.M31, _matrix.M32, _matrix.M33, _matrix.M34,
            _matrix.M41, _matrix.M42, _matrix.M43, _matrix.M44
        );
    }

    /// <summary>
    ///     Creates a new Transform containing the inverse of this Transform.
    /// </summary>
    /// <returns>
    ///     A new Transform object representing the inverse of this Transform's matrix.
    /// </returns>
    public Transform Inverse()
    {
        return new Transform(GetInverseMatrix());
    }

    /// <summary>
    ///     <para>Applies the reverse of the Transform object's transformation to the given 2D position.</para>
    ///     <para>
    ///         This effectively converts the given position from the local coordinate space of the Transform into global
    ///         coordinates.
    ///     </para>
    ///     <para>
    ///         One use of this method can be to convert a screen-space mouse position into global world coordinates, if the
    ///         given Transform has transformations applied that are used for a camera system in-game.
    ///     </para>
    /// </summary>
    /// <param name="localX">The x component of the position with the transform applied.</param>
    /// <param name="localY">The y component of the position with the transform applied.</param>
    /// <returns>
    ///     <list type="bullet">
    ///         <item>
    ///             <term>globalX</term><description>The x component of the position in global coordinates.</description>
    ///         </item>
    ///         <item>
    ///             <term>globalY</term><description>The y component of the position in global coordinates.</description>
    ///         </item>
    ///     </list>
    /// </returns>
    public (double globalX, double globalY) InverseTransformPoint(double localX, double localY)
    {
        var inv = GetInverseMatrix();
        var v = Vector4.Transform(new Vector4((float)localX, (float)localY, 0f, 1f), inv);
        return (v.X, v.Y);
    }

    /// <summary>
    ///     Checks whether the Transform is an affine transformation.
    /// </summary>
    /// <returns>
    ///     True if the transform object is an affine transformation, false otherwise.
    /// </returns>
    public bool IsAffine2DTransform()
    {
        return MathF.Abs
               (
                   _matrix.M13 + _matrix.M14 +
                   _matrix.M23 + _matrix.M24 +
                   _matrix.M31 + _matrix.M32 +
                   _matrix.M34 + _matrix.M43
               ) < 0.00001f
               && MathF.Abs(_matrix.M33 + _matrix.M44 - 2.0f) < 0.00001f;
    }

    /// <summary>
    ///     Resets the Transform to an identity state. All previously applied transformations are erased.
    /// </summary>
    /// <returns>
    ///     The Transform object the method was called on. Allows easily chaining Transform methods.
    /// </returns>
    public Transform Reset()
    {
        _matrix = Matrix4x4.Identity;
        _inverseDirty = true;
        return this;
    }

    /// <summary>
    ///     Applies a rotation to the Transform's coordinate system. This method does not reset any previously applied
    ///     transformations.
    /// </summary>
    /// <param name="angle">The relative angle in radians to rotate this Transform by.</param>
    /// <returns>
    ///     The Transform object the method was called on. Allows easily chaining Transform methods.
    /// </returns>
    public Transform Rotate(float angle)
    {
        _matrix = Matrix4x4.Identity;

        var c = MathF.Cos(angle);
        var s = MathF.Sin(angle);

        // Match the original element assignments:
        // e[0]=c, e[4]=-s, e[1]=s, e[5]=c  (row-major)
        _matrix = Matrix4x4.Identity;
        _matrix.M11 = c;
        _matrix.M12 = s;
        _matrix.M21 = -s;
        _matrix.M22 = c;

        _inverseDirty = true;
        return this;
    }

    /// <summary>
    ///     Scales the Transform's coordinate system. This method does not reset any previously applied transformations.
    /// </summary>
    /// <param name="sx">The relative scale factor along the x-axis.</param>
    /// <param name="sy">The relative scale factor along the y-axis.</param>
    /// <returns>
    ///     The Transform object the method was called on. Allows easily chaining Transform methods.
    /// </returns>
    public Transform Scale(double sx, double? sy = null)
    {
        var s = Matrix4x4.CreateScale((float)sx, (float)(sy ?? sx), 1f);
        _matrix = Matrix4x4.Multiply(_matrix, s);
        _inverseDirty = true;
        return this;
    }

    /// <summary>
    ///     Directly sets the Transform's internal 4x4 transformation matrix.
    /// </summary>
    /// <param name="e11">The first column of the first row of the matrix.</param>
    /// <param name="e12">The second column of the first row of the matrix.</param>
    /// <param name="">Additional matrix elements.</param>
    /// <param name="e44">The fourth column of the fourth row of the matrix.</param>
    /// <returns>
    ///     The Transform object the method was called on. Allows easily chaining Transform methods.
    /// </returns>
    public Transform SetMatrix(double e11, double e12, double e13, double e14,
        double e21, double e22, double e23, double e24,
        double e31, double e32, double e33, double e34,
        double e41, double e42, double e43, double e44)
    {
        _matrix = new Matrix4x4(
            (float)e11, (float)e12, (float)e13, (float)e14,
            (float)e21, (float)e22, (float)e23, (float)e24,
            (float)e31, (float)e32, (float)e33, (float)e34,
            (float)e41, (float)e42, (float)e43, (float)e44
        );

        _inverseDirty = true;
        return this;
    }

    /// <summary>
    ///     Directly sets the Transform's internal 4x4 transformation matrix.
    /// </summary>
    /// <param name="layout">How to interpret the matrix element arguments (row-major or column-major).</param>
    /// <param name="e11">The first column of the first row of the matrix.</param>
    /// <param name="e12">
    ///     The second column of the first row or the first column of the second row of the matrix, depending on
    ///     the specified layout.
    /// </param>
    /// <param name="">Additional matrix elements.</param>
    /// <param name="e44">The fourth column of the fourth row of the matrix.</param>
    /// <returns>
    ///     The Transform object the method was called on. Allows easily chaining Transform methods.
    /// </returns>
    public Transform SetMatrix(MatrixLayout layout, double e11, double e12, double e13, double e14,
        double e21, double e22, double e23, double e24,
        double e31, double e32, double e33, double e34,
        double e41, double e42, double e43, double e44)
    {
        double[] flat =
        [
            e11, e12, e13, e14,
            e21, e22, e23, e24,
            e31, e32, e33, e34,
            e41, e42, e43, e44
        ];

        return SetMatrix(layout, flat);
    }

    /// <summary>
    ///     Directly sets the Transform's internal 4x4 transformation matrix.
    /// </summary>
    /// <param name="layout">How to interpret the matrix element arguments (row-major or column-major).</param>
    /// <param name="matrix">A flat table containing the 16 matrix elements.</param>
    /// <returns>
    ///     The Transform object the method was called on. Allows easily chaining Transform methods.
    /// </returns>
    public Transform SetMatrix(MatrixLayout layout, double[] matrix)
    {
        var m = new float[16];
        if (layout == MatrixLayout.Row)
        {
            for (var i = 0; i < 16; ++i)
            {
                m[i] = (float)matrix[i];
            }
        }
        else // Column-major input: transpose to row-major
        {
            // column-major array: [m11,m21,m31,m41, m12,m22,...]
            m[0] = (float)matrix[0];
            m[1] = (float)matrix[4];
            m[2] = (float)matrix[8];
            m[3] = (float)matrix[12];
            m[4] = (float)matrix[1];
            m[5] = (float)matrix[5];
            m[6] = (float)matrix[9];
            m[7] = (float)matrix[13];
            m[8] = (float)matrix[2];
            m[9] = (float)matrix[6];
            m[10] = (float)matrix[10];
            m[11] = (float)matrix[14];
            m[12] = (float)matrix[3];
            m[13] = (float)matrix[7];
            m[14] = (float)matrix[11];
            m[15] = (float)matrix[15];
        }

        _matrix = new Matrix4x4(
            m[0], m[1], m[2], m[3],
            m[4], m[5], m[6], m[7],
            m[8], m[9], m[10], m[11],
            m[12], m[13], m[14], m[15]
        );

        _inverseDirty = true;
        return this;
    }

    /// <summary>
    ///     Directly sets the Transform's internal 4x4 transformation matrix.
    /// </summary>
    /// <param name="layout">How to interpret the matrix element arguments (row-major or column-major).</param>
    /// <param name="matrix">A table of 4 tables, with each sub-table containing 4 matrix elements.</param>
    /// <returns>
    ///     The Transform object the method was called on. Allows easily chaining Transform methods.
    /// </returns>
    public Transform SetMatrix(MatrixLayout layout, double[][] matrix)
    {
        var flat = new double[16];
        for (var r = 0; r < 4; ++r)
        {
            if (matrix[r] == null || matrix[r].Length != 4)
            {
                throw new ArgumentException("matrix must be a 4x4 array.", nameof(matrix));
            }

            flat[r * 4 + 0] = matrix[r][0];
            flat[r * 4 + 1] = matrix[r][1];
            flat[r * 4 + 2] = matrix[r][2];
            flat[r * 4 + 3] = matrix[r][3];
        }

        // If caller passed column-major as a table-of-columns, they must pass layout accordingly.
        return SetMatrix(layout, flat);
    }

    /// <summary>
    ///     Resets the Transform to the specified transformation parameters.
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
    ///     The Transform object the method was called on. Allows easily chaining Transform methods.
    /// </returns>
    public Transform SetTransformation
    (
        double x,
        double y,
        double angle = 0,
        double sx = 1,
        double? sy = null,
        double ox = 0,
        double oy = 0,
        double kx = 0,
        double ky = 0
    )
    {
        var fsx = (float)sx;
        var fsy = (float)(sy ?? sx);
        var fox = (float)ox;
        var foy = (float)oy;
        var fkx = (float)kx;
        var fky = (float)ky;
        var fang = (float)angle;

        var c = MathF.Cos(fang);
        var s = MathF.Sin(fang);

        _matrix = Matrix4x4.Identity;

        _matrix.M33 = _matrix.M44 = 1.0f;
        _matrix.M11 = c * fsx - fky * s * fsy;
        _matrix.M12 = s * fsx + fky * c * fsy;
        _matrix.M21 = fkx * c * fsx - s * fsy;
        _matrix.M22 = fkx * s * fsx + c * fsy;
        _matrix.M33 = 1f;
        _matrix.M41 = (float)x - fox * _matrix.M11 - foy * _matrix.M21;
        _matrix.M42 = (float)y - fox * _matrix.M12 - foy * _matrix.M22;
        _matrix.M44 = 1f;

        _inverseDirty = true;
        return this;
    }

    /// <summary>
    ///     Applies a shear factor (skew) to the Transform's coordinate system. This method does not reset any previously
    ///     applied transformations.
    /// </summary>
    /// <param name="kx">The shear factor along the x-axis.</param>
    /// <param name="ky">The shear factor along the y-axis.</param>
    /// <returns>
    ///     The Transform object the method was called on. Allows easily chaining Transform methods.
    /// </returns>
    public Transform Shear(double kx, double ky)
    {
        var t = Matrix4x4.Identity;

        t.M12 = (float)ky;
        t.M21 = (float)kx;

        _matrix = Matrix4x4.Multiply(_matrix, t);
        _inverseDirty = true;
        return this;
    }

    /// <summary>
    ///     <para>Applies the Transform object's transformation to the given 2D position.</para>
    ///     <para>
    ///         This effectively converts the given position from global coordinates into the local coordinate space of the
    ///         Transform.
    ///     </para>
    /// </summary>
    /// <param name="globalX">The x component of the position in global coordinates.</param>
    /// <param name="globalY">The y component of the position in global coordinates.</param>
    /// <returns>
    ///     <list type="bullet">
    ///         <item>
    ///             <term>localX</term><description>The x component of the position with the transform applied.</description>
    ///         </item>
    ///         <item>
    ///             <term>localY</term><description>The y component of the position with the transform applied.</description>
    ///         </item>
    ///     </list>
    /// </returns>
    public (double localX, double localY) TransformPoint(double globalX, double globalY)
    {
        var v = Vector4.Transform(new Vector4((float)globalX, (float)globalY, 0f, 1f), _matrix);
        return (v.X, v.Y);
    }

    /// <summary>
    ///     Applies a translation to the Transform's coordinate system. This method does not reset any previously applied
    ///     transformations.
    /// </summary>
    /// <param name="dx">The relative translation along the x-axis.</param>
    /// <param name="dy">The relative translation along the y-axis.</param>
    /// <returns>
    ///     The Transform object the method was called on. Allows easily chaining Transform methods.
    /// </returns>
    public Transform Translate(double dx, double dy)
    {
        var t = Matrix4x4.Identity;
        t.M41 = (float)dx;
        t.M42 = (float)dy;

        _matrix = Matrix4x4.Multiply(_matrix, t);
        _inverseDirty = true;
        return this;
    }

    private Matrix4x4 GetInverseMatrix()
    {
        if (_inverseDirty)
        {
            _inverseDirty = false;
            Matrix4x4.Invert(_matrix, out _inverseMatrix);
        }

        return _inverseMatrix;
    }
}