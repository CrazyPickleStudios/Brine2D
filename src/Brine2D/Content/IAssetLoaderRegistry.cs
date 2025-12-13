using Microsoft.Extensions.DependencyInjection;

namespace Brine2D.Content;

public interface IAssetLoaderRegistry
{
    IAssetLoader<T> GetLoader<T>() where T : class, IDisposable;
}

public sealed class AssetLoaderRegistry : IAssetLoaderRegistry
{
    private readonly IServiceProvider _services;

    public AssetLoaderRegistry(IServiceProvider services) => _services = services;

    public IAssetLoader<T> GetLoader<T>() where T : class, IDisposable =>
        _services.GetRequiredService<IAssetLoader<T>>();
}