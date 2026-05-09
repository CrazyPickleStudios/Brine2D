using Brine2D.Physics;

namespace Brine2D.Tests.Physics;

public class PhysicsLayerRegistryTests
{
    [Fact]
    public void Register_GetLayer_ReturnsIndex()
    {
        var registry = new PhysicsLayerRegistry();
        registry.Register("Ground", 0);

        Assert.Equal(0, registry.GetLayer("Ground"));
    }

    [Fact]
    public void Register_MultipleDistinctLayers_AllRetrievable()
    {
        var registry = new PhysicsLayerRegistry();
        registry.Register("Ground", 0);
        registry.Register("Player", 1);
        registry.Register("Enemy", 2);

        Assert.Equal(0, registry.GetLayer("Ground"));
        Assert.Equal(1, registry.GetLayer("Player"));
        Assert.Equal(2, registry.GetLayer("Enemy"));
    }

    [Fact]
    public void GetMask_SingleLayer_ReturnsCorrectBit()
    {
        var registry = new PhysicsLayerRegistry();
        registry.Register("Ground", 5);

        Assert.Equal(1UL << 5, registry.GetMask("Ground"));
    }

    [Fact]
    public void GetMask_MaxIndex_ReturnsCorrectBit()
    {
        var registry = new PhysicsLayerRegistry();
        registry.Register("Top", 63);

        Assert.Equal(1UL << 63, registry.GetMask("Top"));
    }

    [Fact]
    public void GetMask_MultipleNames_ReturnsOrOfBits()
    {
        var registry = new PhysicsLayerRegistry();
        registry.Register("Ground", 0);
        registry.Register("Water", 3);
        registry.Register("Wall", 7);

        ulong expected = (1UL << 0) | (1UL << 3) | (1UL << 7);
        Assert.Equal(expected, registry.GetMask("Ground", "Water", "Wall"));
    }

    [Fact]
    public void TryGetLayer_ExistingName_ReturnsTrueAndIndex()
    {
        var registry = new PhysicsLayerRegistry();
        registry.Register("Player", 4);

        Assert.True(registry.TryGetLayer("Player", out int index));
        Assert.Equal(4, index);
    }

    [Fact]
    public void TryGetLayer_UnknownName_ReturnsFalse()
    {
        var registry = new PhysicsLayerRegistry();

        Assert.False(registry.TryGetLayer("Missing", out _));
    }

    [Fact]
    public void GetLayer_UnknownName_ThrowsKeyNotFound()
    {
        var registry = new PhysicsLayerRegistry();

        Assert.Throws<KeyNotFoundException>(() => registry.GetLayer("Missing"));
    }

    [Fact]
    public void Register_DuplicateName_ThrowsArgumentException()
    {
        var registry = new PhysicsLayerRegistry();
        registry.Register("Ground", 0);

        Assert.Throws<ArgumentException>(() => registry.Register("Ground", 1));
    }

    [Fact]
    public void Register_DuplicateIndex_ThrowsArgumentException()
    {
        var registry = new PhysicsLayerRegistry();
        registry.Register("Ground", 0);

        Assert.Throws<ArgumentException>(() => registry.Register("Wall", 0));
    }

    [Fact]
    public void Register_IndexBelowZero_ThrowsArgumentOutOfRange()
    {
        var registry = new PhysicsLayerRegistry();

        Assert.Throws<ArgumentOutOfRangeException>(() => registry.Register("X", -1));
    }

    [Fact]
    public void Register_IndexAbove63_ThrowsArgumentOutOfRange()
    {
        var registry = new PhysicsLayerRegistry();

        Assert.Throws<ArgumentOutOfRangeException>(() => registry.Register("X", 64));
    }

    [Fact]
    public void Register_NullName_Throws()
    {
        var registry = new PhysicsLayerRegistry();

        Assert.ThrowsAny<ArgumentException>(() => registry.Register(null!, 0));
    }

    [Fact]
    public void Register_WhitespaceName_Throws()
    {
        var registry = new PhysicsLayerRegistry();

        Assert.ThrowsAny<ArgumentException>(() => registry.Register("  ", 0));
    }

    [Fact]
    public void Freeze_SetsIsFrozen()
    {
        var registry = new PhysicsLayerRegistry();
        registry.Register("Ground", 0);

        Assert.False(registry.IsFrozen);
        registry.Freeze();
        Assert.True(registry.IsFrozen);
    }

    [Fact]
    public void Freeze_CalledTwice_DoesNotThrow()
    {
        var registry = new PhysicsLayerRegistry();
        registry.Freeze();
        registry.Freeze();
    }

