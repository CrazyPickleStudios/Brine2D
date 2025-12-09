namespace Brine2D.Engine;

public interface ISceneManager
{
    IScene Current { get; }
    void LoadSceneAsync(Func<IScene> sceneFactory, CancellationToken ct = default);
    void Render(IRenderContext ctx);
    void SetInitialScene(IScene scene);
    void SetLoading(IScene loadingScene);
    void SetSceneAsync(IScene scene, CancellationToken ct = default);
    void Update(GameTime time);
}