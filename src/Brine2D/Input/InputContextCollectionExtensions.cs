using Brine2D.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Brine2D.Input;

public static class InputContextCollectionExtensions
{
    /// <summary>
    /// Enables the input layer system, which routes input to registered <see cref="IInputLayer"/>
    /// implementations in priority order. Layers can consume input to prevent lower-priority
    /// layers from receiving it.
    /// </summary>
    /// <remarks>
    /// After calling this, register layers from your scene's <c>OnEnter()</c> via
    /// <c>Game.Services.GetRequiredService&lt;InputLayerManager&gt;().RegisterLayer(this)</c>.
    /// </remarks>
    public static Brine2DBuilder UseInputLayers(this Brine2DBuilder builder)
    {
        builder.Services.AddSingleton<InputLayerManager>();
        return builder;
    }
}