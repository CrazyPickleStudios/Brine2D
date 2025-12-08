using Brine2D.Engine;
using Brine2D.Input;
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

        services.AddSingleton<SdlKeyboard>();
        services.AddSingleton<IKeyboard>(sp => sp.GetRequiredService<SdlKeyboard>());

        services.AddSingleton<SdlMouse>();
        services.AddSingleton<IMouse>(sp => sp.GetRequiredService<SdlMouse>());

        services.AddSingleton<SdlGamepad>();
        services.AddSingleton<IGamepads>(sp => sp.GetRequiredService<SdlGamepads>());

        services.AddSingleton<SdlTouch>();
        services.AddSingleton<ITouch>(sp => sp.GetRequiredService<SdlTouch>());

        services.AddSingleton<SdlTextInput>();
        services.AddSingleton<ITextInput>(sp => sp.GetRequiredService<SdlTextInput>());

        services.AddSingleton<SdlInput>();
        services.AddSingleton<IInput>(sp => sp.GetRequiredService<SdlInput>());

        services.AddSingleton<IGameLoop, SdlGameLoop>();

        return services;
    }
}