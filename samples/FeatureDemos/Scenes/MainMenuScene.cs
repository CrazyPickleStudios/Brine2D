using Brine2D.Core;
using Brine2D.Engine;
using Brine2D.Engine.Transitions;
using Brine2D.Input;
using Brine2D.Rendering;
using Microsoft.Extensions.Logging;
using FeatureDemos.Scenes.ECS;
using FeatureDemos.Scenes.Transitions;
using FeatureDemos.Scenes.Advanced;
using FeatureDemos.Scenes.Collision;
using FeatureDemos.Scenes.UI;

namespace FeatureDemos.Scenes;

/// <summary>
/// Main menu for navigating between feature demos.
/// Showcases scene transitions and provides easy access to all demos.
/// </summary>
public class MainMenuScene : Scene
{
    private readonly IRenderer _renderer;
    private readonly IInputService _input;
    private readonly ISceneManager _sceneManager;
    private readonly IGameContext _gameContext;
    
    private int _selectedIndex = 0;
    private readonly List<DemoEntry> _demos;
    
    private const float TitleY = 60f;
    private const float CategoryStartY = 180f;
    private const float LineHeight = 55f;  // Increased from 45
    private const float CategorySpacing = 25f;  // Increased from 20

    public MainMenuScene(
        IRenderer renderer,
        IInputService input,
        ISceneManager sceneManager,
        IGameContext gameContext,
        ILogger<MainMenuScene> logger) : base(logger)
    {
        _renderer = renderer;
        _input = input;
        _sceneManager = sceneManager;
        _gameContext = gameContext;
        
        // Define all demos organized by category
        _demos = new List<DemoEntry>
        {
            // ECS Demos
            new("Query System", typeof(QueryDemoScene), "Advanced entity queries with fluent API", "ECS"),
            new("Particle System", typeof(ParticleDemoScene), "GPU-accelerated particle effects", "ECS"),
            
            // Collision Demos
            new("Collision Detection", typeof(CollisionDemoScene), "AABB and circle colliders with physics", "Collision"),
    
            // Transition Demos
            new("Scene Transitions", typeof(TransitionDemoScene), "Fade transitions and loading screens", "Transitions"),
            
            // UI Demos
            new("UI Components", typeof(UIDemoScene), "Buttons, sliders, dialogs, and more", "UI"),
            
            // Advanced Demos
            new("Manual Control", typeof(ManualControlScene), "Opt-out of automatic execution", "Advanced"),
        };
    }

    protected override void OnInitialize()
    {
        _renderer.ClearColor = new Color(15, 20, 35);
        
        Logger.LogInformation("=== Brine2D Feature Demos ===");
        Logger.LogInformation("Available demos: {Count}", _demos.Count);
        Logger.LogInformation("Controls:");
        Logger.LogInformation("  Arrow Keys - Navigate");
        Logger.LogInformation("  Enter/Space - Select");
        Logger.LogInformation("  1-{Max} - Quick select", _demos.Count);
        Logger.LogInformation("  ESC - Exit");
    }

    protected override void OnUpdate(GameTime gameTime)
    {
        // Navigation
        if (_input.IsKeyPressed(Keys.Up))
        {
            _selectedIndex = (_selectedIndex - 1 + _demos.Count) % _demos.Count;
            Logger.LogDebug("Selected: {Name}", _demos[_selectedIndex].DisplayName);
        }
        if (_input.IsKeyPressed(Keys.Down))
        {
            _selectedIndex = (_selectedIndex + 1) % _demos.Count;
            Logger.LogDebug("Selected: {Name}", _demos[_selectedIndex].DisplayName);
        }

        // Selection
        if (_input.IsKeyPressed(Keys.Enter) || _input.IsKeyPressed(Keys.Space))
        {
            LoadSelectedDemo();
        }

        // Number key shortcuts (1-9)
        for (int i = 0; i < Math.Min(_demos.Count, 9); i++)
        {
            var key = Keys.D1 + i; // D1 = '1', D2 = '2', etc.
            if (_input.IsKeyPressed(key))
            {
                _selectedIndex = i;
                LoadSelectedDemo();
            }
        }

        // Exit
        if (_input.IsKeyPressed(Keys.Escape))
        {
            Logger.LogInformation("Exiting Feature Demos");
            _gameContext.RequestExit();
        }
    }

    private void LoadSelectedDemo()
    {
        var selected = _demos[_selectedIndex];
        Logger.LogInformation("Loading demo: {Name}", selected.DisplayName);
        
        // Use fade transition
        _ = _sceneManager.LoadSceneAsync(
            selected.SceneType,
            new FadeTransition(duration: 0.5f, color: Color.Black)
        );
    }

    protected override void OnRender(GameTime gameTime)
    {
        var centerX = (_renderer.Camera?.ViewportWidth ?? 1280) / 2f;
        
        // Title
        DrawCenteredText("BRINE2D FEATURE DEMOS", TitleY, new Color(100, 200, 255), large: true);
        DrawCenteredText("v0.5.0-beta", TitleY + 35, new Color(150, 150, 150));
        
        DrawCenteredText("UP/DOWN Navigate  |  ENTER Select  |  1-9 Quick Select  |  ESC Exit", 
            TitleY + 75, new Color(120, 120, 120));
        
        // Draw separator line
        _renderer.DrawRectangleFilled(300, TitleY + 100, 680, 2, new Color(50, 70, 100));
        
        // Demo list grouped by category
        float currentY = CategoryStartY;
        string? lastCategory = null;
        
        for (int i = 0; i < _demos.Count; i++)
        {
            var demo = _demos[i];
            
            // Draw category header if new category
            if (demo.Category != lastCategory)
            {
                if (lastCategory != null)
                {
                    currentY += CategorySpacing;
                }
                
                _renderer.DrawText(
                    $"--- {demo.Category} ---", 
                    370, 
                    currentY, 
                    new Color(80, 150, 200)
                );
                currentY += LineHeight - 10;
                lastCategory = demo.Category;
            }
            
            var isSelected = i == _selectedIndex;
            
            // Selection background
            if (isSelected)
            {
                _renderer.DrawRectangleFilled(350, currentY - 5, 580, 50, new Color(40, 80, 120, 150));
                _renderer.DrawRectangleOutline(350, currentY - 5, 580, 50, new Color(100, 180, 255), 2f);
            }
            
            if (isSelected)
            {
                _renderer.DrawText(">", 325, currentY, new Color(100, 200, 255));
            }
            
            // Demo number and name
            var nameColor = isSelected ? new Color(255, 255, 255) : new Color(200, 200, 200);
            _renderer.DrawText($"{i + 1}. {demo.DisplayName}", 370, currentY, nameColor);
            
            _renderer.DrawText(demo.Description, 395, currentY + 22, new Color(140, 140, 140));
            
            currentY += LineHeight;
        }
        
        // Footer info
        var footerY = 660f;
        DrawCenteredText($"Total Demos: {_demos.Count}", footerY, new Color(100, 100, 100));
    }

    private void DrawCenteredText(string text, float y, Color color, bool large = false)
    {
        var width = _renderer.Camera?.ViewportWidth ?? 1280;
        var textWidth = text.Length * (large ? 12 : 8);
        var x = (width - textWidth) / 2f;
        
        _renderer.DrawText(text, x, y, color);
    }

    /// <summary>
    /// Represents a demo entry in the menu.
    /// </summary>
    private record DemoEntry(string DisplayName, Type SceneType, string Description, string Category);
}