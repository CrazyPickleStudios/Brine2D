using Brine2D.Core;
using Brine2D.Engine;
using Brine2D.Input;
using Brine2D.Rendering;
using Microsoft.Extensions.Logging;

namespace FeatureDemos.Scenes.Rendering
{
    /// <summary>
    /// Demonstrates scissor rectangle clipping functionality.
    /// Shows basic clipping, nested clipping, and scroll view simulation.
    /// </summary>
    public class ScissorRectDemoScene : Scene
    {
        private readonly IInputContext _input;
        private readonly ISceneManager _sceneManager;

        private float _scrollOffset = 0f;
        private const float ScrollSpeed = 200f;

        public ScissorRectDemoScene(
            IInputContext input,
            ISceneManager sceneManager)
        {
            _input = input;
            _sceneManager = sceneManager;
        }

        protected override Task OnLoadAsync(CancellationToken cancellationToken)
        {
            Logger.LogInformation("Loading Scissor Rect Demo Scene");
            Renderer.ClearColor = new Color(30, 30, 40);
            return Task.CompletedTask;
        }

        protected override void OnUpdate(GameTime gameTime) 
        {
            var deltaTime = (float)gameTime.DeltaTime;

            // Scroll with arrow keys
            if (_input.IsKeyDown(Key.Up))
                _scrollOffset -= ScrollSpeed * deltaTime;
            if (_input.IsKeyDown(Key.Down))
                _scrollOffset += ScrollSpeed * deltaTime;

            // Clamp scroll
            _scrollOffset = Math.Clamp(_scrollOffset, -500f, 500f);

            // Reset with R
            if (_input.IsKeyPressed(Key.R))
                _scrollOffset = 0f;

            // Return to menu
            if (_input.IsKeyPressed(Key.Escape))
                _ = _sceneManager.LoadSceneAsync<MainMenuScene>();
        }

        protected override void OnRender(GameTime gameTime)  
        {
            // Title
            Renderer.DrawText("Scissor Rect Demo - Press Arrow Keys to Scroll, R to Reset", 20, 20, Color.White);

            Renderer.SetScissorRect(null);
            DrawBasicClippingDemo();

            Renderer.SetScissorRect(null);
            DrawNestedClippingDemo();

            Renderer.SetScissorRect(null);
            DrawScrollViewDemo();

            Renderer.SetScissorRect(null);  
            DrawMultiplePanelsDemo();

            Renderer.SetScissorRect(null);  
            DrawInstructions();
        }

        private void DrawBasicClippingDemo()
        {
            var panelBounds = new Rectangle(50, 80, 300, 200);

            // Draw panel background
            Renderer.DrawRectangleFilled(panelBounds, new Color(50, 50, 60));
            Renderer.DrawRectangleOutline(panelBounds, Color.Yellow, 2f);
            Renderer.DrawText("Basic Clipping", panelBounds.X + 10, panelBounds.Y + 10, Color.White);

            // Enable scissor rect
            Renderer.SetScissorRect(new Rectangle(
                panelBounds.X + 10,
                panelBounds.Y + 40,
                panelBounds.Width - 20,
                panelBounds.Height - 50));

            // Draw content that extends beyond bounds (will be clipped)
            for (int i = 0; i < 5; i++)
            {
                var y = panelBounds.Y + 40 + i * 50;
                Renderer.DrawRectangleFilled(
                    panelBounds.X + 20, y, 400, 40,
                    new Color(100, 150, 200));
                Renderer.DrawText($"Clipped Item {i + 1}", panelBounds.X + 30, y + 10, Color.White);
            }

            // Disable clipping
            Renderer.SetScissorRect(null);
        }

        private void DrawNestedClippingDemo()
        {
            var outerBounds = new Rectangle(400, 80, 350, 200);
            var innerBounds = new Rectangle(420, 120, 200, 120);

            // Outer panel
            Renderer.DrawRectangleFilled(outerBounds, new Color(50, 60, 50));
            Renderer.DrawRectangleOutline(outerBounds, Color.Green, 2f);
            Renderer.DrawText("Nested Clipping", outerBounds.X + 10, outerBounds.Y + 10, Color.White);

            // Outer clip
            Renderer.PushScissorRect(new Rectangle(
                outerBounds.X + 10,
                outerBounds.Y + 40,
                outerBounds.Width - 20,
                outerBounds.Height - 50));

            // Draw background that's clipped to outer
            Renderer.DrawRectangleFilled(
                outerBounds.X + 20, outerBounds.Y + 50, 400, 140,
                new Color(70, 80, 70));

            // Inner panel outline
            Renderer.DrawRectangleOutline(innerBounds, Color.Cyan, 2f);

            // Inner clip (intersected with outer)
            Renderer.PushIntersectedScissorRect(innerBounds);

            // Draw content clipped to inner bounds only
            for (int i = 0; i < 8; i++)
            {
                var y = innerBounds.Y + i * 25;
                Renderer.DrawRectangleFilled(
                    innerBounds.X + 5, y, 300, 20,
                    new Color(150, 200, 150));
                Renderer.DrawText($"Inner {i + 1}", innerBounds.X + 10, y + 3, Color.Black);
            }

            // Pop both clips
            Renderer.PopScissorRect(); // Inner
            Renderer.PopScissorRect(); // Outer
        }

