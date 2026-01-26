using Brine2D.Core;
using Brine2D.Engine;
using Brine2D.Input;
using Brine2D.Rendering;
using Microsoft.Extensions.Logging;
using System.Drawing;

namespace SceneBasics
{
    /// <summary>
    /// Menu scene demonstrating scene lifecycle.
    /// Lifecycle order: Constructor → OnInitialize → OnLoad → OnUpdate/OnRender loop → OnUnload
    /// </summary>
    public class MenuScene : Scene
    {
        private readonly IRenderer _renderer;
        private readonly IInputService _input;
        private readonly ISceneManager _sceneManager;
        private readonly IGameContext _gameContext;

        public MenuScene(
            IRenderer renderer,
            IInputService input,
            ISceneManager sceneManager,
            IGameContext gameContext,
            ILogger<MenuScene> logger) : base(logger)
        {
            _renderer = renderer;
            _input = input;
            _sceneManager = sceneManager;
            _gameContext = gameContext;

            Logger.LogInformation("MenuScene: Constructor called");
        }

        // OnLoad: Called when scene loads - initialize state and load resources
        protected override Task OnLoadAsync(CancellationToken cancellationToken)
        {
            Logger.LogInformation("MenuScene: OnLoad - Scene is loading");
            _renderer.ClearColor = Color.DarkSlateBlue;
            return Task.CompletedTask;
        }

        protected override void OnUpdate(GameTime gameTime)
        {
            // SPACE starts game
            if (_input.IsKeyPressed(Keys.Space))
            {
                Logger.LogInformation("MenuScene: Transitioning to GameScene");
                _sceneManager.LoadSceneAsync<GameScene>();
            }
            
            // ESC exits application
            if (_input.IsKeyPressed(Keys.Escape))
            {
                Logger.LogInformation("MenuScene: Exiting application");
                _gameContext.RequestExit();
            }
        }

        protected override void OnRender(GameTime gameTime)
        {
            _renderer.DrawText("MAIN MENU", 100, 100, Color.White);
            _renderer.DrawText("Press SPACE to start game", 100, 140, Color.LightGray);
            _renderer.DrawText("Press ESC to exit", 100, 180, Color.LightGray);
        }

        // OnUnload: Called when scene unloads - cleanup resources
        protected override Task OnUnloadAsync(CancellationToken cancellationToken)
        {
            Logger.LogInformation("MenuScene: OnUnload - Scene is being unloaded");
            // Cleanup resources here (if needed)
            return Task.CompletedTask;
        }
    }
}
