using Microsoft.Extensions.DependencyInjection;

namespace Brine2D.Hosting;

/// <summary>
/// Builder for configuring Brine2D with platform backends.
/// </summary>
/// <remarks>
/// This builder is returned by <see cref="Brine2DServiceCollectionExtensions.AddBrine2D"/>
/// and allows chaining backend configuration methods like <c>.UseSDL()</c>.
/// </remarks>
public class Brine2DBuilder
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