        private void DrawScrollViewDemo()
        {
            var scrollBounds = new Rectangle(50, 320, 700, 200);

            // Scroll view background
            Renderer.DrawRectangleFilled(scrollBounds, new Color(40, 40, 50));
            Renderer.DrawRectangleOutline(scrollBounds, Color.Magenta, 2f);
            Renderer.DrawText($"Scroll View (Offset: {_scrollOffset:F0})",
                scrollBounds.X + 10, scrollBounds.Y + 10, Color.White);

            // Enable clipping for scroll area
            Renderer.SetScissorRect(new Rectangle(
                scrollBounds.X + 10,
                scrollBounds.Y + 40,
                scrollBounds.Width - 20,
                scrollBounds.Height - 50));

            // Draw scrollable content
            for (int i = 0; i < 20; i++)
            {
                var y = scrollBounds.Y + 50 + i * 60 - _scrollOffset;

                // Background
                Renderer.DrawRectangleFilled(
                    scrollBounds.X + 20, y, scrollBounds.Width - 40, 50,
                    i % 2 == 0 ? new Color(80, 80, 100) : new Color(90, 90, 110));

                // Text
                Renderer.DrawText($"Scroll Item {i + 1} - This content scrolls!",
                    scrollBounds.X + 30, y + 15, Color.White);

                // Icon
                Renderer.DrawCircleFilled(scrollBounds.X + scrollBounds.Width - 50, y + 25, 15,
                    new Color(200, 100, 100));
            }

            // Disable clipping
            Renderer.SetScissorRect(null);

            // Draw scrollbar
            var scrollbarHeight = scrollBounds.Height - 50;
            var contentHeight = 20 * 60f;
            var scrollbarY = (_scrollOffset / contentHeight) * scrollbarHeight;
            Renderer.DrawRectangleFilled(
                scrollBounds.Right - 15, scrollBounds.Y + 40 + scrollbarY,
                10, 50,
                Color.Gray);
        }

        private void DrawMultiplePanelsDemo()
        {
            var panel1 = new Rectangle(50, 530, 200, 100);
            var panel2 = new Rectangle(280, 530, 200, 100);
            var panel3 = new Rectangle(510, 530, 200, 100);

            Renderer.DrawText("Multiple Panels (Extension Method)", 50, 510, Color.White);

            // Panel 1
            Renderer.DrawRectangleFilled(panel1, new Color(60, 40, 40));
            Renderer.DrawRectangleOutline(panel1, Color.Red, 2f);
            Renderer.DrawText("Panel 1", panel1.X + 10, panel1.Y + 10, Color.White);
            
            Renderer.WithScissorRect(panel1, () =>
            {
                Renderer.DrawCircleFilled(panel1.Center, 60, new Color(255, 100, 100, 180));
            });

            // Panel 2
            Renderer.DrawRectangleFilled(panel2, new Color(40, 60, 40));
            Renderer.DrawRectangleOutline(panel2, Color.Green, 2f);
            Renderer.DrawText("Panel 2", panel2.X + 10, panel2.Y + 10, Color.White);
            
            Renderer.WithScissorRect(panel2, () =>
            {
                Renderer.DrawCircleFilled(panel2.Center, 60, new Color(100, 255, 100, 180));
            });

            // Panel 3
            Renderer.DrawRectangleFilled(panel3, new Color(40, 40, 60));
            Renderer.DrawRectangleOutline(panel3, Color.Blue, 2f);
            Renderer.DrawText("Panel 3", panel3.X + 10, panel3.Y + 10, Color.White);
            
            Renderer.WithScissorRect(panel3, () =>
            {
                Renderer.DrawCircleFilled(panel3.Center, 60, new Color(100, 100, 255, 180));
            });
        }

        private void DrawInstructions()
        {
            var instructions = new[]
            {
                "Arrow Up/Down: Scroll the scroll view",
                "R: Reset scroll position",
                "ESC: Back to Menu",
                "",
                "Notice how content is clipped to panel bounds!",
                "The nested clipping demo shows intersection clipping."
            };

            var y = Renderer.Height - 120;
            foreach (var line in instructions)
            {
                Renderer.DrawText(line, 20, y, Color.LightGray);
                y += 20;
            }
        }

        protected override Task OnUnloadAsync(CancellationToken cancellationToken)
        {
            Logger.LogInformation("Unloading Scissor Rect Demo Scene");
            return Task.CompletedTask;
        }
    }
}