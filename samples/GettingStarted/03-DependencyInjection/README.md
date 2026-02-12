# 03 - Dependency Injection

Learn how Brine2D uses the same DI patterns as ASP.NET Core!

## What You'll Learn

- ✅ Custom service interfaces and implementations
- ✅ Service registration (`AddSingleton`, `AddTransient`, `AddScoped`)
- ✅ Constructor injection
- ✅ `ILogger<T>` structured logging
- ✅ `IOptions<T>` configuration pattern
- ✅ JSON configuration binding

## Service Architecture

~~~
Program.cs
  ↓ Register services
  ↓
DI Container
  ↓ Inject into scenes
  ↓
GameScene (receives services via constructor)
~~~

## Key Concepts

### 1. Create Service Interface

~~~csharp
// Services/IScoreService.cs
public interface IScoreService
{
    float GetScore();
    void AddPoints(float points);
    void ResetScore();
}
~~~

### 2. Implement Service

~~~csharp
// Services/ScoreService.cs
public class ScoreService : IScoreService
{
    private readonly ILogger<ScoreService> _logger;
    private float _currentScore;

    // Services can inject other services!
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

### 3. Register Service

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

    // DI provides the service automatically!
    public GameScene(
        IScoreService scoreService,
        ILogger<GameScene> logger) : base(logger)
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
|----------|-------------|---------|
| **Singleton** | One instance for entire game | Game state, score tracker, audio manager |
| **Transient** | New instance every time | Temporary calculations, factory patterns |
| **Scoped** | One instance per scene (rare) | Scene-specific state |

### Singleton (Most Common)

~~~csharp
builder.Services.AddSingleton<IScoreService, ScoreService>();
~~~

**Use for:** Services that maintain state across the entire game.

### Transient

~~~csharp
builder.Services.AddTransient<IRandomGenerator, RandomGenerator>();
~~~

**Use for:** Stateless services or when you need fresh instances.

### Scoped

~~~csharp
builder.Services.AddScoped<ISceneState, SceneState>();
~~~

**Use for:** Scene-specific state (rarely needed in games).

## Configuration with IOptions<T>

### 1. Create Options Class

~~~csharp
// Options/GameOptions.cs
public class GameOptions
{
    public int PointsPerSecond { get; set; } = 10;
    public string PlayerName { get; set; } = "Player";
}
~~~

### 2. Add Configuration File

~~~json
// gamesettings.json
{
  "Game": {
    "PointsPerSecond": 50,
    "PlayerName": "ASP.NET Dev"
  }
}
~~~

### 3. Bind Configuration

~~~csharp
// Program.cs
builder.Services.Configure<GameOptions>(
    builder.Configuration.GetSection("Game"));
~~~

### 4. Inject and Use

~~~csharp
public GameScene(
    IOptions<GameOptions> gameOptions,
    ILogger<GameScene> logger) : base(logger)
{
    _gameOptions = gameOptions.Value;  // Unwrap IOptions<T>
    
    Logger.LogInformation(
        "Loaded config: PointsPerSecond={Points}",
        _gameOptions.PointsPerSecond);
}
~~~

## Structured Logging

Brine2D uses Microsoft.Extensions.Logging - same as ASP.NET!

~~~csharp
// Log levels
_logger.LogDebug("Detailed diagnostic info");
_logger.LogInformation("General info");
_logger.LogWarning("Something unexpected");
_logger.LogError("An error occurred");

// Structured logging (parameters)
_logger.LogInformation(
    "Score increased from {OldScore} to {NewScore}",
    oldScore, newScore);
~~~

**Configure log levels in gamesettings.json:**

~~~json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "DependencyInjection.Services": "Debug"
    }
  }
}
~~~

## ASP.NET Parallels

| ASP.NET Core | Brine2D | Same? |
|--------------|---------|-------|
| `services.AddSingleton<T>()` | `services.AddSingleton<T>()` | ✅ Identical |
| `IOptions<T>` pattern | `IOptions<T>` pattern | ✅ Identical |
| `ILogger<T>` | `ILogger<T>` | ✅ Identical |
| Constructor injection | Constructor injection | ✅ Identical |
| `gamesettings.json` | `gamesettings.json` | ✅ Same concept |

**If you know ASP.NET DI, you already know Brine2D DI!**

## Try This

1. Run the sample - score increases automatically
2. Press SPACE - resets score
3. Check console - see structured logging
4. Edit `gamesettings.json` - change `PointsPerSecond`
5. Restart - see new configuration loaded

## Run It

~~~sh
dotnet run
~~~

**Expected Console Output:**

~~~
info: DependencyInjection.Services.ScoreService[0]
      ScoreService created (Singleton)
info: DependencyInjection.GameScene[0]
      GameScene initialized with config: PointsPerSecond=50, PlayerName=ASP.NET Dev
info: DependencyInjection.GameScene[0]
      GameScene: OnEnter
dbug: DependencyInjection.Services.ScoreService[0]
      Score reset from 0.0 to 0
~~~

## Controls

- `SPACE` - Reset score
- `ESC` - Exit

## Project Structure

~~~
03-DependencyInjection/
├── Program.cs                    # Service registration
├── GameScene.cs                  # Uses injected services
├── Services/
│   ├── IScoreService.cs          # Interface
│   └── ScoreService.cs           # Implementation
├── Options/
│   └── GameOptions.cs            # Configuration class
└── gamesettings.json             # Configuration file
~~~

## What's Next?

- **04-InputAndText** - Keyboard, mouse, and text rendering

---

**Brine2D uses the same DI patterns you already know from ASP.NET!**