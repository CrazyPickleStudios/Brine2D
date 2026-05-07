using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Brine2D.Tests.Systems.Physics;

[CollectionDefinition("Physics", DisableParallelization = true)]
public class PhysicsCollectionDefinition;