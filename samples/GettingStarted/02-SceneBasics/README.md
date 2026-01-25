# 02 - Scene Basics

Learn how scenes work in Brine2D! This sample demonstrates scene lifecycle, transitions, and state management.

## What You'll Learn

- ✅ Scene lifecycle hooks (`OnEnter`, `OnExit`)
- ✅ Multiple scenes (Menu → Game → Menu)
- ✅ Scene transitions with `LoadSceneAsync<T>()`
- ✅ State management (resetting on scene enter)
- ✅ Scene registration

## Scene Lifecycle

~~~
Constructor → OnEnter → OnUpdate/OnRender loop → OnExit
~~~

**Key Hooks:**
- `OnEnter()` - Called when scene becomes active (reset state here)
- `OnExit()` - Called when leaving scene (cleanup, save state)
- `OnUpdate()` - Called every frame for game logic
- `OnRender()` - Called every frame for rendering

## The Flow

~~~
MenuScene (active)
  ↓ Press SPACE
  ↓ MenuScene.OnExit()
  ↓
GameScene.OnEnter() → game starts, score resets
  ↓ Score increases over time...
  ↓ Press ESC
  ↓ GameScene.OnExit()
  ↓
MenuScene.OnEnter() → menu resets
~~~

## Key Code Patterns

### Registering Multiple Scenes

~~~csharp
// Program.cs
builder.Services.AddScene<MenuScene>();
builder.Services.AddScene<GameScene>();

var game = builder.Build();
await game.RunAsync<MenuScene>();  // Start with MenuScene
~~~

### Transitioning Between Scenes

~~~csharp
// In any scene
if (_input.IsKeyPressed(Keys.Space))
{
    _sceneManager.LoadSceneAsync<GameScene>();  // Switch to GameScene
}
~~~

### Resetting State on Enter

~~~csharp
protected override void OnEnter()
{
    // Reset state every time scene becomes active
    _score = 0;
    _elapsedTime = 0f;
    
    Logger.LogInformation("GameScene: OnEnter - Game starting");
}
~~~

### Cleanup on Exit

~~~csharp
protected override void OnExit()
{
    // Called before leaving scene
    Logger.LogInformation($"GameScene: OnExit - Final score: {_score}");
    
    // Stop music, save state, etc.
}
~~~

## ASP.NET Parallel

In ASP.NET, Controllers handle different routes. In Brine2D, Scenes handle different game states:

| ASP.NET | Brine2D |
|---------|---------|
| Multiple Controllers | Multiple Scenes |
| `RedirectToAction()` | `LoadSceneAsync<T>()` |
| Controller initialization | Scene `OnEnter()` |
| Controller disposal | Scene `OnExit()` |

Both are about **navigation** and **state management** - just in different contexts!

## Try This

1. Run the sample - starts at MenuScene
2. Press SPACE - transitions to GameScene
3. Watch score increase over time
4. Press ESC - returns to MenuScene (score resets!)
5. Check console logs - see lifecycle hooks being called

## Run It

~~~sh
dotnet run
~~~

**Console Output:**
~~~
MenuScene: Constructor called
MenuScene: OnEnter - Scene is now active
(Press SPACE)
MenuScene: Transitioning to GameScene
MenuScene: OnExit - Scene is being unloaded
GameScene: OnEnter - Game starting
(Score increases...)
(Press ESC)
GameScene: OnExit - Final score was 5
MenuScene: OnEnter - Scene is now active
~~~

## Controls

**MenuScene:**
- `SPACE` - Start game
- `ESC` - Exit application

**GameScene:**
- `ESC` - Return to menu
- `Q` - Quit game

## Important Concepts

### Why OnEnter/OnExit?

- **OnEnter** can be called multiple times (returning to menu)
- **OnExit** can be called multiple times (leaving for pause menu)
- Separates **state lifecycle** from **resource lifecycle**

### When to Use Each Hook

| Hook | Purpose | Called When |
|------|---------|-------------|
| `Constructor` | Inject dependencies | Scene is created (once) |
| `OnInitializeAsync()` | Fast setup | Scene first loads (once) |
| `OnLoadAsync()` | Load assets | Scene first loads (once) |
| **`OnEnter()`** | **Reset state, start music** | **Scene becomes active** |
| `OnUpdate()` | Game logic | Every frame |
| `OnRender()` | Drawing | Every frame |
| **`OnExit()`** | **Stop music, save state** | **Scene becomes inactive** |
| `OnUnloadAsync()` | Cleanup resources | Scene is destroyed (once) |

## What's Next?

- **03-DependencyInjection** - Custom services and configuration
- **04-InputAndText** - Detailed input handling

---

**Scenes are like ASP.NET Controllers - they manage state and navigation!**