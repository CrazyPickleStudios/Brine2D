using Brine2D.ECS;
using Brine2D.Systems.Audio;

namespace Brine2D.Tests.Systems.Audio;

public class AudioListenerComponentTests : TestBase
{
    private AudioListenerComponent CreateListener()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity()
            .AddComponent<AudioListenerComponent>();
        return entity.GetComponent<AudioListenerComponent>()!;
    }

    [Fact]
    public void SpeedOfSound_DefaultIs343()
    {
        var listener = CreateListener();
        Assert.Equal(343f, listener.SpeedOfSound);
    }

    [Fact]
    public void SpeedOfSound_CanBeSet()
    {
        var listener = CreateListener();
        listener.SpeedOfSound = 500f;
        Assert.Equal(500f, listener.SpeedOfSound);
    }

    [Theory]
    [InlineData(0f, 1f)]
    [InlineData(-100f, 1f)]
    [InlineData(1f, 1f)]
    [InlineData(700f, 700f)]
    public void SpeedOfSound_BelowMinimum_IsClampedToOne(float input, float expected)
    {
        var listener = CreateListener();
        listener.SpeedOfSound = input;
        Assert.Equal(expected, listener.SpeedOfSound);
    }

    [Fact]
    public void GlobalSpatialVolume_DefaultIsOne()
    {
        var listener = CreateListener();
        Assert.Equal(1.0f, listener.GlobalSpatialVolume);
    }

    [Theory]
    [InlineData(-0.5f, 0f)]
    [InlineData(1.5f, 1f)]
    [InlineData(0.5f, 0.5f)]
    public void GlobalSpatialVolume_OutOfRange_IsClamped(float input, float expected)
    {
        var listener = CreateListener();
        listener.GlobalSpatialVolume = input;
        Assert.Equal(expected, listener.GlobalSpatialVolume);
    }
}