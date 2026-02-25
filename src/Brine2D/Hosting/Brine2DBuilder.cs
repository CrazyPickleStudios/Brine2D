using Microsoft.Extensions.DependencyInjection;

namespace Brine2D.Hosting;

/// <summary>
/// Builder for configuring Brine2D with optional platform extensions.
/// Returned by <see cref="Brine2DServiceCollectionExtensions.AddBrine2D"/> to allow
/// fluent chaining of optional subsystems, e.g.:
/// <code>
/// services.AddBrine2D()
///         .UseNetworking()
///         .UseDebugOverlay();
/// </code>
/// Extension methods on this type are the approved extension point for optional
/// subsystems. Prefer a named <c>Use*()</c> method over adding registrations
/// directly to <see cref="Services"/> at call sites.
/// </summary>
public sealed class Brine2DBuilder
{
    /// <summary>
    /// Gets the service collection being configured.
    /// </summary>
    public IServiceCollection Services { get; }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="Brine2DBuilder"/> class.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    public Brine2DBuilder(IServiceCollection services)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
    }
}