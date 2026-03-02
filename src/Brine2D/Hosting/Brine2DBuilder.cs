using Microsoft.Extensions.DependencyInjection;

namespace Brine2D.Hosting;

/// <summary>
/// Builder returned by <see cref="Brine2DServiceCollectionExtensions.AddBrine2D"/> for fluent
/// chaining of optional subsystems via <c>Use*()</c> extension methods:
/// <code>
/// services.AddBrine2D()
///         .UseNetworking()
///         .UseDebugOverlay();
/// </code>
/// In standalone DI usage, register <see cref="Brine2DOptions"/> before calling
/// <see cref="Brine2DServiceCollectionExtensions.AddBrine2D"/>:
/// <code>
/// services.AddSingleton(new Brine2DOptions { Headless = true });
/// services.AddBrine2D();
/// </code>
/// </summary>
public sealed class Brine2DBuilder
{
    /// <summary>Gets the service collection being configured.</summary>
    public IServiceCollection Services { get; }

    internal Brine2DBuilder(IServiceCollection services)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
    }
}