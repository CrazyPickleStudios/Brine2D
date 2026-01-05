using Brine2D.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Brine2D.Engine
{
    /// <summary>
    /// Default implementation of scene management.
    /// </summary>
    public class SceneManager : ISceneManager
    {
        private readonly ILogger<SceneManager> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly Dictionary<Type, Type> _registeredScenes;

        public IScene? CurrentScene { get; private set; }

        public SceneManager(ILogger<SceneManager> logger, IServiceProvider serviceProvider)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _registeredScenes = new Dictionary<Type, Type>();
        }

        public void RegisterScene<TScene>() where TScene : IScene
        {
            var sceneType = typeof(TScene);
            _registeredScenes[sceneType] = sceneType;
            _logger.LogDebug("Registered scene: {SceneType}", sceneType.Name);
        }

        public Task LoadSceneAsync<TScene>(CancellationToken cancellationToken = default) where TScene : IScene
        {
            return LoadSceneAsync(typeof(TScene), cancellationToken);
        }

        public async Task LoadSceneAsync(Type sceneType, CancellationToken cancellationToken = default)
        {
            if (!typeof(IScene).IsAssignableFrom(sceneType))
            {
                throw new ArgumentException($"Type {sceneType.Name} does not implement IScene", nameof(sceneType));
            }

            _logger.LogInformation("Loading scene: {SceneType}", sceneType.Name);

            // Unload current scene
            if (CurrentScene != null)
            {
                await CurrentScene.UnloadAsync(cancellationToken);
            }

            // Create new scene instance from DI
            var scene = (IScene)_serviceProvider.GetRequiredService(sceneType);

            // Initialize and load
            scene.Initialize();
            await scene.LoadAsync(cancellationToken);

            CurrentScene = scene;
            _logger.LogInformation("Scene loaded: {SceneName}", scene.Name);
        }

        public void Update(GameTime gameTime)
        {
            CurrentScene?.Update(gameTime);
        }

        public void Render(GameTime gameTime)
        {
            CurrentScene?.Render(gameTime);
        }
    }
}