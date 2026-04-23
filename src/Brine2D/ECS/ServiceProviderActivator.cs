using Microsoft.Extensions.DependencyInjection;

namespace Brine2D.ECS;

/// <summary>
///     Default <see cref="IActivator" /> backed by <see cref="ActivatorUtilities" />.
///     Registered as scoped so each scene scope resolves from the correct DI scope.
/// </summary>
internal sealed class ServiceProviderActivator(IServiceProvider serviceProvider) : IActivator
{
    public T CreateInstance<T>() where T : class
    {
        return ActivatorUtilities.CreateInstance<T>(serviceProvider);
    }
}