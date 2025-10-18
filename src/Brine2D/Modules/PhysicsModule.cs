using System;

namespace Brine2D;

// TODO: Needs review
public sealed class PhysicsModule
{
/// <summary>
        /// <para>Returns the two closest points between two fixtures and their distance.</para>
        /// </summary>
        /// <param name="fixture1">The first fixture.</param>
        /// <param name="fixture2">The second fixture.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>distance</term><description>The distance of the two points.</description></item>
        /// <item><term>x1</term><description>The x-coordinate of the first point.</description></item>
        /// <item><term>y1</term><description>The y-coordinate of the first point.</description></item>
        /// <item><term>x2</term><description>The x-coordinate of the second point.</description></item>
        /// <item><term>y2</term><description>The y-coordinate of the second point.</description></item>
        /// </list>
        /// </returns>
    public (double distance, double x1, double y1, double x2, double y2) GetDistance(object fixture1, object fixture2) => throw new NotImplementedException();

/// <summary>
        /// <para>Returns the meter scale factor.</para>
        /// <para>All coordinates in the physics module are divided by this number, creating a convenient way to draw the objects directly to the screen without the need for graphics transformations.</para>
        /// <para>It is recommended to create shapes no larger than 10 times the scale. This is important because Box2D is tuned to work well with shape sizes from 0.1 to 10 meters.</para>
        /// </summary>
        /// <param name="scale">The scale factor as an integer.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>scale</term><description>The scale factor as an integer.</description></item>
        /// </list>
        /// </returns>
    public double GetMeter(double scale) => throw new NotImplementedException();

/// <summary>
        /// <para>Creates a new ChainShape.</para>
        /// </summary>
        /// <param name="loop">If the chain should loop back to the first point.</param>
        /// <param name="x1">The x position of the first point.</param>
        /// <param name="y1">The y position of the first point.</param>
        /// <param name="x2">The x position of the second point.</param>
        /// <param name="y2">The y position of the second point.</param>
        /// <param name="">Additional point positions.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>shape</term><description>The new shape.</description></item>
        /// </list>
        /// </returns>
    // TODO: public object NewChainShape(bool loop, double x1, double y1, double x2, double y2, object) => throw new NotImplementedException();

/// <summary>
        /// <para>Creates a new ChainShape.</para>
        /// </summary>
        /// <param name="loop">If the chain should loop back to the first point.</param>
        /// <param name="points">A list of points to construct the ChainShape, in the form of .</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>shape</term><description>The new shape.</description></item>
        /// </list>
        /// </returns>
    public object NewChainShape(bool loop, object points) => throw new NotImplementedException();

/// <summary>
        /// <para>Creates a new EdgeShape.</para>
        /// </summary>
        /// <param name="x1">The x position of the first point.</param>
        /// <param name="y1">The y position of the first point.</param>
        /// <param name="x2">The x position of the second point.</param>
        /// <param name="y2">The y position of the second point.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>shape</term><description>The new shape.</description></item>
        /// </list>
        /// </returns>
    public object NewEdgeShape(double x1, double y1, double x2, double y2) => throw new NotImplementedException();

/// <summary>
        /// <para>Creates and attaches a Fixture to a body.</para>
        /// <para>Note that the Shape object is copied rather than kept as a reference when the Fixture is created. To get the Shape object that the Fixture owns, use Fixture:getShape.</para>
        /// </summary>
        /// <param name="body">The body which gets the fixture attached.</param>
        /// <param name="shape">The shape to be copied to the fixture.</param>
        /// <param name="density">The density of the fixture.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>fixture</term><description>The new fixture.</description></item>
        /// </list>
        /// </returns>
    public object NewFixture(object body, object shape, double density = 1) => throw new NotImplementedException();

/// <summary>
        /// <para>Create a friction joint between two bodies. A FrictionJoint applies friction to a body.</para>
        /// </summary>
        /// <param name="body1">The first body to attach to the joint.</param>
        /// <param name="body2">The second body to attach to the joint.</param>
        /// <param name="x">The x position of the anchor point.</param>
        /// <param name="y">The y position of the anchor point.</param>
        /// <param name="collideConnected">Specifies whether the two bodies should collide with each other.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>joint</term><description>The new FrictionJoint.</description></item>
        /// </list>
        /// </returns>
    public object NewFrictionJoint(object body1, object body2, double x, double y, bool collideConnected = false) => throw new NotImplementedException();

/// <summary>
        /// <para>Create a friction joint between two bodies. A FrictionJoint applies friction to a body.</para>
        /// </summary>
        /// <param name="body1">The first body to attach to the joint.</param>
        /// <param name="body2">The second body to attach to the joint.</param>
        /// <param name="x1">The x position of the first anchor point.</param>
        /// <param name="y1">The y position of the first anchor point.</param>
        /// <param name="x2">The x position of the second anchor point.</param>
        /// <param name="y2">The y position of the second anchor point.</param>
        /// <param name="collideConnected">Specifies whether the two bodies should collide with each other.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>joint</term><description>The new FrictionJoint.</description></item>
        /// </list>
        /// </returns>
    public object NewFrictionJoint(object body1, object body2, double x1, double y1, double x2, double y2, bool collideConnected = false) => throw new NotImplementedException();

/// <summary>
        /// <para>Creates a joint between two bodies which controls the relative motion between them.</para>
        /// <para>Position and rotation offsets can be specified once the MotorJoint has been created, as well as the maximum motor force and torque that will be be applied to reach the target offsets.</para>
        /// </summary>
        /// <param name="body1">The first body to attach to the joint.</param>
        /// <param name="body2">The second body to attach to the joint.</param>
        /// <param name="correctionFactor">The joint's initial position correction factor, in the range of [0, 1].</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>joint</term><description>The new MotorJoint.</description></item>
        /// </list>
        /// </returns>
    public object NewMotorJoint(object body1, object body2, double correctionFactor = 0.3) => throw new NotImplementedException();

/// <summary>
        /// <para>Creates a joint between two bodies which controls the relative motion between them.</para>
        /// <para>Position and rotation offsets can be specified once the MotorJoint has been created, as well as the maximum motor force and torque that will be be applied to reach the target offsets.</para>
        /// </summary>
        /// <param name="body1">The first body to attach to the joint.</param>
        /// <param name="body2">The second body to attach to the joint.</param>
        /// <param name="correctionFactor">The joint's initial position correction factor, in the range of [0, 1].</param>
        /// <param name="collideConnected">Specifies whether the two bodies should collide with each other.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>joint</term><description>The new MotorJoint.</description></item>
        /// </list>
        /// </returns>
    public object NewMotorJoint(object body1, object body2, double correctionFactor = 0.3, bool collideConnected = false) => throw new NotImplementedException();

/// <summary>
        /// <para>Create a joint between a body and the mouse.</para>
        /// <para>This joint actually connects the body to a fixed point in the world. To make it follow the mouse, the fixed point must be updated every timestep (example below).</para>
        /// <para>The advantage of using a MouseJoint instead of just changing a body position directly is that collisions and reactions to other joints are handled by the physics engine.</para>
        /// </summary>
        /// <param name="body">The body to attach to the mouse.</param>
        /// <param name="x">The x position of the connecting point.</param>
        /// <param name="y">The y position of the connecting point.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>joint</term><description>The new mouse joint.</description></item>
        /// </list>
        /// </returns>
    public object NewMouseJoint(object body, double x, double y) => throw new NotImplementedException();

/// <summary>
        /// <para>Create a joint between a body and the mouse.</para>
        /// <para>This joint actually connects the body to a fixed point in the world. To make it follow the mouse, the fixed point must be updated every timestep (example below).</para>
        /// <para>The advantage of using a MouseJoint instead of just changing a body position directly is that collisions and reactions to other joints are handled by the physics engine.</para>
        /// </summary>
    public void NewWorld() => throw new NotImplementedException();

/// <summary>
        /// <para>Creates a joint between two bodies. Its only function is enforcing a max distance between these bodies.</para>
        /// </summary>
        /// <param name="body1">The first body to attach to the joint.</param>
        /// <param name="body2">The second body to attach to the joint.</param>
        /// <param name="x1">The x position of the first anchor point.</param>
        /// <param name="y1">The y position of the first anchor point.</param>
        /// <param name="x2">The x position of the second anchor point.</param>
        /// <param name="y2">The y position of the second anchor point.</param>
        /// <param name="maxLength">The maximum distance for the bodies.</param>
        /// <param name="collideConnected">Specifies whether the two bodies should collide with each other.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>joint</term><description>The new RopeJoint.</description></item>
        /// </list>
        /// </returns>
    public object NewRopeJoint(object body1, object body2, double x1, double y1, double x2, double y2, double maxLength, bool collideConnected = false) => throw new NotImplementedException();

/// <summary>
        /// <para>Creates a constraint joint between two bodies. A WeldJoint essentially glues two bodies together. The constraint is a bit soft, however, due to Box2D's iterative solver.</para>
        /// </summary>
        /// <param name="body1">The first body to attach to the joint.</param>
        /// <param name="body2">The second body to attach to the joint.</param>
        /// <param name="x">The x position of the anchor point (world space).</param>
        /// <param name="y">The y position of the anchor point (world space).</param>
        /// <param name="collideConnected">Specifies whether the two bodies should collide with each other.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>joint</term><description>The new WeldJoint.</description></item>
        /// </list>
        /// </returns>
    public object NewWeldJoint(object body1, object body2, double x, double y, bool collideConnected = false) => throw new NotImplementedException();

/// <summary>
        /// <para>Creates a constraint joint between two bodies. A WeldJoint essentially glues two bodies together. The constraint is a bit soft, however, due to Box2D's iterative solver.</para>
        /// </summary>
        /// <param name="body1">The first body to attach to the joint.</param>
        /// <param name="body2">The second body to attach to the joint.</param>
        /// <param name="x1">The x position of the first anchor point (world space).</param>
        /// <param name="y1">The y position of the first anchor point (world space).</param>
        /// <param name="x2">The x position of the second anchor point (world space).</param>
        /// <param name="y2">The y position of the second anchor point (world space).</param>
        /// <param name="collideConnected">Specifies whether the two bodies should collide with each other.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>joint</term><description>The new WeldJoint.</description></item>
        /// </list>
        /// </returns>
    public object NewWeldJoint(object body1, object body2, double x1, double y1, double x2, double y2, bool collideConnected = false) => throw new NotImplementedException();

/// <summary>
        /// <para>Creates a constraint joint between two bodies. A WeldJoint essentially glues two bodies together. The constraint is a bit soft, however, due to Box2D's iterative solver.</para>
        /// </summary>
        /// <param name="body1">The first body to attach to the joint.</param>
        /// <param name="body2">The second body to attach to the joint.</param>
        /// <param name="x1">The x position of the first anchor point (world space).</param>
        /// <param name="y1">The y position of the first anchor point  (world space).</param>
        /// <param name="x2">The x position of the second anchor point (world space).</param>
        /// <param name="y2">The y position of the second anchor point (world space).</param>
        /// <param name="collideConnected">Specifies whether the two bodies should collide with each other.</param>
        /// <param name="referenceAngle">The reference angle between body1 and body2, in radians.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>joint</term><description>The new WeldJoint.</description></item>
        /// </list>
        /// </returns>
    public object NewWeldJoint(object body1, object body2, double x1, double y1, double x2, double y2, bool collideConnected = false, double referenceAngle = 0) => throw new NotImplementedException();

/// <summary>
        /// <para>Creates a wheel joint.</para>
        /// </summary>
        /// <param name="body1">The first body.</param>
        /// <param name="body2">The second body.</param>
        /// <param name="x">The x position of the anchor point.</param>
        /// <param name="y">The y position of the anchor point.</param>
        /// <param name="ax">The x position of the axis unit vector.</param>
        /// <param name="ay">The y position of the axis unit vector.</param>
        /// <param name="collideConnected">Specifies whether the two bodies should collide with each other.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>joint</term><description>The new WheelJoint.</description></item>
        /// </list>
        /// </returns>
    public object NewWheelJoint(object body1, object body2, double x, double y, double ax, double ay, bool collideConnected = false) => throw new NotImplementedException();

/// <summary>
        /// <para>Creates a wheel joint.</para>
        /// </summary>
        /// <param name="body1">The first body.</param>
        /// <param name="body2">The second body.</param>
        /// <param name="x1">The x position of the first anchor point.</param>
        /// <param name="y1">The y position of the first anchor point.</param>
        /// <param name="x2">The x position of the second anchor point.</param>
        /// <param name="y2">The y position of the second anchor point.</param>
        /// <param name="ax">The x position of the axis unit vector.</param>
        /// <param name="ay">The y position of the axis unit vector.</param>
        /// <param name="collideConnected">Specifies whether the two bodies should collide with each other.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>joint</term><description>The new WheelJoint.</description></item>
        /// </list>
        /// </returns>
    public object NewWheelJoint(object body1, object body2, double x1, double y1, double x2, double y2, double ax, double ay, bool collideConnected = false) => throw new NotImplementedException();

/// <summary>
        /// <para>Sets the pixels to meter scale factor.</para>
        /// <para>All coordinates in the physics module are divided by this number and converted to meters, and it creates a convenient way to draw the objects directly to the screen without the need for graphics transformations.</para>
        /// <para>It is recommended to create shapes no larger than 10 times the scale. This is important because Box2D is tuned to work well with shape sizes from 0.1 to 10 meters. The default meter scale is 30.</para>
        /// </summary>
        /// <param name="scale">The scale factor as an integer.</param>
    public void SetMeter(double scale) => throw new NotImplementedException();

/// <summary>
        /// <para>Sets the pixels to meter scale factor.</para>
        /// <para>All coordinates in the physics module are divided by this number and converted to meters, and it creates a convenient way to draw the objects directly to the screen without the need for graphics transformations.</para>
        /// <para>It is recommended to create shapes no larger than 10 times the scale. This is important because Box2D is tuned to work well with shape sizes from 0.1 to 10 meters. The default meter scale is 30.</para>
        /// </summary>
    public void SetMeter() => throw new NotImplementedException();

}
