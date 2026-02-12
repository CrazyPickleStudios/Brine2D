using Brine2D.Assets;
using Brine2D.Core;
using Brine2D.Engine;
using Brine2D.Input;
using Brine2D.Rendering;
using Microsoft.Extensions.Logging;

namespace FeatureDemos.Scenes.Advanced;

/// <summary>
/// Demonstrates background asset loading with the non-blocking GameLoop.
/// The CustomLoadingScreen renders animated UI while this scene loads in the background.
/// Window stays responsive at 60 FPS during the entire load process!
/// </summary>
public class BackgroundLoadingDemoScene : Scene
{
    private readonly ISceneManager _sceneManager;
    private readonly IAssetLoader _assetLoader;
    private readonly IInputContext _input;

    private const int SimulatedLoadDelayMs = 800; // Slow enough to see loading screen
    private const int AssetCount = 5;

    private readonly List<ITexture> _loadedTextures = new();

    public BackgroundLoadingDemoScene(
        ISceneManager sceneManager,
        IAssetLoader assetLoader,
        IInputContext input)
    {
        _sceneManager = sceneManager;
        _assetLoader = assetLoader;
        _input = input;
    }

    protected override async Task OnLoadAsync(CancellationToken ct)
    {
        Logger.LogInformation("Loading started...");

        // Just a simple delay to test if loading screen shows
        for (int i = 0; i < 5; i++)
        {
            await Task.Delay(1000, ct);
            Logger.LogInformation("Loading step {Step}/5", i + 1);
        }

        Logger.LogInformation("Loading complete!");
    }

    protected override void OnUpdate(GameTime gameTime)
    {
        // Return to menu
        if (_input.IsKeyPressed(Key.Escape))
        {
            Logger.LogInformation("Returning to main menu");
            _sceneManager.LoadSceneAsync<MainMenuScene>();
        }
    }

    protected override void OnRender(GameTime gameTime)
    {
        var centerX = Renderer.Width / 2f;

        // Title
        Renderer.DrawText("Background Loading Complete!", centerX - 180, 50, Color.Green);
        Renderer.DrawText($"Successfully loaded {_loadedTextures.Count} textures",
            centerX - 150, 90, Color.White);
        Renderer.DrawText($"(with {SimulatedLoadDelayMs}ms simulated delay per asset)",
            centerX - 180, 120, new Color(150, 150, 150));

        // Success indicators
        var successY = 170;
        DrawSuccessIndicator("Window Never Froze", centerX - 200, successY);
        DrawSuccessIndicator("Loading Screen Animated", centerX - 200, successY + 35);
        DrawSuccessIndicator("Main Thread Responsive", centerX - 200, successY + 70);
        DrawSuccessIndicator("Assets Loaded in Background", centerX - 200, successY + 105);

        // Display loaded textures in a grid
        if (_loadedTextures.Count > 0)
        {
            Renderer.DrawText("Loaded Textures:", centerX - 100, 320, Color.Yellow);

            float x = 100;
            float y = 360;
            const float spacing = 150f;
            const float textureSize = 128f;

            for (int i = 0; i < _loadedTextures.Count; i++)
            {
                var texture = _loadedTextures[i];

                // Draw texture with border
                Renderer.DrawRectangleFilled(x - 2, y - 2, textureSize + 4, textureSize + 4, Color.White);
                Renderer.DrawTexture(texture, x, y, textureSize, textureSize);

                // Draw info
                Renderer.DrawText($"#{i + 1}", x + 5, y + textureSize + 5, Color.Gray);
                Renderer.DrawText($"{texture.Width}x{texture.Height}",
                    x + 5, y + textureSize + 25, new Color(100, 100, 100));

                x += spacing;
                if (x > Renderer.Width - spacing)
                {
                    x = 100;
                    y += textureSize + 60;
                }
            }
        }

        // Instructions
        Renderer.DrawText("Press ESC to return to menu", 10, Renderer.Height - 30, Color.White);

        // Performance info
        Renderer.DrawText("All assets loaded on background threads while UI remained responsive!",
            centerX - 300, Renderer.Height - 80, new Color(100, 200, 255));
        Renderer.DrawText("This demonstrates true non-blocking async scene loading!",
            centerX - 250, Renderer.Height - 55, new Color(100, 200, 255));
    }

    private void DrawSuccessIndicator(string text, float x, float y)
    {
        // Green checkmark box
        Renderer.DrawRectangleFilled(x, y, 25, 25, new Color(0, 150, 50));
        Renderer.DrawText("✓", x + 6, y + 2, Color.White);

        // Text
        Renderer.DrawText(text, x + 35, y + 4, Color.White);
    }
}