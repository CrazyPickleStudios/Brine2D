namespace Brine2D
{
    /// <summary>
    /// <para>Bodies are objects with velocity and position.</para>
    /// </summary>
    // TODO: Requires Review
    public class Body
    {
        /// <summary>
        /// <para>Applies an angular impulse to a body. This makes a single, instantaneous addition to the body momentum. Applying an angular impulse large enough will rotate the body.</para>
        /// <para>A body with with a larger mass will react less. The reaction does not depend on the timestep, and is equivalent to applying a force continuously for 1 second. Impulses are best used to give a single push to a body. For a continuous push to a body it is better to use Body:applyTorque.</para>
        /// </summary>
        /// <param name="impulse">The impulse in kilogram-square meter per second.</param>
        public void ApplyAngularImpulse(double impulse) => throw new NotImplementedException();
        /// <summary>
        /// <para>Apply force to a Body.</para>
        /// <para>A force pushes a body in a direction. A body with with a larger mass will react less. The reaction also depends on how long a force is applied: since the force acts continuously over the entire timestep, a short timestep will only push the body for a short time. Thus forces are best used for many timesteps to give a continuous push to a body (like gravity). For a single push that is independent of timestep, it is better to use Body:applyLinearImpulse.</para>
        /// <para>If the position to apply the force is not given, it will act on the center of mass of the body. The part of the force not directed towards the center of mass will cause the body to spin (and depends on the rotational inertia).</para>
        /// <para>Note that the force components and position must be given in world coordinates.</para>
        /// </summary>
        /// <param name="fx">The x component of force to apply to the center of mass.</param>
        /// <param name="fy">The y component of force to apply to the center of mass.</param>
        public void ApplyForce(double fx, double fy) => throw new NotImplementedException();
        /// <summary>
        /// <para>Apply force to a Body.</para>
        /// <para>A force pushes a body in a direction. A body with with a larger mass will react less. The reaction also depends on how long a force is applied: since the force acts continuously over the entire timestep, a short timestep will only push the body for a short time. Thus forces are best used for many timesteps to give a continuous push to a body (like gravity). For a single push that is independent of timestep, it is better to use Body:applyLinearImpulse.</para>
        /// <para>If the position to apply the force is not given, it will act on the center of mass of the body. The part of the force not directed towards the center of mass will cause the body to spin (and depends on the rotational inertia).</para>
        /// <para>Note that the force components and position must be given in world coordinates.</para>
        /// </summary>
        /// <param name="fx">The x component of force to apply.</param>
        /// <param name="fy">The y component of force to apply.</param>
        /// <param name="x">The x position to apply the force.</param>
        /// <param name="y">The y position to apply the force.</param>
        public void ApplyForce(double fx, double fy, double x, double y) => throw new NotImplementedException();
        /// <summary>
        /// <para>Applies an impulse to a body.</para>
        /// <para>This makes a single, instantaneous addition to the body momentum.</para>
        /// <para>An impulse pushes a body in a direction. A body with with a larger mass will react less. The reaction does not depend on the timestep, and is equivalent to applying a force continuously for 1 second. Impulses are best used to give a single push to a body. For a continuous push to a body it is better to use Body:applyForce.</para>
        /// <para>If the position to apply the impulse is not given, it will act on the center of mass of the body. The part of the impulse not directed towards the center of mass will cause the body to spin (and depends on the rotational inertia).</para>
        /// <para>Note that the impulse components and position must be given in world coordinates.</para>
        /// </summary>
        /// <param name="ix">The x component of the impulse applied to the center of mass.</param>
        /// <param name="iy">The y component of the impulse applied to the center of mass.</param>
        public void ApplyLinearImpulse(double ix, double iy) => throw new NotImplementedException();
        /// <summary>
        /// <para>Applies an impulse to a body.</para>
        /// <para>This makes a single, instantaneous addition to the body momentum.</para>
        /// <para>An impulse pushes a body in a direction. A body with with a larger mass will react less. The reaction does not depend on the timestep, and is equivalent to applying a force continuously for 1 second. Impulses are best used to give a single push to a body. For a continuous push to a body it is better to use Body:applyForce.</para>
        /// <para>If the position to apply the impulse is not given, it will act on the center of mass of the body. The part of the impulse not directed towards the center of mass will cause the body to spin (and depends on the rotational inertia).</para>
        /// <para>Note that the impulse components and position must be given in world coordinates.</para>
        /// </summary>
        /// <param name="ix">The x component of the impulse.</param>
        /// <param name="iy">The y component of the impulse.</param>
        /// <param name="x">The x position to apply the impulse.</param>
        /// <param name="y">The y position to apply the impulse.</param>
        public void ApplyLinearImpulse(double ix, double iy, double x, double y) => throw new NotImplementedException();
        /// <summary>
        /// <para>Apply torque to a body.</para>
        /// <para>Torque is like a force that will change the angular velocity (spin) of a body. The effect will depend on the rotational inertia a body has.</para>
        /// </summary>
        /// <param name="torque">The torque to apply.</param>
        public void ApplyTorque(double torque) => throw new NotImplementedException();
        /// <summary>
        /// <para>Explicitly destroys the Body and all fixtures and joints attached to it.</para>
        /// <para>An error will occur if you attempt to use the object after calling this function. In 0.7.2, when you don't have time to wait for garbage collection, this function may be used to free the object immediately.</para>
        /// </summary>
        public void Destroy() => throw new NotImplementedException();
        /// <summary>
        /// <para>Return whether a body is allowed to sleep.</para>
        /// <para>A sleeping body is much more efficient to simulate than when awake.</para>
        /// </summary>
        /// <param name="status">True when the body is allowed to sleep.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>status</term><description>True when the body is allowed to sleep.</description></item>
        /// </list>
        /// </returns>
        public bool GetAllowSleeping(bool status) => throw new NotImplementedException();
        /// <summary>
        /// <para>Get the angle of the body.</para>
        /// <para>The angle is measured in radians. If you need to transform it to degrees, use math.deg.</para>
        /// <para>A value of 0 radians will mean "looking to the right". Although radians increase counter-clockwise, the y axis points down so it becomes clockwise from our point of view.</para>
        /// </summary>
        /// <param name="angle">The angle in radians.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>angle</term><description>The angle in radians.</description></item>
        /// </list>
        /// </returns>
        public double GetAngle(double angle) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the Angular damping of the Body</para>
        /// <para>The angular damping is the rate of decrease of the angular velocity over time: A spinning body with no damping and no external forces will continue spinning indefinitely. A spinning body with damping will gradually stop spinning.</para>
        /// <para>Damping is not the same as friction - they can be modelled together. However, only damping is provided by Box2D (and LOVE).</para>
        /// <para>Damping parameters should be between 0 and infinity, with 0 meaning no damping, and infinity meaning full damping. Normally you will use a damping value between 0 and 0.1.</para>
        /// </summary>
        /// <param name="damping">The value of the angular damping.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>damping</term><description>The value of the angular damping.</description></item>
        /// </list>
        /// </returns>
        public double GetAngularDamping(double damping) => throw new NotImplementedException();
        /// <summary>
        /// <para>Get the angular velocity of the Body.</para>
        /// <para>The angular velocity is the rate of change of angle over time.</para>
        /// <para>It is changed in World:update by applying torques, off centre forces/impulses, and angular damping. It can be set directly with Body:setAngularVelocity.</para>
        /// <para>If you need the rate of change of position over time, use Body:getLinearVelocity.</para>
        /// </summary>
        /// <param name="w">The angular velocity in radians/second.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>w</term><description>The angular velocity in radians/second.</description></item>
        /// </list>
        /// </returns>
        public double GetAngularVelocity(double w) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets a list of all Contacts attached to the Body.</para>
        /// </summary>
        /// <param name="contacts">A list with all contacts associated with the Body.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>contacts</term><description>A list with all contacts associated with the Body.</description></item>
        /// </list>
        /// </returns>
        public object GetContacts(object contacts) => throw new NotImplementedException();
        /// <summary>
        /// <para>Returns a table with all fixtures.</para>
        /// </summary>
        /// <param name="fixtures">A with all .</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>fixtures</term><description>A with all .</description></item>
        /// </list>
        /// </returns>
        public object GetFixtures(object fixtures) => throw new NotImplementedException();
        /// <summary>
        /// <para>Returns the gravity scale factor.</para>
        /// </summary>
        /// <param name="scale">The gravity scale factor.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>scale</term><description>The gravity scale factor.</description></item>
        /// </list>
        /// </returns>
        public double GetGravityScale(double scale) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the rotational inertia of the body.</para>
        /// <para>The rotational inertia is how hard is it to make the body spin.</para>
        /// </summary>
        /// <param name="inertia">The rotational inertial of the body.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>inertia</term><description>The rotational inertial of the body.</description></item>
        /// </list>
        /// </returns>
        public double GetInertia(double inertia) => throw new NotImplementedException();
        /// <summary>
        /// <para>Returns a table containing the Joints attached to this Body.</para>
        /// </summary>
        /// <param name="joints">A with the attached to the Body.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>joints</term><description>A with the attached to the Body.</description></item>
        /// </list>
        /// </returns>
        public object GetJoints(object joints) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the linear damping of the Body.</para>
        /// <para>The linear damping is the rate of decrease of the linear velocity over time. A moving body with no damping and no external forces will continue moving indefinitely, as is the case in space. A moving body with damping will gradually stop moving.</para>
        /// <para>Damping is not the same as friction - they can be modelled together.</para>
        /// </summary>
        /// <param name="damping">The value of the linear damping.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>damping</term><description>The value of the linear damping.</description></item>
        /// </list>
        /// </returns>
        public double GetLinearDamping(double damping) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the linear velocity of the Body from its center of mass.</para>
        /// <para>The linear velocity is the rate of change of position over time.</para>
        /// <para>If you need the rate of change of angle over time, use Body:getAngularVelocity.</para>
        /// <para>If you need to get the linear velocity of a point different from the center of mass:</para>
        /// <para>See page 136 of "Essential Mathematics for Games and Interactive Applications" for definitions of local and world coordinates.</para>
        /// </summary>
        /// <param name="x">The x-component of the velocity vector</param>
        /// <param name="y">The y-component of the velocity vector</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>x</term><description>The x-component of the velocity vector</description></item>
        /// <item><term>y</term><description>The y-component of the velocity vector</description></item>
        /// </list>
        /// </returns>
        public (double x, double y) GetLinearVelocity(double x, double y) => throw new NotImplementedException();
        /// <summary>
        /// <para>Get the linear velocity of a point on the body.</para>
        /// <para>The linear velocity for a point on the body is the velocity of the body center of mass plus the velocity at that point from the body spinning.</para>
        /// <para>The point on the body must given in local coordinates. Use Body:getLinearVelocityFromWorldPoint to specify this with world coordinates.</para>
        /// </summary>
        /// <param name="x">The x position to measure velocity.</param>
        /// <param name="y">The y position to measure velocity.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>vx</term><description>The x component of velocity at point (x,y).</description></item>
        /// <item><term>vy</term><description>The y component of velocity at point (x,y).</description></item>
        /// </list>
        /// </returns>
        public (double vx, double vy) GetLinearVelocityFromLocalPoint(double x, double y) => throw new NotImplementedException();
        /// <summary>
        /// <para>Get the linear velocity of a point on the body.</para>
        /// <para>The linear velocity for a point on the body is the velocity of the body center of mass plus the velocity at that point from the body spinning.</para>
        /// <para>The point on the body must given in world coordinates. Use Body:getLinearVelocityFromLocalPoint to specify this with local coordinates.</para>
        /// </summary>
        /// <param name="x">The x position to measure velocity.</param>
        /// <param name="y">The y position to measure velocity.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>vx</term><description>The x component of velocity at point (x,y).</description></item>
        /// <item><term>vy</term><description>The y component of velocity at point (x,y).</description></item>
        /// </list>
        /// </returns>
        public (double vx, double vy) GetLinearVelocityFromWorldPoint(double x, double y) => throw new NotImplementedException();
        /// <summary>
        /// <para>Get the center of mass position in local coordinates.</para>
        /// <para>Use Body:getWorldCenter to get the center of mass in world coordinates.</para>
        /// </summary>
        /// <param name="x">The x coordinate of the center of mass.</param>
        /// <param name="y">The y coordinate of the center of mass.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>x</term><description>The x coordinate of the center of mass.</description></item>
        /// <item><term>y</term><description>The y coordinate of the center of mass.</description></item>
        /// </list>
        /// </returns>
        public (double x, double y) GetLocalCenter(double x, double y) => throw new NotImplementedException();
        /// <summary>
        /// <para>Transform a point from world coordinates to local coordinates.</para>
        /// </summary>
        /// <param name="worldX">The x position in world coordinates.</param>
        /// <param name="worldY">The y position in world coordinates.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>localX</term><description>The x position in local coordinates.</description></item>
        /// <item><term>localY</term><description>The y position in local coordinates.</description></item>
        /// </list>
        /// </returns>
        public (double localX, double localY) GetLocalPoint(double worldX, double worldY) => throw new NotImplementedException();
        /// <summary>
        /// <para>Transforms multiple points from world coordinates to local coordinates.</para>
        /// </summary>
        /// <param name="x1">The x position of the first point.</param>
        /// <param name="y1">The y position of the first point.</param>
        /// <param name="x2">The x position of the second point.</param>
        /// <param name="y2">The y position of the second point.</param>
        /// <param name="">You can continue passing x and y position of the points.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>x1</term><description>The transformed x position of the first point.</description></item>
        /// <item><term>y1</term><description>The transformed y position of the first point.</description></item>
        /// <item><term>x2</term><description>The transformed x position of the second point.</description></item>
        /// <item><term>y2</term><description>The transformed y position of the second point.</description></item>
        /// <item><term></term><description>Additional transformed x and y position of the points.</description></item>
        /// </list>
        /// </returns>
        // TODO: public (double x1, double y1, double x2, double y2, object) GetLocalPoints(double x1, double y1, double x2, double y2, object) => throw new NotImplementedException();
        /// <summary>
        /// <para>Transform a vector from world coordinates to local coordinates.</para>
        /// </summary>
        /// <param name="worldX">The vector x component in world coordinates.</param>
        /// <param name="worldY">The vector y component in world coordinates.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>localX</term><description>The vector x component in local coordinates.</description></item>
        /// <item><term>localY</term><description>The vector y component in local coordinates.</description></item>
        /// </list>
        /// </returns>
        public (double localX, double localY) GetLocalVector(double worldX, double worldY) => throw new NotImplementedException();
        /// <summary>
        /// <para>Get the mass of the body.</para>
        /// <para>Static bodies always have a mass of 0.</para>
        /// </summary>
        /// <param name="mass">The mass of the body (in kilograms).</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>mass</term><description>The mass of the body (in kilograms).</description></item>
        /// </list>
        /// </returns>
        public double GetMass(double mass) => throw new NotImplementedException();
        /// <summary>
        /// <para>Returns the mass, its center, and the rotational inertia.</para>
        /// </summary>
        /// <param name="x">The x position of the center of mass.</param>
        /// <param name="y">The y position of the center of mass.</param>
        /// <param name="mass">The mass of the body.</param>
        /// <param name="inertia">The rotational inertia.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>x</term><description>The x position of the center of mass.</description></item>
        /// <item><term>y</term><description>The y position of the center of mass.</description></item>
        /// <item><term>mass</term><description>The mass of the body.</description></item>
        /// <item><term>inertia</term><description>The rotational inertia.</description></item>
        /// </list>
        /// </returns>
        public (double x, double y, double mass, double inertia) GetMassData(double x, double y, double mass, double inertia) => throw new NotImplementedException();
        /// <summary>
        /// <para>Get the position of the body.</para>
        /// <para>Note that this may not be the center of mass of the body.</para>
        /// </summary>
        /// <param name="x">The x position.</param>
        /// <param name="y">The y position.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>x</term><description>The x position.</description></item>
        /// <item><term>y</term><description>The y position.</description></item>
        /// </list>
        /// </returns>
        public (double x, double y) GetPosition(double x, double y) => throw new NotImplementedException();
        /// <summary>
        /// <para>Returns the type of the body.</para>
        /// </summary>
        /// <param name="type">The body type.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>type</term><description>The body type.</description></item>
        /// </list>
        /// </returns>
        public BodyType GetType(BodyType type) => throw new NotImplementedException();
        /// <summary>
        /// <para>Returns the Lua value associated with this Body.</para>
        /// </summary>
        /// <param name="value">The Lua value associated with the Body.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>value</term><description>The Lua value associated with the Body.</description></item>
        /// </list>
        /// </returns>
        public object GetUserData(object value) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the World the body lives in.</para>
        /// </summary>
        /// <param name="world">The world the body lives in.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>world</term><description>The world the body lives in.</description></item>
        /// </list>
        /// </returns>
        public object GetWorld(object world) => throw new NotImplementedException();
        /// <summary>
        /// <para>Get the center of mass position in world coordinates.</para>
        /// <para>Use Body:getLocalCenter to get the center of mass in local coordinates.</para>
        /// </summary>
        /// <param name="x">The x coordinate of the center of mass.</param>
        /// <param name="y">The y coordinate of the center of mass.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>x</term><description>The x coordinate of the center of mass.</description></item>
        /// <item><term>y</term><description>The y coordinate of the center of mass.</description></item>
        /// </list>
        /// </returns>
        public (double x, double y) GetWorldCenter(double x, double y) => throw new NotImplementedException();
        /// <summary>
        /// <para>Transform a point from local coordinates to world coordinates.</para>
        /// </summary>
        /// <param name="localX">The x position in local coordinates.</param>
        /// <param name="localY">The y position in local coordinates.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>worldX</term><description>The x position in world coordinates.</description></item>
        /// <item><term>worldY</term><description>The y position in world coordinates.</description></item>
        /// </list>
        /// </returns>
        public (double worldX, double worldY) GetWorldPoint(double localX, double localY) => throw new NotImplementedException();
        /// <summary>
        /// <para>Transforms multiple points from local coordinates to world coordinates.</para>
        /// </summary>
        /// <param name="x1">The x position of the first point.</param>
        /// <param name="y1">The y position of the first point.</param>
        /// <param name="x2">The x position of the second point.</param>
        /// <param name="y2">The y position of the second point.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>x1</term><description>The transformed x position of the first point.</description></item>
        /// <item><term>y1</term><description>The transformed y position of the first point.</description></item>
        /// <item><term>x2</term><description>The transformed x position of the second point.</description></item>
        /// <item><term>y2</term><description>The transformed y position of the second point.</description></item>
        /// </list>
        /// </returns>
        public (double x1, double y1, double x2, double y2) GetWorldPoints(double x1, double y1, double x2, double y2) => throw new NotImplementedException();
        /// <summary>
        /// <para>Transform a vector from local coordinates to world coordinates.</para>
        /// </summary>
        /// <param name="localX">The vector x component in local coordinates.</param>
        /// <param name="localY">The vector y component in local coordinates.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>worldX</term><description>The vector x component in world coordinates.</description></item>
        /// <item><term>worldY</term><description>The vector y component in world coordinates.</description></item>
        /// </list>
        /// </returns>
        public (double worldX, double worldY) GetWorldVector(double localX, double localY) => throw new NotImplementedException();
        /// <summary>
        /// <para>Get the x position of the body in world coordinates.</para>
        /// </summary>
        /// <param name="x">The x position in world coordinates.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>x</term><description>The x position in world coordinates.</description></item>
        /// </list>
        /// </returns>
        public double GetX(double x) => throw new NotImplementedException();
        /// <summary>
        /// <para>Get the y position of the body in world coordinates.</para>
        /// </summary>
        /// <param name="y">The y position in world coordinates.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>y</term><description>The y position in world coordinates.</description></item>
        /// </list>
        /// </returns>
        public double GetY(double y) => throw new NotImplementedException();
        /// <summary>
        /// <para>Returns whether the body is actively used in the simulation.</para>
        /// </summary>
        /// <param name="status">True if the body is active or false if not.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>status</term><description>True if the body is active or false if not.</description></item>
        /// </list>
        /// </returns>
        public bool IsActive(bool status) => throw new NotImplementedException();
        /// <summary>
        /// <para>Returns the sleep status of the body.</para>
        /// </summary>
        /// <param name="status">True if the body is awake or false if not.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>status</term><description>True if the body is awake or false if not.</description></item>
        /// </list>
        /// </returns>
        public bool IsAwake(bool status) => throw new NotImplementedException();
        /// <summary>
        /// <para>Get the bullet status of a body.</para>
        /// <para>There are two methods to check for body collisions:</para>
        /// <para>The default method is efficient, but a body moving very quickly may sometimes jump over another body without producing a collision. A body that is set as a bullet will use CCD. This is less efficient, but is guaranteed not to jump when moving quickly.</para>
        /// <para>Note that static bodies (with zero mass) always use CCD, so your walls will not let a fast moving body pass through even if it is not a bullet.</para>
        /// </summary>
        /// <param name="status">The bullet status of the body.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>status</term><description>The bullet status of the body.</description></item>
        /// </list>
        /// </returns>
        public bool IsBullet(bool status) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets whether the Body is destroyed. Destroyed bodies cannot be used.</para>
        /// </summary>
        /// <param name="destroyed">Whether the Body is destroyed.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>destroyed</term><description>Whether the Body is destroyed.</description></item>
        /// </list>
        /// </returns>
        public bool IsDestroyed(bool destroyed) => throw new NotImplementedException();
        /// <summary>
        /// <para>Returns whether the body rotation is locked.</para>
        /// </summary>
        /// <param name="fixed">True if the body's rotation is locked or false if not.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>fixed</term><description>True if the body's rotation is locked or false if not.</description></item>
        /// </list>
        /// </returns>
        // TODO: public bool IsFixedRotation(bool fixed) => throw new NotImplementedException();
        /// <summary>
        /// <para>Returns the sleeping behaviour of the body.</para>
        /// </summary>
        /// <param name="allowed">True if the body is allowed to sleep or false if not.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>allowed</term><description>True if the body is allowed to sleep or false if not.</description></item>
        /// </list>
        /// </returns>
        public bool IsSleepingAllowed(bool allowed) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets whether the Body is touching the given other Body.</para>
        /// </summary>
        /// <param name="otherbody">The other body to check.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>touching</term><description>True if this body is touching the other body, false otherwise.</description></item>
        /// </list>
        /// </returns>
        public bool IsTouching(object otherbody) => throw new NotImplementedException();
        /// <summary>
        /// <para>Resets the mass of the body by recalculating it from the mass properties of the fixtures.</para>
        /// </summary>
        public void ResetMassData() => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets whether the body is active in the world.</para>
        /// <para>An inactive body does not take part in the simulation. It will not move or cause any collisions.</para>
        /// </summary>
        /// <param name="active">If the body is active or not.</param>
        public void SetActive(bool active) => throw new NotImplementedException();
        /// <summary>
        /// <para>Set the angle of the body.</para>
        /// <para>The angle is measured in radians. If you need to transform it from degrees, use math.rad.</para>
        /// <para>A value of 0 radians will mean "looking to the right". Although radians increase counter-clockwise, the y axis points down so it becomes clockwise from our point of view.</para>
        /// <para>It is possible to cause a collision with another body by changing its angle.</para>
        /// </summary>
        /// <param name="angle">The angle in radians.</param>
        public void SetAngle(double angle) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the angular damping of a Body</para>
        /// <para>See Body:getAngularDamping for a definition of angular damping.</para>
        /// <para>Angular damping can take any value from 0 to infinity. It is recommended to stay between 0 and 0.1, though. Other values will look unrealistic.</para>
        /// </summary>
        /// <param name="damping">The new angular damping.</param>
        public void SetAngularDamping(double damping) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the angular velocity of a Body.</para>
        /// <para>The angular velocity is the rate of change of angle over time.</para>
        /// <para>This function will not accumulate anything; any impulses previously applied since the last call to World:update will be lost.</para>
        /// </summary>
        /// <param name="w">The new angular velocity, in radians per second</param>
        public void SetAngularVelocity(double w) => throw new NotImplementedException();
        /// <summary>
        /// <para>Wakes the body up or puts it to sleep.</para>
        /// </summary>
        /// <param name="awake">The body sleep status.</param>
        public void SetAwake(bool awake) => throw new NotImplementedException();
        /// <summary>
        /// <para>Set the bullet status of a body.</para>
        /// <para>There are two methods to check for body collisions:</para>
        /// <para>The default method is efficient, but a body moving very quickly may sometimes jump over another body without producing a collision. A body that is set as a bullet will use CCD. This is less efficient, but is guaranteed not to jump when moving quickly.</para>
        /// <para>Note that static bodies (with zero mass) always use CCD, so your walls will not let a fast moving body pass through even if it is not a bullet.</para>
        /// </summary>
        /// <param name="status">The bullet status of the body.</param>
        public void SetBullet(bool status) => throw new NotImplementedException();
        /// <summary>
        /// <para>Set whether a body has fixed rotation.</para>
        /// <para>Bodies with fixed rotation don't vary the speed at which they rotate. Calling this function causes the mass to be reset.</para>
        /// </summary>
        /// <param name="isFixed">Whether the body should have fixed rotation.</param>
        public void SetFixedRotation(bool isFixed) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets a new gravity scale factor for the body.</para>
        /// </summary>
        /// <param name="scale">The new gravity scale factor.</param>
        public void SetGravityScale(double scale) => throw new NotImplementedException();
        /// <summary>
        /// <para>Set the inertia of a body.</para>
        /// </summary>
        /// <param name="inertia">The new moment of inertia, in kilograms * pixel squared.</param>
        public void SetInertia(double inertia) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the linear damping of a Body. The linear damping is the rate of decrease of the linear velocity over time. A moving body with no damping and no external forces will continue moving indefinitely, as is the case in space. A moving body with damping will gradually stop moving.</para>
        /// <para>Linear damping can take any value from 0 to infinity. It is recommended to stay between 0 and 1, though. Other values will make the objects look "floaty"(if gravity is enabled).</para>
        /// </summary>
        /// <param name="ld">The new linear damping</param>
        public void SetLinearDamping(double ld) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets a new linear velocity for the Body.</para>
        /// <para>This function will not accumulate anything; any impulses previously applied since the last call to World:update will be lost.</para>
        /// </summary>
        /// <param name="x">The x-component of the velocity vector.</param>
        /// <param name="y">The y-component of the velocity vector.</param>
        public void SetLinearVelocity(double x, double y) => throw new NotImplementedException();
        /// <summary>
        /// <para>Overrides the calculated mass data.</para>
        /// </summary>
        /// <param name="x">The x position of the center of mass.</param>
        /// <param name="y">The y position of the center of mass.</param>
        /// <param name="mass">The mass of the body.</param>
        /// <param name="inertia">The rotational inertia.</param>
        public void SetMassData(double x, double y, double mass, double inertia) => throw new NotImplementedException();
        /// <summary>
        /// <para>Set the position of the body.</para>
        /// <para>Note that this may not be the center of mass of the body.</para>
        /// <para>This function cannot wake up the body.</para>
        /// </summary>
        /// <param name="x">The x position.</param>
        /// <param name="y">The y position.</param>
        public void SetPosition(double x, double y) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the sleeping behaviour of the body. Should sleeping be allowed, a body at rest will automatically sleep. A sleeping body is not simulated unless it collided with an awake body. Be wary that one can end up with a situation like a floating sleeping body if the floor was removed.</para>
        /// </summary>
        /// <param name="allowed">True if the body is allowed to sleep or false if not.</param>
        public void SetSleepingAllowed(bool allowed) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets a new body type.</para>
        /// </summary>
        /// <param name="type">The new type.</param>
        public void SetType(BodyType type) => throw new NotImplementedException();
        /// <summary>
        /// <para>Associates a Lua value with the Body.</para>
        /// <para>To delete the reference, explicitly pass nil.</para>
        /// </summary>
        /// <param name="value">The Lua value to associate with the Body.</param>
        public void SetUserData(object value) => throw new NotImplementedException();
        /// <summary>
        /// <para>Set the x position of the body.</para>
        /// <para>This function cannot wake up the body.</para>
        /// </summary>
        /// <param name="x">The x position.</param>
        public void SetX(double x) => throw new NotImplementedException();
        /// <summary>
        /// <para>Set the y position of the body.</para>
        /// <para>This function cannot wake up the body.</para>
        /// </summary>
        /// <param name="y">The y position.</param>
        public void SetY(double y) => throw new NotImplementedException();
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
