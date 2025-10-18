namespace Brine2D
{
    /// <summary>
    /// <para>Fixtures attach shapes to bodies.</para>
    /// </summary>
    // TODO: Requires Review
    public class Fixture
    {
        /// <summary>
        /// <para>Destroys the fixture.</para>
        /// </summary>
        public void Destroy() => throw new NotImplementedException();
        /// <summary>
        /// <para>Returns the body to which the fixture is attached.</para>
        /// </summary>
        /// <param name="body">The parent body.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>body</term><description>The parent body.</description></item>
        /// </list>
        /// </returns>
        public object GetBody(object body) => throw new NotImplementedException();
        /// <summary>
        /// <para>Returns the points of the fixture bounding box. In case the fixture has multiple children a 1-based index can be specified. For example, a fixture will have multiple children with a chain shape.</para>
        /// </summary>
        /// <param name="index">A bounding box of the fixture.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>topLeftX</term><description>The x position of the top-left point.</description></item>
        /// <item><term>topLeftY</term><description>The y position of the top-left point.</description></item>
        /// <item><term>bottomRightX</term><description>The x position of the bottom-right point.</description></item>
        /// <item><term>bottomRightY</term><description>The y position of the bottom-right point.</description></item>
        /// </list>
        /// </returns>
        public (double topLeftX, double topLeftY, double bottomRightX, double bottomRightY) GetBoundingBox(double index = 1) => throw new NotImplementedException();
        /// <summary>
        /// <para>Returns the categories the fixture belongs to.</para>
        /// </summary>
        /// <param name="category1">The first category.</param>
        /// <param name="category2">The second category.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>category1</term><description>The first category.</description></item>
        /// <item><term>category2</term><description>The second category.</description></item>
        /// </list>
        /// </returns>
        public (double category1, double category2) GetCategory(double category1, double category2) => throw new NotImplementedException();
        /// <summary>
        /// <para>Returns the density of the fixture.</para>
        /// </summary>
        /// <param name="density">The fixture density in kilograms per square meter.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>density</term><description>The fixture density in kilograms per square meter.</description></item>
        /// </list>
        /// </returns>
        public double GetDensity(double density) => throw new NotImplementedException();
        /// <summary>
        /// <para>Returns the filter data of the fixture.</para>
        /// <para>Categories and masks are encoded as the bits of a 16-bit integer.</para>
        /// </summary>
        /// <param name="categories">The categories as an integer from 0 to 65535.</param>
        /// <param name="mask">The mask as an integer from 0 to 65535.</param>
        /// <param name="group">The group as an integer from -32768 to 32767.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>categories</term><description>The categories as an integer from 0 to 65535.</description></item>
        /// <item><term>mask</term><description>The mask as an integer from 0 to 65535.</description></item>
        /// <item><term>group</term><description>The group as an integer from -32768 to 32767.</description></item>
        /// </list>
        /// </returns>
        public (double categories, double mask, double group) GetFilterData(double categories, double mask, double group) => throw new NotImplementedException();
        /// <summary>
        /// <para>Returns the friction of the fixture.</para>
        /// </summary>
        /// <param name="friction">The fixture friction.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>friction</term><description>The fixture friction.</description></item>
        /// </list>
        /// </returns>
        public double GetFriction(double friction) => throw new NotImplementedException();
        /// <summary>
        /// <para>Returns the group the fixture belongs to. Fixtures with the same group will always collide if the group is positive or never collide if it's negative. The group zero means no group.</para>
        /// <para>The groups range from -32768 to 32767.</para>
        /// </summary>
        /// <param name="group">The group of the fixture.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>group</term><description>The group of the fixture.</description></item>
        /// </list>
        /// </returns>
        public double GetGroupIndex(double group) => throw new NotImplementedException();
        /// <summary>
        /// <para>Returns which categories this fixture should NOT collide with.</para>
        /// </summary>
        /// <param name="mask1">The first category selected by the mask.</param>
        /// <param name="mask2">The second category selected by the mask.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>mask1</term><description>The first category selected by the mask.</description></item>
        /// <item><term>mask2</term><description>The second category selected by the mask.</description></item>
        /// </list>
        /// </returns>
        public (double mask1, double mask2) GetMask(double mask1, double mask2) => throw new NotImplementedException();
        /// <summary>
        /// <para>Returns the mass, its center and the rotational inertia.</para>
        /// </summary>
        /// <param name="x">The x position of the center of mass.</param>
        /// <param name="y">The y position of the center of mass.</param>
        /// <param name="mass">The mass of the fixture.</param>
        /// <param name="inertia">The rotational inertia.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>x</term><description>The x position of the center of mass.</description></item>
        /// <item><term>y</term><description>The y position of the center of mass.</description></item>
        /// <item><term>mass</term><description>The mass of the fixture.</description></item>
        /// <item><term>inertia</term><description>The rotational inertia.</description></item>
        /// </list>
        /// </returns>
        public (double x, double y, double mass, double inertia) GetMassData(double x, double y, double mass, double inertia) => throw new NotImplementedException();
        /// <summary>
        /// <para>Returns the restitution of the fixture.</para>
        /// </summary>
        /// <param name="restitution">The fixture restitution.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>restitution</term><description>The fixture restitution.</description></item>
        /// </list>
        /// </returns>
        public double GetRestitution(double restitution) => throw new NotImplementedException();
        /// <summary>
        /// <para>Returns the Shape of the fixture. This shape is a reference to the actual data used in the simulation. It's possible to change its values between timesteps.</para>
        /// </summary>
        /// <param name="shape">The fixture's shape.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>shape</term><description>The fixture's shape.</description></item>
        /// </list>
        /// </returns>
        public object GetShape(object shape) => throw new NotImplementedException();
        /// <summary>
        /// <para>Returns the Lua value associated with this fixture.</para>
        /// </summary>
        /// <param name="value">The Lua value associated with the fixture.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>value</term><description>The Lua value associated with the fixture.</description></item>
        /// </list>
        /// </returns>
        public object GetUserData(object value) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets whether the Fixture is destroyed. Destroyed fixtures cannot be used.</para>
        /// </summary>
        /// <param name="destroyed">Whether the Fixture is destroyed.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>destroyed</term><description>Whether the Fixture is destroyed.</description></item>
        /// </list>
        /// </returns>
        public bool IsDestroyed(bool destroyed) => throw new NotImplementedException();
        /// <summary>
        /// <para>Returns whether the fixture is a sensor.</para>
        /// </summary>
        /// <param name="sensor">If the fixture is a sensor.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>sensor</term><description>If the fixture is a sensor.</description></item>
        /// </list>
        /// </returns>
        public bool IsSensor(bool sensor) => throw new NotImplementedException();
        /// <summary>
        /// <para>Casts a ray against the shape of the fixture and returns the surface normal vector and the line position where the ray hit. If the ray missed the shape, nil will be returned.</para>
        /// <para>The ray starts on the first point of the input line and goes towards the second point of the line. The fifth argument is the maximum distance the ray is going to travel as a scale factor of the input line length.</para>
        /// <para>The childIndex parameter is used to specify which child of a parent shape, such as a ChainShape, will be ray casted. For ChainShapes, the index of 1 is the first edge on the chain. Ray casting a parent shape will only test the child specified so if you want to test every shape of the parent, you must loop through all of its children.</para>
        /// <para>The world position of the impact can be calculated by multiplying the line vector with the third return value and adding it to the line starting point.</para>
        /// </summary>
        /// <param name="x1">The x position of the input line starting point.</param>
        /// <param name="y1">The y position of the input line starting point.</param>
        /// <param name="x2">The x position of the input line end point.</param>
        /// <param name="y2">The y position of the input line end point.</param>
        /// <param name="maxFraction">Ray length parameter.</param>
        /// <param name="childIndex">The index of the child the ray gets cast against.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>xn</term><description>The x component of the normal vector of the edge where the ray hit the shape.</description></item>
        /// <item><term>yn</term><description>The y component of the normal vector of the edge where the ray hit the shape.</description></item>
        /// <item><term>fraction</term><description>The position on the input line where the intersection happened as a factor of the line length.</description></item>
        /// </list>
        /// </returns>
        public (double xn, double yn, double fraction) RayCast(double x1, double y1, double x2, double y2, double maxFraction, double childIndex = 1) => throw new NotImplementedException();
        /// <summary>
        /// <para>Casts a ray against the shape of the fixture and returns the surface normal vector and the line position where the ray hit. If the ray missed the shape, nil will be returned.</para>
        /// <para>The ray starts on the first point of the input line and goes towards the second point of the line. The fifth argument is the maximum distance the ray is going to travel as a scale factor of the input line length.</para>
        /// <para>The childIndex parameter is used to specify which child of a parent shape, such as a ChainShape, will be ray casted. For ChainShapes, the index of 1 is the first edge on the chain. Ray casting a parent shape will only test the child specified so if you want to test every shape of the parent, you must loop through all of its children.</para>
        /// <para>The world position of the impact can be calculated by multiplying the line vector with the third return value and adding it to the line starting point.</para>
        /// </summary>
        public void SetMeter() => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the categories the fixture belongs to. There can be up to 16 categories represented as a number from 1 to 16.</para>
        /// <para>All fixture's default category is 1.</para>
        /// </summary>
        /// <param name="category1">The first category.</param>
        /// <param name="category2">The second category.</param>
        public void SetCategory(double category1, double category2) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the density of the fixture. Call Body:resetMassData if this needs to take effect immediately.</para>
        /// </summary>
        /// <param name="density">The fixture density in kilograms per square meter.</param>
        public void SetDensity(double density) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the filter data of the fixture.</para>
        /// <para>Groups, categories, and mask can be used to define the collision behaviour of the fixture.</para>
        /// <para>If two fixtures are in the same group they either always collide if the group is positive, or never collide if it's negative. If the group is zero or they do not match, then the contact filter checks if the fixtures select a category of the other fixture with their masks. The fixtures do not collide if that's not the case. If they do have each other's categories selected, the return value of the custom contact filter will be used. They always collide if none was set.</para>
        /// <para>There can be up to 16 categories. Categories and masks are encoded as the bits of a 16-bit integer.</para>
        /// <para>When created, prior to calling this function, all fixtures have category set to 1, mask set to 65535 (all categories) and group set to 0.</para>
        /// <para>This function allows setting all filter data for a fixture at once. To set only the categories, the mask or the group, you can use Fixture:setCategory, Fixture:setMask or Fixture:setGroupIndex respectively.</para>
        /// </summary>
        /// <param name="categories">The categories as an integer from 0 to 65535.</param>
        /// <param name="mask">The mask as an integer from 0 to 65535.</param>
        /// <param name="group">The group as an integer from -32768 to 32767.</param>
        public void SetFilterData(double categories, double mask, double group) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the friction of the fixture.</para>
        /// <para>Friction determines how shapes react when they "slide" along other shapes. Low friction indicates a slippery surface, like ice, while high friction indicates a rough surface, like concrete. Range: [0.0, 1.0]</para>
        /// </summary>
        /// <param name="friction">The fixture friction.</param>
        public void SetFriction(double friction) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the group the fixture belongs to. Fixtures with the same group will always collide if the group is positive or never collide if it's negative. The group zero means no group.</para>
        /// <para>The groups range from -32768 to 32767.</para>
        /// </summary>
        /// <param name="group">The group as an integer from -32768 to 32767.</param>
        public void SetGroupIndex(double group) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the category masks of the fixture. There can be up to 16 categories represented as a number from 1 to 16.</para>
        /// <para>This fixture will NOT collide with the fixtures that are in the selected categories if the other fixture also has a category of this fixture selected.</para>
        /// <para>Calling this function with no arguments will make the fixture have no masks.</para>
        /// </summary>
        /// <param name="mask1">The first category.</param>
        /// <param name="mask2">The second category.</param>
        /// <param name="">Additional categories.</param>
        // TODO: public void SetMask(double mask1, double mask2, object) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the category masks of the fixture. There can be up to 16 categories represented as a number from 1 to 16.</para>
        /// <para>This fixture will NOT collide with the fixtures that are in the selected categories if the other fixture also has a category of this fixture selected.</para>
        /// <para>Calling this function with no arguments will make the fixture have no masks.</para>
        /// </summary>
        public void SetCategory() => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the restitution of the fixture. Range: [0.0, 1.0]</para>
        /// <para>Restitution determines how much a body bounces after a collision. The value should be greater than zero and is usually in [0, 1].</para>
        /// <para>You can think of restitution as a scale on the reflected velocity after a collision. A bouncy ball would have a high restitution (an elastic collision keeps most of the velocity) and a brick a low restitution (inelastic collision loses most of the velocity). A value of one would be perfectly elastic and maintain all of its velocity.</para>
        /// <para>For more detail, see how Box2D's ContactSolver uses restitution to scale the velocityBias and how it preserves the higher restitution of two bodies to bias towards bounciness.</para>
        /// </summary>
        /// <param name="restitution">The fixture restitution.</param>
        public void SetRestitution(double restitution) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets whether the fixture should act as a sensor and trigger the beginContact callback.</para>
        /// <para>Fixtures with sensors do not cause collision responses (they pass through other objects instead of bouncing off), but the beginContact and endContact World callbacks will still be called for this fixture.</para>
        /// <para>When dealing with sensor fixtures, a Contact object behaves differently than with solid fixtures. Specifically:</para>
        /// </summary>
        /// <param name="sensor">The sensor status.</param>
        public void SetSensor(bool sensor) => throw new NotImplementedException();
        /// <summary>
        /// <para>Associates a Lua value with the fixture.</para>
        /// <para>To delete the reference, explicitly pass nil.</para>
        /// </summary>
        /// <param name="value">The Lua value to associate with the fixture.</param>
        public void SetUserData(object value) => throw new NotImplementedException();
        /// <summary>
        /// <para>Checks if a point is inside the shape of the fixture.</para>
        /// </summary>
        /// <param name="x">The x position of the point.</param>
        /// <param name="y">The y position of the point.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>isInside</term><description>True if the point is inside or false if it is outside.</description></item>
        /// </list>
        /// </returns>
        public bool TestPoint(double x, double y) => throw new NotImplementedException();
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
