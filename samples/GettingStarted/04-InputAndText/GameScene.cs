using System.Drawing;
using System.Numerics;
using System.Text;
using Brine2D.Core;
using Brine2D.Engine;
using Brine2D.Input;
using Brine2D.Rendering;
using Microsoft.Extensions.Logging;

namespace InputAndText;

/// <summary>
/// Demonstrates keyboard and mouse input, plus text rendering.
/// </summary>
public class GameScene : Scene
{
    private readonly IInputContext _input;
    private readonly IGameContext _gameContext;

    // Player position (controlled by input)
    private Vector2 _playerPosition = new(400, 300);
    private const float PlayerSpeed = 200f; // pixels per second

    // Text input demo
    private readonly StringBuilder _textInput = new();
    private const int MaxInputLength = 20;

    // Mouse tracking
    private Vector2 _mousePosition;
    private bool _mouseClicked;

    // Input state tracking
    private string _lastKeyPressed = "None";
    private string _keysCurrentlyHeld = "";

    public GameScene(
        IInputContext input,
        IGameContext gameContext)
    {
        _input = input;
        _gameContext = gameContext;
    }

    protected override Task OnLoadAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("GameScene: OnLoad");
        Renderer.ClearColor = Color.FromArgb(255, 52, 78, 65); // Dirty brine

        // Reset state
        _playerPosition = new Vector2(400, 300);
        _textInput.Clear();
        _textInput.Append("Type here!");
        
