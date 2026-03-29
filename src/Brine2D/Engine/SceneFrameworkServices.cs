using Brine2D.Audio;
using Brine2D.Input;
using Brine2D.Rendering;
using Microsoft.Extensions.Logging;

namespace Brine2D.Engine;

/// <summary>
/// Groups the framework services that <see cref="SceneManager"/> stamps onto every scene.
/// Registered as a singleton and resolved by DI — all constructor parameters are already
/// in the container.
/// </summary>
internal sealed class SceneFrameworkServices(
    ILoggerFactory loggerFactory,
    IRenderer renderer,
    IInputContext inputContext,
    IAudioPlayer audioPlayer,
    IGameContext gameContext)
{
    public ILoggerFactory LoggerFactory => loggerFactory;
    public IRenderer Renderer => renderer;
    public IInputContext InputContext => inputContext;
    public IAudioPlayer AudioPlayer => audioPlayer;
    public IGameContext GameContext => gameContext;
}