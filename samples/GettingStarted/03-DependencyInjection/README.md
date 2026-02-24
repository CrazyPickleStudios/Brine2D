# 03 - Dependency Injection

Learn how Brine2D uses the same DI patterns as ASP.NET Core.

## What You'll Learn

- Custom service interfaces and implementations
- Service registration (`AddSingleton`, `AddTransient`, `AddScoped`)
- Constructor injection for your own services
- `ILogger<T>` structured logging
- Game options as plain registered singletons

## Service Architecture

~~~
Program.cs
  ↓ Register services
  ↓
DI Container
  ↓ Inject into scenes
  ↓
GameScene (receives custom services via constructor)
~~~

## Key Concepts

### 1. Create a Service Interface

~~~csharp
// Services/IScoreService.cs
public interface IScoreService
{
    float GetScore();
    void AddPoints(float points);
    void ResetScore();
}
~~~

### 2. Implement the Service

~~~csharp
// Services/ScoreService.cs
public class ScoreService : IScoreService
{
    private readonly ILogger<ScoreService> _logger;
    private float _currentScore;

    // Services can inject other services
    public ScoreService(ILogger<ScoreService> logger)
    {
        _logger = logger;
    }

    public void AddPoints(float points)
    {
        _currentScore += points;
        _logger.LogDebug("Score increased to {Score}", _currentScore);
    }

    // ... other methods
}
~~~

### 3. Register the Service

~~~csharp
// Program.cs
builder.Services.AddSingleton<IScoreService, ScoreService>();
~~~

### 4. Inject and Use

~~~csharp
// GameScene.cs
public class GameScene : Scene
{
    private readonly IScoreService _scoreService;

    // Only inject YOUR services; framework properties handle the rest
    public GameScene(IScoreService scoreService)
    {
        _scoreService = scoreService;
    }

    protected override void OnUpdate(GameTime gameTime)
    {
        _scoreService.AddPoints(10 * (float)gameTime.DeltaTime);
    }
}
~~~

## Service Lifetimes

| Lifetime | When to Use | Example |
|---|---|---|
| **Singleton** | One instance for the entire game | Game state, score tracker |
| **Transient** | New instance every time | Stateless helpers, factory patterns |
| **Scoped** | One instance per scene (rare) | Scene-specific state |

---

## Configuration

For game-specific settings, create a plain class and register it as a singleton. No framework wrappers needed.

~~~csharp
// Options/GameOptions.cs
public class GameOptions
{
    public int PointsPerSecond { get; set; } = 10;
    public string PlayerName   { get; set; } = "Player";
}
~~~

Register in `Program.cs`:

~~~csharp
builder.Services.AddSingleton(new GameOptions
{
    PointsPerSecond = 50,
    PlayerName      = "Player"
});
~~~

Inject directly like any other service:

~~~csharp
public GameScene(IScoreService scoreService, GameOptions gameOptions)
{
    _scoreService = scoreService;
    _gameOptions  = gameOptions;
}

protected override void OnEnter()
{
    Logger.LogInformation(
        "Config: PointsPerSecond={Points}",
        _gameOptions.PointsPerSecond);
}
~~~

---

## Structured Logging

Brine2D uses `Microsoft.Extensions.Logging`, the same as ASP.NET Core.

~~~csharp
// Use the Logger framework property (available from OnLoadAsync onwards)
Logger.LogDebug("Detailed diagnostic info");
Logger.LogInformation("General info");
Logger.LogWarning("Something unexpected");
Logger.LogError("An error occurred");

// Structured parameters, not string interpolation
Logger.LogInformation(
    "Score increased from {OldScore} to {NewScore}",
    oldScore, newScore);
~~~

---

## ASP.NET Parallels

| ASP.NET Core | Brine2D |
|---|---|
| `services.AddSingleton<T>()` | `services.AddSingleton<T>()` |
| `ILogger<T>` | `ILogger<T>` (same interface) |
| Constructor injection | Constructor injection (your services only) |
| `ControllerBase` properties | `Scene` properties (`Renderer`, `Input`, `Audio`) |

## Try This

1. Run the sample; score increases automatically
2. Press SPACE to reset the score
3. Check the console to see structured logging
4. Change `PointsPerSecond` in `Program.cs` and restart

## Run It

~~~sh
dotnet run
~~~