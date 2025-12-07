using Brine2D.Abstractions;
using Brine2D.Engine;
using Microsoft.Extensions.DependencyInjection;

namespace Brine2D.SDL3;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBrine2DSdl3(this IServiceCollection services)
    {
        services.AddSingleton<SdlInitializer>();

        services.AddSingleton<SdlWindow>();
        services.AddSingleton<IWindow>(sp => sp.GetRequiredService<SdlWindow>());

        services.AddSingleton<IRenderContext, SdlRenderer>();

        services.AddSingleton<SdlInput>();
        services.AddSingleton<IInput>(sp => sp.GetRequiredService<SdlInput>());

        services.AddSingleton<IGameLoop, SdlGameLoop>();

        return services;
    }
}