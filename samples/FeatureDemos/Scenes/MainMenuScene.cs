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
using FeatureDemos.Scenes.Performance;

namespace FeatureDemos.Scenes;

/// <summary>
/// Main menu for navigating between feature demos.
/// Showcases scene transitions and provides easy access to all demos.
/// Uses two-column layout for better space utilization.
/// </summary>
public class MainMenuScene : Scene
{
    private readonly IRenderer _renderer;
    private readonly IInputService _input;
    private readonly ISceneManager _sceneManager;
    private readonly IGameContext _gameContext;
    
    private int _selectedIndex = 0;
    private readonly List<DemoEntry> _demos;
    
    private const float TitleY = 40f;
    private const float ColumnStartY = 160f;
    private const float LineHeight = 50f;
    private const float CategorySpacing = 20f;
    private const float ColumnSpacing = 40f; // Space between columns
    private const float LeftColumnX = 100f;
    private const float RightColumnX = 640f;
    private const float ColumnWidth = 520f;

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
            // ECS Demos (Left Column)
            new("Query System", typeof(QueryDemoScene), "Advanced entity queries", "ECS"),
            new("Particle System", typeof(ParticleDemoScene), "Pooled particle effects", "ECS"),
            
            // Collision Demos (Left Column)
            new("Collision Detection", typeof(CollisionDemoScene), "Physics & colliders", "Collision"),
            
            // UI Demos (Right Column)
            new("UI Components", typeof(UIDemoScene), "Complete UI framework", "UI"),
            
            // Transition Demos (Right Column)
            new("Scene Transitions", typeof(TransitionDemoScene), "Fade & loading screens", "Transitions"),
            
            // Performance Demos (Right Column)
            new("Performance Benchmark", typeof(SpriteBenchmarkScene), "Batching & culling test", "Performance"),
            
            // Advanced Demos (Right Column)
            new("Manual Control", typeof(ManualControlScene), "Opt-out lifecycle hooks", "Advanced"),
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
        // Navigation - Up/Down
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
        
        // Navigation - Left/Right (jump to adjacent column)
        if (_input.IsKeyPressed(Keys.Left))
        {
            // Jump to left column (wrap around)
            var targetIndex = _selectedIndex - GetItemsPerColumn();
            if (targetIndex < 0)
                targetIndex = _demos.Count + targetIndex;
            _selectedIndex = targetIndex % _demos.Count;
        }
        if (_input.IsKeyPressed(Keys.Right))
        {
            // Jump to right column (wrap around)
            _selectedIndex = (_selectedIndex + GetItemsPerColumn()) % _demos.Count;
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
        // Title
        DrawCenteredText("BRINE2D FEATURE DEMOS", TitleY, new Color(100, 200, 255), large: true);
        DrawCenteredText("v0.6.0-beta", TitleY + 30, new Color(150, 150, 150));
        
        DrawCenteredText("↑↓ Navigate  |  ←→ Switch Column  |  ENTER Select  |  1-9 Quick  |  ESC Exit", 
            TitleY + 65, new Color(120, 120, 120));
        
        // Draw separator line
        _renderer.DrawRectangleFilled(50, TitleY + 95, 1180, 2, new Color(50, 70, 100));
        
        // Determine column split (distribute demos evenly)
        var itemsPerColumn = GetItemsPerColumn();
        
        // Draw left column
        DrawColumn(_demos.Take(itemsPerColumn).ToList(), LeftColumnX, ColumnStartY, 0);
        
        // Draw right column
        DrawColumn(_demos.Skip(itemsPerColumn).ToList(), RightColumnX, ColumnStartY, itemsPerColumn);
        
        // Draw column separator
        var separatorX = LeftColumnX + ColumnWidth + (ColumnSpacing / 2);
        _renderer.DrawRectangleFilled(separatorX, ColumnStartY - 10, 2, 450, new Color(50, 70, 100));
        
        // Footer info
        var footerY = 660f;
        DrawCenteredText($"Total Demos: {_demos.Count}  |  Batched Rendering + Frustum Culling + Object Pooling", 
            footerY, new Color(100, 100, 100));
    }

    private void DrawColumn(List<DemoEntry> demos, float columnX, float startY, int indexOffset)
    {
        float currentY = startY;
        string? lastCategory = null;
        
        for (int i = 0; i < demos.Count; i++)
        {
            var demo = demos[i];
            var globalIndex = i + indexOffset;
            
            // Draw category header if new category
            if (demo.Category != lastCategory)
            {
                if (lastCategory != null)
                {
                    currentY += CategorySpacing;
                }
                
                _renderer.DrawText(
                    $"--- {demo.Category} ---", 
                    columnX + 20, 
                    currentY, 
                    new Color(80, 150, 200)
                );
                currentY += LineHeight - 10;
                lastCategory = demo.Category;
            }
            
            var isSelected = globalIndex == _selectedIndex;
            
            // Selection background
            if (isSelected)
            {
                _renderer.DrawRectangleFilled(columnX, currentY - 5, ColumnWidth, 48, new Color(40, 80, 120, 150));
                _renderer.DrawRectangleOutline(columnX, currentY - 5, ColumnWidth, 48, new Color(100, 180, 255), 2f);
            }
            
            // Selection arrow
            if (isSelected)
            {
                _renderer.DrawText(">", columnX - 20, currentY, new Color(100, 200, 255));
            }
            
            // Demo number and name
            var nameColor = isSelected ? new Color(255, 255, 255) : new Color(200, 200, 200);
            _renderer.DrawText($"{globalIndex + 1}. {demo.DisplayName}", columnX + 20, currentY, nameColor);
            
            // Description (smaller text)
            _renderer.DrawText(demo.Description, columnX + 40, currentY + 22, new Color(140, 140, 140));
            
            currentY += LineHeight;
        }
    }

    private int GetItemsPerColumn()
    {
        // Split demos into two roughly equal columns
        return (_demos.Count + 1) / 2; // Ceiling division
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