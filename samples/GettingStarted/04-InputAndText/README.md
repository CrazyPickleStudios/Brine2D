# 04 - Input and Text

Learn keyboard and mouse input handling, plus text rendering!

## What You'll Learn

- ✅ Keyboard input states (`IsKeyDown`, `IsKeyPressed`, `IsKeyReleased`)
- ✅ Mouse input (position and button clicks)
- ✅ Text input handling
- ✅ Text rendering with colors
- ✅ Vector2 for 2D positions
- ✅ Movement with deltaTime

## Input States Explained

### IsKeyDown - Continuous (Held)

Returns `true` **every frame** while key is held down.

~~~csharp
// Perfect for movement - smooth and continuous
if (_input.IsKeyDown(Keys.W))
    _playerPosition.Y -= speed * deltaTime;
~~~

**Use for:** Movement, aiming, charging

### IsKeyPressed - Single Frame (Pressed)

Returns `true` **only on the frame** when key is first pressed.

~~~csharp
// Perfect for actions - fires once per press
if (_input.IsKeyPressed(Keys.Space))
    Jump();
~~~

**Use for:** Jumping, firing, menu selections, toggles

### IsKeyReleased - Single Frame (Released)

Returns `true` **only on the frame** when key is released.

~~~csharp
// Perfect for charge attacks
if (_input.IsKeyReleased(Keys.Space))
    ReleaseChargedAttack();
~~~

**Use for:** Charge attacks, button release detection

## Mouse Input

### Get Mouse Position

~~~csharp
Vector2 mousePos = _input.MousePosition;
_renderer.DrawText($"Mouse: ({mousePos.X}, {mousePos.Y})", 100, 100, Color.White);
~~~

### Check Mouse Buttons

~~~csharp
// Left click - single press
if (_input.IsMouseButtonPressed(MouseButton.Left))
{
    FireWeapon(mousePos);
}

// Right click - held down
if (_input.IsMouseButtonDown(MouseButton.Right))
{
    AimAt(mousePos);
}
~~~

## Text Rendering

~~~csharp
// Basic text
_renderer.DrawText("Hello!", 100, 100, Color.White);

// Colored text
_renderer.DrawText("Score: 100", 100, 140, Color.Yellow);

// Dynamic text
_renderer.DrawText($"FPS: {fps}", 100, 180, Color.Green);
~~~

## Movement with Vector2

~~~csharp
private Vector2 _playerPosition = new(400, 300);
private const float PlayerSpeed = 200f;

protected override void OnUpdate(GameTime gameTime)
{
    var deltaTime = (float)gameTime.DeltaTime;
    var movement = Vector2.Zero;

    // Accumulate movement
    if (_input.IsKeyDown(Keys.W)) movement.Y -= 1;
    if (_input.IsKeyDown(Keys.S)) movement.Y += 1;
    if (_input.IsKeyDown(Keys.A)) movement.X -= 1;
    if (_input.IsKeyDown(Keys.D)) movement.X += 1;

    // Normalize for diagonal movement
    if (movement != Vector2.Zero)
        movement = Vector2.Normalize(movement);

    // Apply movement with deltaTime for frame-rate independence
    _playerPosition += movement * PlayerSpeed * deltaTime;
}
~~~

## Input Patterns Comparison

| Pattern | Method | Example | Use Case |
|---------|--------|---------|----------|
| **Continuous** | `IsKeyDown` | Movement, aiming | Action needs to happen every frame |
| **Single Press** | `IsKeyPressed` | Jump, fire | Action happens once per press |
| **Single Release** | `IsKeyReleased` | Charge attack | Action happens on button release |

## ASP.NET Parallel

In ASP.NET, you handle input via HTTP requests. In games, you poll input every frame:

| ASP.NET | Brine2D |
|---------|---------|
| Request-based | Frame-based polling |
| `[HttpPost]` | `IsKeyPressed()` |
| Form data | Input state |
| One request → One response | Every frame → Check input |

Different context, same goal: **respond to user input!**

## Sample Features

This sample demonstrates:

1. **WASD/Arrow Keys** - Smooth movement with `IsKeyDown`
2. **SPACE** - Action button with `IsKeyPressed`
3. **R** - Reset position
4. **Text Input** - Type letters and numbers
5. **Backspace** - Delete characters
6. **Mouse Tracking** - Real-time position display
7. **Left Click** - Detect clicks
8. **Right Click** - Teleport to mouse position
9. **Visual Feedback** - Circle player, line to center, bounding box

## Try This

1. **Move player** - Use WASD or Arrow Keys
2. **Press SPACE** - See action in console
3. **Type text** - Letters, numbers, Shift for uppercase
4. **Click mouse** - Left click to detect, right click to teleport
5. **Press R** - Reset player to center

## Run It

~~~sh
dotnet run
~~~

## Controls Reference

**Keyboard:**
- `W/A/S/D` or `Arrow Keys` - Move player
- `SPACE` - Action (demonstrates IsKeyPressed)
- `R` - Reset player position
- `A-Z, 0-9` - Type text
- `Backspace` - Delete text
- `Shift` - Uppercase letters
- `ESC` - Exit

**Mouse:**
- Move - Track position
- `Left Click` - Detect clicks
- `Right Click` - Teleport player

## Code Highlights

### Smooth Movement

~~~csharp
// Frame-rate independent movement
var deltaTime = (float)gameTime.DeltaTime;
var movement = Vector2.Zero;

if (_input.IsKeyDown(Keys.W)) movement.Y -= 1;
if (_input.IsKeyDown(Keys.S)) movement.Y += 1;

_playerPosition += movement * PlayerSpeed * deltaTime;
~~~

### Text Input Handling

~~~csharp
// Check each letter key
for (var key = Keys.A; key <= Keys.Z; key++)
{
    if (_input.IsKeyPressed(key))
    {
        var letter = key.ToString();
        
        // Check for Shift (uppercase)
        if (_input.IsKeyDown(Keys.LeftShift))
            _textInput.Append(letter);
        else
            _textInput.Append(letter.ToLower());
    }
}
~~~

### Vector2 Drawing

~~~csharp
// Draw player with Vector2
_renderer.DrawCircleFilled(_playerPosition, 15f, Color.Lime);

// Draw line from center to player
var center = new Vector2(640, 360);
_renderer.DrawLine(center, _playerPosition, Color.Yellow, 2f);
~~~

## Common Patterns

### Clamping Position to Screen

~~~csharp
_playerPosition.X = Math.Clamp(_playerPosition.X, 0, 1280);
_playerPosition.Y = Math.Clamp(_playerPosition.Y, 0, 720);
~~~

### Tracking Held Keys

~~~csharp
var heldKeys = new List<string>();
if (_input.IsKeyDown(Keys.W)) heldKeys.Add("W");
if (_input.IsKeyDown(Keys.A)) heldKeys.Add("A");
// Display: "Keys Held: W, A"
~~~

### Mouse Click Detection

~~~csharp
if (_input.IsMouseButtonPressed(MouseButton.Left))
{
    var pos = _input.MousePosition;
    Logger.LogInformation("Clicked at ({X}, {Y})", pos.X, pos.Y);
}
~~~

## What's Next?

**You now have all the basics!** Next samples will build on these foundations:

- **CoreConcepts/** - ECS, sprites, collision, camera, audio
- **MiniGames/** - Apply what you learned in real games
- **AdvancedFeatures/** - Particles, spatial audio, post-processing

---

**Input handling is fundamental to interactive games!**