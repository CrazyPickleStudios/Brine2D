namespace Brine2D.ECS;

/// <summary>
/// Abstracts DI-backed instance creation so <see cref="EntityWorld"/> does not need
/// to hold a direct reference to <see cref="IServiceProvider"/>.
/// </summary>
internal interface IActivator
{
    T CreateInstance<T>() where T : class;
}