    [Fact]
    public void Register_AfterFreeze_ThrowsInvalidOperation()
    {
        var registry = new PhysicsLayerRegistry();
        registry.Freeze();

        Assert.Throws<InvalidOperationException>(() => registry.Register("New", 0));
    }

    [Fact]
    public void GetLayer_AfterFreeze_StillReturnsIndex()
    {
        var registry = new PhysicsLayerRegistry();
        registry.Register("Ground", 0);
        registry.Freeze();

        Assert.Equal(0, registry.GetLayer("Ground"));
    }

    [Fact]
    public void TryGetLayer_AfterFreeze_StillWorks()
    {
        var registry = new PhysicsLayerRegistry();
        registry.Register("Player", 2);
        registry.Freeze();

        Assert.True(registry.TryGetLayer("Player", out int index));
        Assert.Equal(2, index);
        Assert.False(registry.TryGetLayer("Missing", out _));
    }

    [Fact]
    public void ForLayer_ReturnsFilterWithCorrectCollisionMask()
    {
        var registry = new PhysicsLayerRegistry();
        registry.Register("Ground", 3);

        var filter = registry.ForLayer("Ground");

        Assert.Equal(1UL << 3, filter.CollisionMask);
        Assert.False(filter.ExcludeSensors);
    }

    [Fact]
    public void SolidLayer_ReturnsFilterWithExcludeSensors()
    {
        var registry = new PhysicsLayerRegistry();
        registry.Register("Wall", 6);

        var filter = registry.SolidLayer("Wall");

        Assert.Equal(1UL << 6, filter.CollisionMask);
        Assert.True(filter.ExcludeSensors);
    }

    [Fact]
    public void ForLayers_ReturnsCombinedMask()
    {
        var registry = new PhysicsLayerRegistry();
        registry.Register("Ground", 0);
        registry.Register("Wall", 2);

        var filter = registry.ForLayers("Ground", "Wall");

        Assert.Equal((1UL << 0) | (1UL << 2), filter.CollisionMask);
        Assert.False(filter.ExcludeSensors);
    }

    [Fact]
    public void SolidLayers_ReturnsCombinedMaskAndExcludeSensors()
    {
        var registry = new PhysicsLayerRegistry();
        registry.Register("Ground", 0);
        registry.Register("Wall", 2);

        var filter = registry.SolidLayers("Ground", "Wall");

        Assert.Equal((1UL << 0) | (1UL << 2), filter.CollisionMask);
        Assert.True(filter.ExcludeSensors);
    }

    [Fact]
    public void ForLayer_UnknownName_ThrowsKeyNotFound()
    {
        var registry = new PhysicsLayerRegistry();

        Assert.Throws<KeyNotFoundException>(() => registry.ForLayer("Unknown"));
    }

    [Fact]
    public void SolidLayer_UnknownName_ThrowsKeyNotFound()
    {
        var registry = new PhysicsLayerRegistry();

        Assert.Throws<KeyNotFoundException>(() => registry.SolidLayer("Unknown"));
    }

    [Fact]
    public void GetAllLayers_ReturnsAllRegisteredPairs()
    {
        var registry = new PhysicsLayerRegistry();
        registry.Register("Ground", 0);
        registry.Register("Player", 1);
        registry.Register("Enemy", 5);

        var all = registry.GetAllLayers().ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        Assert.Equal(3, all.Count);
        Assert.Equal(0, all["Ground"]);
        Assert.Equal(1, all["Player"]);
        Assert.Equal(5, all["Enemy"]);
    }

    [Fact]
    public void GetAllLayers_AfterFreeze_StillReturnsAllPairs()
    {
        var registry = new PhysicsLayerRegistry();
        registry.Register("Ground", 0);
        registry.Register("Player", 1);
        registry.Freeze();

        var all = registry.GetAllLayers().ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        Assert.Equal(2, all.Count);
    }

    [Fact]
    public void GetMask_MultipleNames_UnknownEntry_Throws()
    {
        var registry = new PhysicsLayerRegistry();
        registry.Register("Ground", 0);

        Assert.Throws<KeyNotFoundException>(() => registry.GetMask("Ground", "Missing"));
    }

    [Fact]
    public void LayerIndex_MapsToCorrectCategoryBit()
    {
        var registry = new PhysicsLayerRegistry();
        for (int i = 0; i <= 63; i++)
            registry.Register($"Layer{i}", i);

        for (int i = 0; i <= 63; i++)
            Assert.Equal(1UL << i, registry.GetMask($"Layer{i}"));
    }
}