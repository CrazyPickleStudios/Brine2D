using Brine2D.Core;
using Brine2D.ECS;
using Brine2D.Engine.Transitions;
using Brine2D.Input;
using Brine2D.Rendering;
using Microsoft.Extensions.Logging;
using FeatureDemos.Scenes.ECS;
using FeatureDemos.Scenes.Transitions;
using FeatureDemos.Scenes.Advanced;
using FeatureDemos.Scenes.Audio;
using FeatureDemos.Scenes.Collision;
using FeatureDemos.Scenes.UI;
using FeatureDemos.Scenes.Performance;
using FeatureDemos.Scenes.Rendering;
using Brine2D.Engine;

namespace FeatureDemos.Scenes;

/// <summary>
/// Main menu for navigating between feature demos.
/// Showcases scene transitions and provides easy access to all demos.
/// Uses two-column layout for better space utilization.
/// </summary>
public class MainMenuScene : Scene
{
    private readonly ISceneManager _sceneManager;
    
    private int _selectedIndex = 0;
    private readonly List<DemoEntry> _demos;
    
    private const float TitleY = 40f;
    private const float ColumnStartY = 160f;
    private const float LineHeight = 50f;
    private const float CategorySpacing = 20f;
    private const float ColumnSpacing = 40f;
    private const float LeftColumnX = 100f;
    private const float RightColumnX = 640f;
    private const float ColumnWidth = 520f;

    public MainMenuScene(ISceneManager sceneManager)
    {
        _sceneManager = sceneManager;
        
        // Define all demos organized by category
        _demos = new List<DemoEntry>
        {
            // ECS
            new("Query System",          () => _sceneManager.LoadSceneAsync<QueryDemoScene>(new FadeTransition(0.5f, Color.Black)),           "Advanced entity queries",        "ECS"),
            new("Particle System",       () => _sceneManager.LoadSceneAsync<ParticleDemoScene>(new FadeTransition(0.5f, Color.Black)),         "Pooled particle effects",        "ECS"),

            // Rendering
            new("Texture Atlasing",      () => _sceneManager.LoadSceneAsync<TextureAtlasDemoScene>(new FadeTransition(0.5f, Color.Black)),     "Sprite batching & atlases",      "Rendering"),
            new("Scissor Rects",         () => _sceneManager.LoadSceneAsync<ScissorRectDemoScene>(new FadeTransition(0.5f, Color.Black)),       "UI clipping & scroll views",     "Rendering"),

            // Collision
            new("Collision Detection",   () => _sceneManager.LoadSceneAsync<CollisionDemoScene>(new FadeTransition(0.5f, Color.Black)),         "Physics & colliders",            "Collision"),

            // Audio
            new("Spatial Audio",         () => _sceneManager.LoadSceneAsync<SpatialAudioDemoScene>(new FadeTransition(0.5f, Color.Black)),      "2D spatial audio with panning",  "Audio"),

            // UI
            new("UI Components",         () => _sceneManager.LoadSceneAsync<UIDemoScene>(new FadeTransition(0.5f, Color.Black)),                "Complete UI framework",          "UI"),

            // Transitions
            new("Scene Transitions",     () => _sceneManager.LoadSceneAsync<TransitionDemoScene>(new FadeTransition(0.5f, Color.Black)),        "Fade & loading screens",         "Transitions"),

            // Performance
            new("Performance Benchmark", () => _sceneManager.LoadSceneAsync<SpriteBenchmarkScene>(new FadeTransition(0.5f, Color.Black)),       "Batching & culling test",        "Performance"),
            new("Background Loading",    () => _sceneManager.LoadSceneAsync<BackgroundLoadingDemoScene, CustomLoadingScreen>(),                  "Async asset streaming",          "Performance"),
        };
    }
    
    protected override Task OnLoadAsync(CancellationToken cancellationToken)
    {
        Renderer.ClearColor = new Color(15, 20, 35);
        
        Logger.LogInformation("=== Brine2D Feature Demos ===");
        Logger.LogInformation("Available demos: {Count}", _demos.Count);
        Logger.LogInformation("Controls:");
        Logger.LogInformation("  Arrow Keys - Navigate");
        Logger.LogInformation("  Enter/Space - Select");
        Logger.LogInformation("  1-9, 0 - Quick select");
        Logger.LogInformation("  ESC - Exit");

        return Task.CompletedTask;
    }

