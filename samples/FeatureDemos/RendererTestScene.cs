using Brine2D.Core;
using Brine2D.Rendering;
using Microsoft.Extensions.Logging;

namespace FeatureDemos.Scenes;

public class RendererTestScene : IScene
{
    private readonly IRenderer _renderer;
    private readonly ILogger<RendererTestScene> _logger;
    private int _frameCount = 0;

    public RendererTestScene(IRenderer renderer, ILogger<RendererTestScene> logger)
    {
        _renderer = renderer;
        _logger = logger;
    }

    public Task LoadAsync()
    {
        _logger.LogInformation("RendererTestScene loaded");
        return Task.CompletedTask;
    }

    public string Name { get; }
    public bool IsActive { get; }
    public bool EnableLifecycleHooks { get; } = true;
    public bool EnableAutomaticFrameManagement { get; } = true;
    public void Initialize()
    {
        _renderer.ClearColor = Color.CornflowerBlue;
    }

    public Task LoadAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public void Update(GameTime deltaTime)
    {
        _frameCount++;

        if (_frameCount % 60 == 0)
        {
            _logger.LogInformation("RendererTestScene update - Frame {Frame}", _frameCount);
        }
    }

    public void Render(GameTime gameTime)
    {
        _logger.LogInformation("RendererTestScene Render called - Frame {Frame}", _frameCount);
        
        // Try to draw a simple red rectangle in the center
        _logger.LogInformation("Drawing test rectangle at (100, 100, 200, 200)");
        _renderer.DrawRectangleFilled(100, 100, 200, 200, Color.Red);

        // Also try a green rectangle
        _logger.LogInformation("Drawing test rectangle at (400, 300, 150, 150)");
        _renderer.DrawRectangleFilled(400, 300, 150, 150, Color.Green);

        _logger.LogInformation("RendererTestScene Render complete");
    }

    public Task UnloadAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task UnloadAsync()
    {
        _logger.LogInformation("RendererTestScene unloaded");
        return Task.CompletedTask;
    }
}