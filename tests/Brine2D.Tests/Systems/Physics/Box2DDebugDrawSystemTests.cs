//using Brine2D.ECS.Systems;
//using Brine2D.Systems.Physics;

//namespace Brine2D.Tests.Systems.Physics;

//[Collection("Physics")]
//public class Box2DDebugDrawSystemTests : PhysicsTestBase
//{
//    [Fact]
//    public void RenderOrder_IsDebugPlusOne()
//    {
//        var system = new Box2DDebugDrawSystem(PhysicsWorld);
//        Assert.Equal(SystemRenderOrder.Debug + 1, system.RenderOrder);
//    }

//    [Fact]
//    public void DefaultProperties_AreCorrect()
//    {
//        var system = new Box2DDebugDrawSystem(PhysicsWorld);

//        Assert.True(system.DrawShapes);
//        Assert.True(system.DrawJoints);
//        Assert.False(system.DrawBounds);
//        Assert.False(system.DrawMass);
//        Assert.False(system.DrawContacts);
//        Assert.False(system.DrawContactNormals);
//    }

//    [Fact]
//    public void Properties_CanBeToggled()
//    {
//        var system = new Box2DDebugDrawSystem(PhysicsWorld);

//        system.DrawShapes = false;
//        system.DrawBounds = true;
//        system.DrawMass = true;
//        system.DrawContacts = true;
//        system.DrawContactNormals = true;

//        Assert.False(system.DrawShapes);
//        Assert.True(system.DrawBounds);
//        Assert.True(system.DrawMass);
//        Assert.True(system.DrawContacts);
//        Assert.True(system.DrawContactNormals);
//    }

//    [Fact]
//    public void IsEnabled_DefaultTrue()
//    {
//        var system = new Box2DDebugDrawSystem(PhysicsWorld);
//        Assert.True(system.IsEnabled);
//    }

//    [Fact]
//    public void IsEnabled_CanBeDisabled()
//    {
//        var system = new Box2DDebugDrawSystem(PhysicsWorld);
//        system.IsEnabled = false;
//        Assert.False(system.IsEnabled);
//    }
//}