namespace Brine2D
{
    /// <summary>
    /// <para>A world is an object that contains all bodies and joints.</para>
    /// </summary>
    // TODO: Requires Review
    public class World
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
        /// <para>Destroys the world, taking all bodies, joints, fixtures and their shapes with it.</para>
        /// <para>An error will occur if you attempt to use any of the destroyed objects after calling this function.</para>
        /// </summary>
        public void Destroy() => throw new NotImplementedException();
        /// <summary>
        /// <para>Returns a table with all bodies.</para>
        /// </summary>
        /// <param name="bodies">A with all bodies.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>bodies</term><description>A with all bodies.</description></item>
        /// </list>
        /// </returns>
        public object GetBodies(object bodies) => throw new NotImplementedException();
        /// <summary>
        /// <para>Returns the number of bodies in the world.</para>
        /// </summary>
        /// <param name="n">The number of bodies in the world.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>n</term><description>The number of bodies in the world.</description></item>
        /// </list>
        /// </returns>
        public double GetBodyCount(double n) => throw new NotImplementedException();
        /// <summary>
        /// <para>Returns the number of contacts in the world.</para>
        /// </summary>
        /// <param name="n">The number of contacts in the world.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>n</term><description>The number of contacts in the world.</description></item>
        /// </list>
        /// </returns>
        public double GetContactCount(double n) => throw new NotImplementedException();
        /// <summary>
        /// <para>Returns the function for collision filtering.</para>
        /// </summary>
        /// <param name="contactFilter">The function that handles the contact filtering.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>contactFilter</term><description>The function that handles the contact filtering.</description></item>
        /// </list>
        /// </returns>
        public object GetContactFilter(object contactFilter) => throw new NotImplementedException();
        /// <summary>
        /// <para>Returns a table with all Contacts.</para>
        /// </summary>
        /// <param name="contacts">A with all .</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>contacts</term><description>A with all .</description></item>
        /// </list>
        /// </returns>
        public object GetContacts(object contacts) => throw new NotImplementedException();
        /// <summary>
        /// <para>Get the gravity of the world.</para>
        /// </summary>
        /// <param name="x">The x component of gravity.</param>
        /// <param name="y">The y component of gravity.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>x</term><description>The x component of gravity.</description></item>
        /// <item><term>y</term><description>The y component of gravity.</description></item>
        /// </list>
        /// </returns>
        public (double x, double y) GetGravity(double x, double y) => throw new NotImplementedException();
        /// <summary>
        /// <para>Returns the number of joints in the world.</para>
        /// </summary>
        /// <param name="n">The number of joints in the world.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>n</term><description>The number of joints in the world.</description></item>
        /// </list>
        /// </returns>
        public double GetJointCount(double n) => throw new NotImplementedException();
        /// <summary>
        /// <para>Returns a table with all joints.</para>
        /// </summary>
        /// <param name="joints">A with all joints.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>joints</term><description>A with all joints.</description></item>
        /// </list>
        /// </returns>
        public object GetJoints(object joints) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets whether the World is destroyed. Destroyed worlds cannot be used.</para>
        /// </summary>
        /// <param name="destroyed">Whether the World is destroyed.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>destroyed</term><description>Whether the World is destroyed.</description></item>
        /// </list>
        /// </returns>
        public bool IsDestroyed(bool destroyed) => throw new NotImplementedException();
        /// <summary>
        /// <para>Returns if the world is updating its state.</para>
        /// <para>This will return true inside the callbacks from World:setCallbacks.</para>
        /// </summary>
        /// <param name="locked">Will be true if the world is in the process of updating its state.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>locked</term><description>Will be true if the world is in the process of updating its state.</description></item>
        /// </list>
        /// </returns>
        public bool IsLocked(bool locked) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the sleep behaviour of the world.</para>
        /// </summary>
        /// <param name="allow">True if bodies in the world are allowed to sleep, or false if not.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>allow</term><description>True if bodies in the world are allowed to sleep, or false if not.</description></item>
        /// </list>
        /// </returns>
        public bool IsSleepingAllowed(bool allow) => throw new NotImplementedException();
        /// <summary>
        /// <para>Calls a function for each fixture inside the specified area by searching for any overlapping bounding box (Fixture:getBoundingBox).</para>
        /// </summary>
        /// <param name="topLeftX">The x position of the top-left point.</param>
        /// <param name="topLeftY">The y position of the top-left point.</param>
        /// <param name="bottomRightX">The x position of the bottom-right point.</param>
        /// <param name="bottomRightY">The y position of the bottom-right point.</param>
        /// <param name="callback">This function gets passed one argument, the fixture, and should return a boolean. The search will continue if it is true or stop if it is false.</param>
        public void QueryBoundingBox(double topLeftX, double topLeftY, double bottomRightX, double bottomRightY, object callback) => throw new NotImplementedException();
        /// <summary>
        /// <para>Casts a ray and calls a function for each fixtures it intersects.</para>
        /// </summary>
        /// <param name="x1">The x position of the starting point of the ray.</param>
        /// <param name="y1">The y position of the starting point of the ray.</param>
        /// <param name="x2">The x position of the end point of the ray.</param>
        /// <param name="y2">The y position of the end point of the ray.</param>
        /// <param name="callback">A function called for each fixture intersected by the ray. The function gets six arguments and should return a number as a control value. The intersection points fed into the function will be in an arbitrary order. If you wish to find the closest point of intersection, you'll need to do that yourself within the function. The easiest way to do that is by using the fraction value.</param>
        public void RayCast(double x1, double y1, double x2, double y2, object callback) => throw new NotImplementedException();
        /// <summary>
        /// <para>Casts a ray and calls a function for each fixtures it intersects.</para>
        /// </summary>
        public void NewRectangleShape() => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets a function for collision filtering.</para>
        /// <para>If the group and category filtering doesn't generate a collision decision, this function gets called with the two fixtures as arguments. The function should return a boolean value where true means the fixtures will collide and false means they will pass through each other.</para>
        /// </summary>
        /// <param name="filter">The function handling the contact filtering.</param>
        public void SetContactFilter(object filter) => throw new NotImplementedException();
        /// <summary>
        /// <para>Set the gravity of the world.</para>
        /// </summary>
        /// <param name="x">The x component of gravity.</param>
        /// <param name="y">The y component of gravity.</param>
        public void SetGravity(double x, double y) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the sleep behaviour of the world.</para>
        /// </summary>
        /// <param name="allow">True if bodies in the world are allowed to sleep, or false if not.</param>
        public void SetSleepingAllowed(bool allow) => throw new NotImplementedException();
        /// <summary>
        /// <para>Translates the World's origin. Useful in large worlds where floating point precision issues become noticeable at far distances from the origin.</para>
        /// </summary>
        /// <param name="x">The x component of the new origin with respect to the old origin.</param>
        /// <param name="y">The y component of the new origin with respect to the old origin.</param>
        public void TranslateOrigin(double x, double y) => throw new NotImplementedException();
        /// <summary>
        /// <para>Update the state of the world.</para>
        /// </summary>
        /// <param name="dt">The time (in seconds) to advance the physics simulation.</param>
        public void Update(double dt) => throw new NotImplementedException();
    }
}
