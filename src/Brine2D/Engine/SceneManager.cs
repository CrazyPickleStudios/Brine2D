using Brine2D.Graphics;
using Microsoft.Extensions.Logging;

namespace Brine2D.Engine;

public sealed class SceneManager : ISceneManager
{
    private readonly IGameContext _context;
    private readonly ILogger<SceneManager> _logger;

    private IScene? _current;
    private CancellationTokenSource? _initCts;
    private Task? _initTask;
    private IScene? _loading;
    private IScene? _pending;

    public SceneManager(IGameContext context, ILogger<SceneManager> logger)
    {
        _context = context;
        _logger = logger;
    }

    public IScene Current => _current!;

    public void LoadSceneAsync(Func<IScene> sceneFactory, CancellationToken ct = default)
    {
        if (sceneFactory is null)
        {
            throw new ArgumentNullException(nameof(sceneFactory));
        }

        SetSceneAsync(sceneFactory(), ct);
    }

    public void Render(IRenderContext ctx)
    {
        _current?.Render(ctx);
    }

    public void SetInitialScene(IScene scene)
    {
        _current = scene ?? throw new ArgumentNullException(nameof(scene));
    }

    public void SetLoading(IScene loadingScene)
    {
        _loading = loadingScene;
    }

    public void SetSceneAsync(IScene scene, CancellationToken ct = default)
    {
        if (scene is null)
        {
            throw new ArgumentNullException(nameof(scene));
        }

        try
        {
            _initCts?.Cancel();
        }
        catch
        {
            // No-op. -RP
        }

        _initCts?.Dispose();
        _initCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        
        var linkedCt = _initCts.Token;

        _pending = scene;

        if (_loading is not null)
        {
            _current = _loading;
        }

        _initTask = InitializeSceneAsync(scene, linkedCt);
    }

    public void Update(GameTime time)
    {
        if (_initTask is not null)
        {
            if (_initTask.IsCompleted)
            {
                var completedTask = _initTask;
                var next = _pending;

                _initTask = null;
                _pending = null;

                _initCts?.Dispose();
                _initCts = null;

                if (completedTask.IsCompletedSuccessfully)
                {
                    if (next is not null)
                    {
                        _current = next;
                        _logger.LogInformation("Scene {Scene} initialized.", next.GetType().Name);
                    }
                }
                else if (completedTask.IsCanceled)
                {
                    if (next is not null)
                    {
                        _logger.LogInformation("Initialization canceled for scene {Scene}.", next.GetType().Name);
                    }
                }
                else if (completedTask.IsFaulted)
                {
                    var ex = completedTask.Exception is AggregateException ae
                        ? ae.Flatten().InnerException ?? ae
                        : completedTask.Exception!;

                    if (next is not null)
                    {
                        _logger.LogError(ex, "Failed to initialize scene {Scene}.", next.GetType().Name);
                    }
                    else
                    {
                        _logger.LogError(ex, "Failed to initialize scene.");
                    }
                }
            }
        }

        _current?.Update(time);
    }

    private async Task InitializeSceneAsync(IScene scene, CancellationToken ct)
    {
        await scene.InitializeAsync(_context, ct).ConfigureAwait(false);
    }
}