    protected override void OnUpdate(GameTime gameTime)
    {
        // Navigation - Up/Down
        if (Input.IsKeyPressed(Key.Up))
        {
            _selectedIndex = (_selectedIndex - 1 + _demos.Count) % _demos.Count;
            Logger.LogDebug("Selected: {Name}", _demos[_selectedIndex].DisplayName);
        }
        if (Input.IsKeyPressed(Key.Down))
        {
            _selectedIndex = (_selectedIndex + 1) % _demos.Count;
            Logger.LogDebug("Selected: {Name}", _demos[_selectedIndex].DisplayName);
        }
        
        // Navigation - Left/Right (jump to adjacent column)
        if (Input.IsKeyPressed(Key.Left))
        {
            var targetIndex = _selectedIndex - GetItemsPerColumn();
            if (targetIndex < 0)
                targetIndex = _demos.Count + targetIndex;
            _selectedIndex = targetIndex % _demos.Count;
        }
        if (Input.IsKeyPressed(Key.Right))
        {
            _selectedIndex = (_selectedIndex + GetItemsPerColumn()) % _demos.Count;
        }

        // Selection
        if (Input.IsKeyPressed(Key.Enter) || Input.IsKeyPressed(Key.Space))
        {
            LoadSelectedDemo();
        }

        // Number key shortcuts (1-9, 0 for 10th item)
        for (int i = 0; i < Math.Min(_demos.Count, 9); i++)
        {
            var key = Key.D1 + i; // D1 = '1', D2 = '2', etc.
            if (Input.IsKeyPressed(key))
            {
                _selectedIndex = i;
                LoadSelectedDemo();
            }
        }
        
        // 0 key for 10th demo
        if (_demos.Count >= 10 && Input.IsKeyPressed(Key.D0))
        {
            _selectedIndex = 9;
            LoadSelectedDemo();
        }

        // Exit
        if (Input.IsKeyPressed(Key.Escape))
        {
            Logger.LogInformation("Exiting Feature Demos");
            Game.RequestExit();
        }
    }

    private void LoadSelectedDemo()
    {
        var selected = _demos[_selectedIndex];
        Logger.LogInformation("Loading demo: {Name}", selected.DisplayName);
        _ = selected.Load();
    }

    protected override void OnRender(GameTime gameTime)
    {
        // Title
        DrawCenteredText("BRINE2D FEATURE DEMOS", TitleY, new Color(100, 200, 255), large: true);
        DrawCenteredText("v0.6.0-beta", TitleY + 30, new Color(150, 150, 150));
        
        DrawCenteredText("↑↓ Navigate  |  ←→ Switch Column  |  ENTER Select  |  1-0 Quick  |  ESC Exit", 
            TitleY + 65, new Color(120, 120, 120));
        
        // Draw separator line
        Renderer.DrawRectangleFilled(50, TitleY + 95, 1180, 2, new Color(50, 70, 100));
        
        // Determine column split
        var itemsPerColumn = GetItemsPerColumn();
        
        // Draw left column
        DrawColumn(_demos.Take(itemsPerColumn).ToList(), LeftColumnX, ColumnStartY, 0);
        
        // Draw right column
        DrawColumn(_demos.Skip(itemsPerColumn).ToList(), RightColumnX, ColumnStartY, itemsPerColumn);
        
        // Draw column separator
        var separatorX = LeftColumnX + ColumnWidth + (ColumnSpacing / 2);
        Renderer.DrawRectangleFilled(separatorX, ColumnStartY - 10, 2, 500, new Color(50, 70, 100));
        
        // Footer info
        var footerY = 660f;
        DrawCenteredText($"Total Demos: {_demos.Count}  |  SDL3 + Vulkan + Multi-Threading", 
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
                
                Renderer.DrawText(
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
                Renderer.DrawRectangleFilled(columnX, currentY - 5, ColumnWidth, 48, new Color(40, 80, 120, 150));
                Renderer.DrawRectangleOutline(columnX, currentY - 5, ColumnWidth, 48, new Color(100, 180, 255), 2f);
            }
            
            // Selection arrow
            if (isSelected)
            {
                Renderer.DrawText(">", columnX - 20, currentY, new Color(100, 200, 255));
            }
            
            // Demo number and name
            var nameColor = isSelected ? new Color(255, 255, 255) : new Color(200, 200, 200);
            var numberStr = globalIndex < 9 ? $"{globalIndex + 1}" : "0";
            Renderer.DrawText($"{numberStr}. {demo.DisplayName}", columnX + 20, currentY, nameColor);
            
            // Description (smaller text)
            Renderer.DrawText(demo.Description, columnX + 40, currentY + 22, new Color(140, 140, 140));
            
            currentY += LineHeight;
        }
    }

    private int GetItemsPerColumn()
    {
        // Split demos into two roughly equal columns
        return (_demos.Count + 1) / 2;
    }

    private void DrawCenteredText(string text, float y, Color color, bool large = false)
    {
        var width = Renderer.Camera?.ViewportWidth ?? 1280;
        var textWidth = text.Length * (large ? 12 : 8);
        var x = (width - textWidth) / 2f;
        
        Renderer.DrawText(text, x, y, color);
    }

    /// <summary>
    /// Represents a demo entry in the menu.
    /// </summary>
    private record DemoEntry(string DisplayName, Func<Task> Load, string Description, string Category);
}