        return Task.CompletedTask;
    }

    protected override void OnUpdate(GameTime gameTime)
    {
        var deltaTime = (float)gameTime.DeltaTime;
        var movement = Vector2.Zero;

        // WASD movement
        if (_input.IsKeyDown(Key.W) || _input.IsKeyDown(Key.Up))
            movement.Y -= PlayerSpeed * deltaTime;
        
        if (_input.IsKeyDown(Key.S) || _input.IsKeyDown(Key.Down))
            movement.Y += PlayerSpeed * deltaTime;
        
        if (_input.IsKeyDown(Key.A) || _input.IsKeyDown(Key.Left))
            movement.X -= PlayerSpeed * deltaTime;
        
        if (_input.IsKeyDown(Key.D) || _input.IsKeyDown(Key.Right))
            movement.X += PlayerSpeed * deltaTime;

        _playerPosition += movement;

        // Clamp to screen
        _playerPosition.X = Math.Clamp(_playerPosition.X, 0, 1280);
        _playerPosition.Y = Math.Clamp(_playerPosition.Y, 0, 720);

        // Track which keys are held (for display)
        var heldKeys = new List<string>();
        if (_input.IsKeyDown(Key.W)) heldKeys.Add("W");
        if (_input.IsKeyDown(Key.A)) heldKeys.Add("A");
        if (_input.IsKeyDown(Key.S)) heldKeys.Add("S");
        if (_input.IsKeyDown(Key.D)) heldKeys.Add("D");
        _keysCurrentlyHeld = heldKeys.Count > 0 ? string.Join(", ", heldKeys) : "None";

        // ============================================================
        // KEYBOARD INPUT - Actions (Pressed Once)
        // ============================================================
        // IsKeyPressed returns true ONLY on the frame when key is first pressed

        if (_input.IsKeyPressed(Key.Space))
        {
            _lastKeyPressed = "SPACE";
            Logger.LogInformation("Space bar pressed!");
        }

        if (_input.IsKeyPressed(Key.R))
        {
            _lastKeyPressed = "R";
            ResetPlayer();
        }

        // ============================================================
        // KEYBOARD INPUT - Text Input Demo
        // ============================================================
        // Backspace to delete
        if (_input.IsKeyPressed(Key.Backspace) && _textInput.Length > 0)
        {
            _textInput.Length--;
        }

        // Letter keys for text input (simplified - real games use SDL_TextInputEvent)
        if (_textInput.Length < MaxInputLength)
        {
            // Check alphabet keys
            for (var key = Key.A; key <= Key.Z; key++)
            {
                if (_input.IsKeyPressed(key))
                {
                    var letter = key.ToString();
                    // Check if Shift is held for uppercase
                    if (_input.IsKeyDown(Key.LeftShift) || _input.IsKeyDown(Key.RightShift))
                        _textInput.Append(letter);
                    else
                        _textInput.Append(letter.ToLower());
                }
            }

            // Numbers
            if (_input.IsKeyPressed(Key.D0)) _textInput.Append('0');
            if (_input.IsKeyPressed(Key.D1)) _textInput.Append('1');
            if (_input.IsKeyPressed(Key.D2)) _textInput.Append('2');
            if (_input.IsKeyPressed(Key.D3)) _textInput.Append('3');
            if (_input.IsKeyPressed(Key.D4)) _textInput.Append('4');
            if (_input.IsKeyPressed(Key.D5)) _textInput.Append('5');
            if (_input.IsKeyPressed(Key.D6)) _textInput.Append('6');
            if (_input.IsKeyPressed(Key.D7)) _textInput.Append('7');
            if (_input.IsKeyPressed(Key.D8)) _textInput.Append('8');
            if (_input.IsKeyPressed(Key.D9)) _textInput.Append('9');
        }

        // ============================================================
        // MOUSE INPUT
        // ============================================================
        // Get mouse position
        _mousePosition = _input.MousePosition;

        // Check for mouse clicks
        _mouseClicked = false;
        if (_input.IsMouseButtonPressed(MouseButton.Left))
        {
            _mouseClicked = true;
            Logger.LogInformation("Mouse clicked at ({X}, {Y})", _mousePosition.X, _mousePosition.Y);
        }

        // Right click to teleport player
        if (_input.IsMouseButtonPressed(MouseButton.Right))
        {
            var mousePos = _input.MousePosition;
            _playerPosition = new Vector2(mousePos.X, mousePos.Y);
            Logger.LogInformation("Player teleported to mouse position");
        }
        
        // ============================================================
        // EXIT
        // ============================================================
        if (_input.IsKeyPressed(Key.Escape))
        {
            _gameContext.RequestExit();
        }
    }

    protected override void OnRender(GameTime gameTime)
    {
        // ============================================================
        // TEXT RENDERING - Title and Instructions
        // ============================================================
        Renderer.DrawText("INPUT AND TEXT DEMO", 100, 50, Color.White);

        // Different colors and positions
        Renderer.DrawText("Keyboard & Mouse Input", 100, 90, Color.LightGray);

        // ============================================================
        // KEYBOARD INPUT DISPLAY
        // ============================================================
        Renderer.DrawText("KEYBOARD INPUT:", 100, 140, Color.Yellow);
        Renderer.DrawText($"Keys Held: {_keysCurrentlyHeld}", 100, 170, Color.White);
        Renderer.DrawText($"Last Pressed: {_lastKeyPressed}", 100, 200, Color.White);

        // Instructions
        Renderer.DrawText("• WASD or Arrow Keys - Move player (IsKeyDown)", 100, 240, Color.Gray);
        Renderer.DrawText("• SPACE - Action (IsKeyPressed)", 100, 270, Color.Gray);
        Renderer.DrawText("• R - Reset position", 100, 300, Color.Gray);

        // ============================================================
        // TEXT INPUT DISPLAY
        // ============================================================
        Renderer.DrawText("TEXT INPUT:", 100, 350, Color.Yellow);
        Renderer.DrawText($"Type: {_textInput}", 100, 380, Color.Cyan);
        Renderer.DrawText("(Type letters/numbers, Backspace to delete)", 100, 410, Color.Gray);

        // ============================================================
        // MOUSE INPUT DISPLAY
        // ============================================================
        Renderer.DrawText("MOUSE INPUT:", 100, 460, Color.Yellow);
        Renderer.DrawText($"Position: ({_mousePosition.X}, {_mousePosition.Y})", 100, 490, Color.White);
        Renderer.DrawText($"Left Click: {(_mouseClicked ? "CLICKED!" : "No")}", 100, 520, Color.White);
        Renderer.DrawText("• Left Click - Detect click", 100, 560, Color.Gray);
        Renderer.DrawText("• Right Click - Teleport player", 100, 590, Color.Gray);

        // ============================================================
        // PLAYER RENDERING
        // ============================================================
        // Draw player as a simple rectangle
        Renderer.DrawCircleFilled(_playerPosition, 15f, Color.Lime);
        
        // Draw line from center to player
        var center = new Vector2(640, 360);
        Renderer.DrawLine(center, _playerPosition, Color.Yellow, 2f);
        
        // Draw rectangle at player position
        var rect = new Rectangle((int)_playerPosition.X - 20, (int)_playerPosition.Y - 20, 40, 40);
        Renderer.DrawRectangleOutline(rect, Color.Cyan, 2f);

        // Draw player label
        Renderer.DrawText("YOU", (int)_playerPosition.X - 15, (int)_playerPosition.Y - 40, Color.White);

        // ============================================================
        // EXIT INSTRUCTION
        // ============================================================
        Renderer.DrawText("Press ESC to exit", 100, 650, Color.DarkGray);
    }

    private void ResetPlayer()
    {
        _playerPosition = new Vector2(400, 300);
        Logger.LogInformation("Player position reset");
    }
}