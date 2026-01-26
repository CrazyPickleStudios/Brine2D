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
    private readonly IRenderer _renderer;
    private readonly IInputService _input;
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
        IRenderer renderer,
        IInputService input,
        IGameContext gameContext,
        ILogger<GameScene> logger) : base(logger)
    {
        _renderer = renderer;
        _input = input;
        _gameContext = gameContext;
    }

    protected override Task OnLoadAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("GameScene: OnLoad");
        _renderer.ClearColor = Color.FromArgb(255, 52, 78, 65); // Dirty brine

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
        if (_input.IsKeyDown(Keys.W) || _input.IsKeyDown(Keys.Up))
            movement.Y -= PlayerSpeed * deltaTime;
        
        if (_input.IsKeyDown(Keys.S) || _input.IsKeyDown(Keys.Down))
            movement.Y += PlayerSpeed * deltaTime;
        
        if (_input.IsKeyDown(Keys.A) || _input.IsKeyDown(Keys.Left))
            movement.X -= PlayerSpeed * deltaTime;
        
        if (_input.IsKeyDown(Keys.D) || _input.IsKeyDown(Keys.Right))
            movement.X += PlayerSpeed * deltaTime;

        _playerPosition += movement;

        // Clamp to screen
        _playerPosition.X = Math.Clamp(_playerPosition.X, 0, 1280);
        _playerPosition.Y = Math.Clamp(_playerPosition.Y, 0, 720);

        // Track which keys are held (for display)
        var heldKeys = new List<string>();
        if (_input.IsKeyDown(Keys.W)) heldKeys.Add("W");
        if (_input.IsKeyDown(Keys.A)) heldKeys.Add("A");
        if (_input.IsKeyDown(Keys.S)) heldKeys.Add("S");
        if (_input.IsKeyDown(Keys.D)) heldKeys.Add("D");
        _keysCurrentlyHeld = heldKeys.Count > 0 ? string.Join(", ", heldKeys) : "None";

        // ============================================================
        // KEYBOARD INPUT - Actions (Pressed Once)
        // ============================================================
        // IsKeyPressed returns true ONLY on the frame when key is first pressed

        if (_input.IsKeyPressed(Keys.Space))
        {
            _lastKeyPressed = "SPACE";
            Logger.LogInformation("Space bar pressed!");
        }

        if (_input.IsKeyPressed(Keys.R))
        {
            _lastKeyPressed = "R";
            ResetPlayer();
        }

        // ============================================================
        // KEYBOARD INPUT - Text Input Demo
        // ============================================================
        // Backspace to delete
        if (_input.IsKeyPressed(Keys.Backspace) && _textInput.Length > 0)
        {
            _textInput.Length--;
        }

        // Letter keys for text input (simplified - real games use SDL_TextInputEvent)
        if (_textInput.Length < MaxInputLength)
        {
            // Check alphabet keys
            for (var key = Keys.A; key <= Keys.Z; key++)
            {
                if (_input.IsKeyPressed(key))
                {
                    var letter = key.ToString();
                    // Check if Shift is held for uppercase
                    if (_input.IsKeyDown(Keys.LeftShift) || _input.IsKeyDown(Keys.RightShift))
                        _textInput.Append(letter);
                    else
                        _textInput.Append(letter.ToLower());
                }
            }

            // Numbers
            if (_input.IsKeyPressed(Keys.D0)) _textInput.Append('0');
            if (_input.IsKeyPressed(Keys.D1)) _textInput.Append('1');
            if (_input.IsKeyPressed(Keys.D2)) _textInput.Append('2');
            if (_input.IsKeyPressed(Keys.D3)) _textInput.Append('3');
            if (_input.IsKeyPressed(Keys.D4)) _textInput.Append('4');
            if (_input.IsKeyPressed(Keys.D5)) _textInput.Append('5');
            if (_input.IsKeyPressed(Keys.D6)) _textInput.Append('6');
            if (_input.IsKeyPressed(Keys.D7)) _textInput.Append('7');
            if (_input.IsKeyPressed(Keys.D8)) _textInput.Append('8');
            if (_input.IsKeyPressed(Keys.D9)) _textInput.Append('9');
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
        if (_input.IsKeyPressed(Keys.Escape))
        {
            _gameContext.RequestExit();
        }
    }

    protected override void OnRender(GameTime gameTime)
    {
        // ============================================================
        // TEXT RENDERING - Title and Instructions
        // ============================================================
        _renderer.DrawText("INPUT AND TEXT DEMO", 100, 50, Color.White);

        // Different colors and positions
        _renderer.DrawText("Keyboard & Mouse Input", 100, 90, Color.LightGray);

        // ============================================================
        // KEYBOARD INPUT DISPLAY
        // ============================================================
        _renderer.DrawText("KEYBOARD INPUT:", 100, 140, Color.Yellow);
        _renderer.DrawText($"Keys Held: {_keysCurrentlyHeld}", 100, 170, Color.White);
        _renderer.DrawText($"Last Pressed: {_lastKeyPressed}", 100, 200, Color.White);

        // Instructions
        _renderer.DrawText("• WASD or Arrow Keys - Move player (IsKeyDown)", 100, 240, Color.Gray);
        _renderer.DrawText("• SPACE - Action (IsKeyPressed)", 100, 270, Color.Gray);
        _renderer.DrawText("• R - Reset position", 100, 300, Color.Gray);

        // ============================================================
        // TEXT INPUT DISPLAY
        // ============================================================
        _renderer.DrawText("TEXT INPUT:", 100, 350, Color.Yellow);
        _renderer.DrawText($"Type: {_textInput}", 100, 380, Color.Cyan);
        _renderer.DrawText("(Type letters/numbers, Backspace to delete)", 100, 410, Color.Gray);

        // ============================================================
        // MOUSE INPUT DISPLAY
        // ============================================================
        _renderer.DrawText("MOUSE INPUT:", 100, 460, Color.Yellow);
        _renderer.DrawText($"Position: ({_mousePosition.X}, {_mousePosition.Y})", 100, 490, Color.White);
        _renderer.DrawText($"Left Click: {(_mouseClicked ? "CLICKED!" : "No")}", 100, 520, Color.White);
        _renderer.DrawText("• Left Click - Detect click", 100, 560, Color.Gray);
        _renderer.DrawText("• Right Click - Teleport player", 100, 590, Color.Gray);

        // ============================================================
        // PLAYER RENDERING
        // ============================================================
        // Draw player as a simple rectangle
        _renderer.DrawCircleFilled(_playerPosition, 15f, Color.Lime);
        
        // Draw line from center to player
        var center = new Vector2(640, 360);
        _renderer.DrawLine(center, _playerPosition, Color.Yellow, 2f);
        
        // Draw rectangle at player position
        var rect = new Rectangle((int)_playerPosition.X - 20, (int)_playerPosition.Y - 20, 40, 40);
        _renderer.DrawRectangleOutline(rect, Color.Cyan, 2f);

        // Draw player label
        _renderer.DrawText("YOU", (int)_playerPosition.X - 15, (int)_playerPosition.Y - 40, Color.White);

        // ============================================================
        // EXIT INSTRUCTION
        // ============================================================
        _renderer.DrawText("Press ESC to exit", 100, 650, Color.DarkGray);
    }

    private void ResetPlayer()
    {
        _playerPosition = new Vector2(400, 300);
        Logger.LogInformation("Player position reset");
    }
}