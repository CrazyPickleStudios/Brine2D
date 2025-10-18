namespace Brine2D
{
    /// <summary>
    /// Different types of joints.
    /// </summary>
    // TODO: Requires Review
    public enum JointType
    {
        /// <summary>
        /// A DistanceJoint. Keeps two bodies at the same distance.
        /// </summary>
        Distance,
        /// <summary>
        /// A FrictionJoint. A FrictionJoint applies friction to a body.
        /// </summary>
        Friction,
        /// <summary>
        /// A GearJoint. Keeps bodies together in such a way that they act like gears.
        /// </summary>
        Gear,
        /// <summary>
        /// A MouseJoint. For controlling objects with the mouse.
        /// </summary>
        Mouse,
        /// <summary>
        /// A PrismaticJoint. Restricts relative motion between Bodies to one shared axis.
        /// </summary>
        Prismatic,
        /// <summary>
        /// A PulleyJoint. Allows you to simulate bodies connected through pulleys.
        /// </summary>
        Pulley,
        /// <summary>
        /// A RevoluteJoint. Allow two Bodies to revolve around a shared point.
        /// </summary>
        Revolute,
        /// <summary>
        /// A RopeJoint. The RopeJoint enforces a maximum distance between two points on two bodies.
        /// </summary>
        Rope,
        /// <summary>
        /// A WeldJoint. A WeldJoint essentially glues two bodies together.
        /// </summary>
        Weld,
    }
}
