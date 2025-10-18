namespace Brine2D
{
    /// <summary>
    /// <para>Contacts are objects created to manage collisions in worlds.</para>
/// <para>The lifetimes of Contacts are short. When you receive them in callbacks, they may be destroyed immediately after the callback returns.  Cache their values instead of storing Contacts directly.</para>
    /// </summary>
    // TODO: Requires Review
    public class Contact
    {
        /// <summary>
        /// <para>Gets the child indices of the shapes of the two colliding fixtures. For ChainShapes, an index of 1 is the first edge in the chain.</para>
        /// <para>Used together with Fixture:rayCast or ChainShape:getChildEdge.</para>
        /// </summary>
        /// <param name="indexA">The child index of the first fixture's shape.</param>
        /// <param name="indexB">The child index of the second fixture's shape.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>indexA</term><description>The child index of the first fixture's shape.</description></item>
        /// <item><term>indexB</term><description>The child index of the second fixture's shape.</description></item>
        /// </list>
        /// </returns>
        public (double indexA, double indexB) GetChildren(double indexA, double indexB) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the two Fixtures that hold the shapes that are in contact.</para>
        /// </summary>
        /// <param name="fixtureA">The first Fixture.</param>
        /// <param name="fixtureB">The second Fixture.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>fixtureA</term><description>The first Fixture.</description></item>
        /// <item><term>fixtureB</term><description>The second Fixture.</description></item>
        /// </list>
        /// </returns>
        public (object fixtureA, object fixtureB) GetFixtures(object fixtureA, object fixtureB) => throw new NotImplementedException();
        /// <summary>
        /// <para>Get the friction between two shapes that are in contact.</para>
        /// </summary>
        /// <param name="friction">The friction of the contact.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>friction</term><description>The friction of the contact.</description></item>
        /// </list>
        /// </returns>
        public double GetFriction(double friction) => throw new NotImplementedException();
        /// <summary>
        /// <para>Get the normal vector between two shapes that are in contact.</para>
        /// <para>This function returns the coordinates of a unit vector that points from the first shape to the second.</para>
        /// </summary>
        /// <param name="nx">The x component of the normal vector.</param>
        /// <param name="ny">The y component of the normal vector.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>nx</term><description>The x component of the normal vector.</description></item>
        /// <item><term>ny</term><description>The y component of the normal vector.</description></item>
        /// </list>
        /// </returns>
        public (double nx, double ny) GetNormal(double nx, double ny) => throw new NotImplementedException();
        /// <summary>
        /// <para>Returns the contact points of the two colliding fixtures. There can be one or two points.</para>
        /// <para>If one of the colliding fixtures is a sensor, this will return nil.</para>
        /// </summary>
        /// <param name="x1">The x coordinate of the first contact point.</param>
        /// <param name="y1">The y coordinate of the first contact point.</param>
        /// <param name="x2">The x coordinate of the second contact point.</param>
        /// <param name="y2">The y coordinate of the second contact point.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>x1</term><description>The x coordinate of the first contact point.</description></item>
        /// <item><term>y1</term><description>The y coordinate of the first contact point.</description></item>
        /// <item><term>x2</term><description>The x coordinate of the second contact point.</description></item>
        /// <item><term>y2</term><description>The y coordinate of the second contact point.</description></item>
        /// </list>
        /// </returns>
        public (double x1, double y1, double x2, double y2) GetPositions(double x1, double y1, double x2, double y2) => throw new NotImplementedException();
        /// <summary>
        /// <para>Get the restitution between two shapes that are in contact.</para>
        /// </summary>
        /// <param name="restitution">The restitution between the two shapes.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>restitution</term><description>The restitution between the two shapes.</description></item>
        /// </list>
        /// </returns>
        public double GetRestitution(double restitution) => throw new NotImplementedException();
        /// <summary>
        /// <para>Returns whether the contact is enabled. The collision will be ignored if a contact gets disabled in the preSolve callback.</para>
        /// </summary>
        /// <param name="enabled">True if enabled, false otherwise.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>enabled</term><description>True if enabled, false otherwise.</description></item>
        /// </list>
        /// </returns>
        public bool IsEnabled(bool enabled) => throw new NotImplementedException();
        /// <summary>
        /// <para>Returns whether the two colliding fixtures are touching each other.</para>
        /// </summary>
        /// <param name="touching">True if they touch or false if not.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>touching</term><description>True if they touch or false if not.</description></item>
        /// </list>
        /// </returns>
        public bool IsTouching(bool touching) => throw new NotImplementedException();
        /// <summary>
        /// <para>Resets the contact friction to the mixture value of both fixtures.</para>
        /// </summary>
        public void ResetFriction() => throw new NotImplementedException();
        /// <summary>
        /// <para>Resets the contact restitution to the mixture value of both fixtures.</para>
        /// </summary>
        public void ResetRestitution() => throw new NotImplementedException();
        /// <summary>
        /// <para>Enables or disables the contact.</para>
        /// </summary>
        /// <param name="enabled">True to enable or false to disable.</param>
        public void SetEnabled(bool enabled) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the contact friction.</para>
        /// </summary>
        /// <param name="friction">The contact friction.</param>
        public void SetFriction(double friction) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the contact restitution.</para>
        /// </summary>
        /// <param name="restitution">The contact restitution.</param>
        public void SetRestitution(double restitution) => throw new NotImplementedException();
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
