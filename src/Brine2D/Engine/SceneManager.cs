namespace Brine2D.Engine;

public sealed class SceneManager : ISceneManager
{
    private readonly IGameContext _context;
    private IScene? _current;
    private IScene? _loading;

    public SceneManager(IGameContext context)
    {
        _context = context;
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

        if (_loading is not null)
        {
            _current = _loading;
        }

        var next = scene;
        
        next.InitializeAsync(_context, ct).ContinueWith(t =>
        {
            if (t.IsCompletedSuccessfully)
            {
                _current = next;
            }
            else if (t.IsFaulted)
            {
                // TODO: log t.Exception; keep showing current (or loading) scene, or swap to an error scene. -RP
            }
        }, TaskScheduler.Default);
    }

    public void Update(GameTime time)
    {
        _current?.Update(time);
    }
}