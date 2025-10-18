namespace Brine2D
{
    /// <summary>
    /// <para>Attach multiple bodies together to interact in unique ways.</para>
    /// </summary>
    // TODO: Requires Review
    public class Joint
    {
        /// <summary>
        /// <para>Explicitly destroys the Joint. An error will occur if you attempt to use the object after calling this function.</para>
        /// <para>In 0.7.2, when you don't have time to wait for garbage collection, this function</para>
        /// <para>may be used to free the object immediately.</para>
        /// </summary>
        public void Destroy() => throw new NotImplementedException();
        /// <summary>
        /// <para>Get the anchor points of the joint.</para>
        /// </summary>
        /// <param name="x1">The x-component of the anchor on Body 1.</param>
        /// <param name="y1">The y-component of the anchor on Body 1.</param>
        /// <param name="x2">The x-component of the anchor on Body 2.</param>
        /// <param name="y2">The y-component of the anchor on Body 2.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>x1</term><description>The x-component of the anchor on Body 1.</description></item>
        /// <item><term>y1</term><description>The y-component of the anchor on Body 1.</description></item>
        /// <item><term>x2</term><description>The x-component of the anchor on Body 2.</description></item>
        /// <item><term>y2</term><description>The y-component of the anchor on Body 2.</description></item>
        /// </list>
        /// </returns>
        public (double x1, double y1, double x2, double y2) GetAnchors(double x1, double y1, double x2, double y2) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the bodies that the Joint is attached to.</para>
        /// </summary>
        /// <param name="bodyA">The first Body.</param>
        /// <param name="bodyB">The second Body.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>bodyA</term><description>The first Body.</description></item>
        /// <item><term>bodyB</term><description>The second Body.</description></item>
        /// </list>
        /// </returns>
        public (object bodyA, object bodyB) GetBodies(object bodyA = null, object bodyB = null) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets whether the connected Bodies collide.</para>
        /// </summary>
        /// <param name="c">True if they collide, false otherwise.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>c</term><description>True if they collide, false otherwise.</description></item>
        /// </list>
        /// </returns>
        public bool GetCollideConnected(bool c) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets a string representing the type.</para>
        /// </summary>
        /// <param name="type">A string with the name of the Joint type.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>type</term><description>A string with the name of the Joint type.</description></item>
        /// </list>
        /// </returns>
        public JointType GetType(JointType type) => throw new NotImplementedException();
        /// <summary>
        /// <para>Returns the Lua value associated with this Joint.</para>
        /// </summary>
        /// <param name="value">The Lua value associated with the Joint.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>value</term><description>The Lua value associated with the Joint.</description></item>
        /// </list>
        /// </returns>
        public object GetUserData(object value) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets whether the Joint is destroyed. Destroyed joints cannot be used.</para>
        /// </summary>
        /// <param name="destroyed">Whether the Joint is destroyed.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>destroyed</term><description>Whether the Joint is destroyed.</description></item>
        /// </list>
        /// </returns>
        public bool IsDestroyed(bool destroyed) => throw new NotImplementedException();
        /// <summary>
        /// <para>Associates a Lua value with the Joint.</para>
        /// <para>To delete the reference, explicitly pass nil.</para>
        /// </summary>
        /// <param name="value">The Lua value to associate with the Joint.</param>
        public void SetUserData(object value) => throw new NotImplementedException();
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
