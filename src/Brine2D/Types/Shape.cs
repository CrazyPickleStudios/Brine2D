namespace Brine2D
{
    /// <summary>
    /// <para>Shapes are solid 2d geometrical objects which handle the mass and collision of a Body in love.physics.</para>
/// <para>Shapes are attached to a Body via a Fixture. The Shape object is copied when this happens.</para>
/// <para>The Shape's position is relative to the position of the Body it has been attached to.</para>
    /// </summary>
    // TODO: Requires Review
    public class Shape
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
        /// <para>Returns the points of the bounding box for the transformed shape.</para>
        /// </summary>
        /// <param name="tx">The translation of the shape on the x-axis.</param>
        /// <param name="ty">The translation of the shape on the y-axis.</param>
        /// <param name="tr">The shape rotation.</param>
        /// <param name="childIndex">The index of the child to compute the bounding box of.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>topLeftX</term><description>The x position of the top-left point.</description></item>
        /// <item><term>topLeftY</term><description>The y position of the top-left point.</description></item>
        /// <item><term>bottomRightX</term><description>The x position of the bottom-right point.</description></item>
        /// <item><term>bottomRightY</term><description>The y position of the bottom-right point.</description></item>
        /// </list>
        /// </returns>
        public (double topLeftX, double topLeftY, double bottomRightX, double bottomRightY) ComputeAABB(double tx, double ty, double tr, double childIndex = 1) => throw new NotImplementedException();
        /// <summary>
        /// <para>Computes the mass properties for the shape with the specified density.</para>
        /// </summary>
        /// <param name="density">The shape density.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>x</term><description>The x postition of the center of mass.</description></item>
        /// <item><term>y</term><description>The y postition of the center of mass.</description></item>
        /// <item><term>mass</term><description>The mass of the shape.</description></item>
        /// <item><term>inertia</term><description>The rotational inertia.</description></item>
        /// </list>
        /// </returns>
        public (double x, double y, double mass, double inertia) ComputeMass(double density) => throw new NotImplementedException();
        /// <summary>
        /// <para>Returns the number of children the shape has.</para>
        /// </summary>
        /// <param name="count">The number of children.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>count</term><description>The number of children.</description></item>
        /// </list>
        /// </returns>
        public double GetChildCount(double count) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the radius of the shape.</para>
        /// </summary>
        /// <param name="radius">The radius of the shape.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>radius</term><description>The radius of the shape.</description></item>
        /// </list>
        /// </returns>
        public double GetRadius(double radius) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets a string representing the Shape.</para>
        /// <para>This function can be useful for conditional debug drawing.</para>
        /// </summary>
        /// <param name="type">The type of the Shape.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>type</term><description>The type of the Shape.</description></item>
        /// </list>
        /// </returns>
        public ShapeType GetType(ShapeType type) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets a string representing the Shape.</para>
        /// <para>This function can be useful for conditional debug drawing.</para>
        /// </summary>
        public void NewCircleShape() => throw new NotImplementedException();
        /// <summary>
        /// <para>Casts a ray against the shape and returns the surface normal vector and the line position where the ray hit. If the ray missed the shape, nil will be returned. The Shape can be transformed to get it into the desired position.</para>
        /// <para>The ray starts on the first point of the input line and goes towards the second point of the line. The fourth argument is the maximum distance the ray is going to travel as a scale factor of the input line length.</para>
        /// <para>The childIndex parameter is used to specify which child of a parent shape, such as a ChainShape, will be ray casted. For ChainShapes, the index of 1 is the first edge on the chain. Ray casting a parent shape will only test the child specified so if you want to test every shape of the parent, you must loop through all of its children.</para>
        /// <para>The world position of the impact can be calculated by multiplying the line vector with the third return value and adding it to the line starting point.</para>
        /// </summary>
        /// <param name="x1">The x position of the input line starting point.</param>
        /// <param name="y1">The y position of the input line starting point.</param>
        /// <param name="x2">The x position of the input line end point.</param>
        /// <param name="y2">The y position of the input line end point.</param>
        /// <param name="maxFraction">Ray length parameter.</param>
        /// <param name="tx">The translation of the shape on the x-axis.</param>
        /// <param name="ty">The translation of the shape on the y-axis.</param>
        /// <param name="tr">The shape rotation.</param>
        /// <param name="childIndex">The index of the child the ray gets cast against.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>xn</term><description>The x component of the normal vector of the edge where the ray hit the shape.</description></item>
        /// <item><term>yn</term><description>The y component of the normal vector of the edge where the ray hit the shape.</description></item>
        /// <item><term>fraction</term><description>The position on the input line where the intersection happened as a factor of the line length.</description></item>
        /// </list>
        /// </returns>
        public (double xn, double yn, double fraction) RayCast(double x1, double y1, double x2, double y2, double maxFraction, double tx, double ty, double tr, double childIndex = 1) => throw new NotImplementedException();
        /// <summary>
        /// <para>Casts a ray against the shape and returns the surface normal vector and the line position where the ray hit. If the ray missed the shape, nil will be returned. The Shape can be transformed to get it into the desired position.</para>
        /// <para>The ray starts on the first point of the input line and goes towards the second point of the line. The fourth argument is the maximum distance the ray is going to travel as a scale factor of the input line length.</para>
        /// <para>The childIndex parameter is used to specify which child of a parent shape, such as a ChainShape, will be ray casted. For ChainShapes, the index of 1 is the first edge on the chain. Ray casting a parent shape will only test the child specified so if you want to test every shape of the parent, you must loop through all of its children.</para>
        /// <para>The world position of the impact can be calculated by multiplying the line vector with the third return value and adding it to the line starting point.</para>
        /// </summary>
        public void SetMeter() => throw new NotImplementedException();
    }
}
