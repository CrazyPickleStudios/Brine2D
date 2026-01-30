using System.Drawing;
using Brine2D.Core;
using Brine2D.Collision;
using Brine2D.Input;
using Brine2D.Rendering;
using Microsoft.Extensions.Logging;
using System.Numerics;
using Brine2D.Engine;
using Brine2D.Performance;

namespace FeatureDemos.Scenes.Collision;

/// <summary>
/// Demo scene showcasing collision detection:
/// - Player movement with collision response
/// - AABB (box) and circle colliders
/// - Static walls and dynamic objects
/// - Bouncing physics
/// - Collectible items (triggers)
/// - Debug visualization
/// </summary>
public class CollisionDemoScene : DemoSceneBase
{
    private readonly CollisionSystem _collisionSystem;
    
    // Player
    private Vector2 _playerPosition = new Vector2(400, 300);
    private readonly BoxCollider _playerCollider;
    private const float PlayerSpeed = 250f;
    private readonly Color PlayerColor = Color.FromArgb(100, 200, 255);
    
    // Static obstacles (walls)
    private readonly List<BoxCollider> _walls = new();
    private readonly Color WallColor = Color.FromArgb(100, 100, 100);
    
    // Dynamic bouncing ball
    private Vector2 _ballPosition = new Vector2(800, 200);
    private Vector2 _ballVelocity = new Vector2(150, 100);
    private readonly CircleCollider _ballCollider;
    private const float BallRadius = 20f;
    private const float Gravity = 600f;
    private readonly Color BallColor = Color.FromArgb(255, 100, 100);
    
    // Collectibles (triggers)
    private readonly List<(CircleCollider collider, Vector2 position)> _coins = new();
    private int _coinsCollected = 0;
    private readonly Color CoinColor = Color.FromArgb(255, 215, 0);
    
    // Pushable box
    private Vector2 _boxPosition = new Vector2(600, 400);
    private Vector2 _boxVelocity = Vector2.Zero;
    private readonly BoxCollider _boxCollider;
    private readonly Color BoxColor = Color.FromArgb(139, 69, 19);
    
    // Debug options
    private bool _showColliders = true;
    private bool _showVelocity = false;

    public CollisionDemoScene(
        IInputContext input,
        ISceneManager sceneManager,
        IGameContext gameContext,
        CollisionSystem collisionSystem,
        PerformanceOverlay? perfOverlay = null)
        : base(input, sceneManager, gameContext, perfOverlay)
    {
        _collisionSystem = collisionSystem;
        
        // Initialize colliders
        _playerCollider = new BoxCollider(32, 48, new Vector2(-16, -24))
        {
            Position = _playerPosition
        };
        
        _ballCollider = new CircleCollider(BallRadius)
        {
            Position = _ballPosition
        };
        
        _boxCollider = new BoxCollider(40, 40, new Vector2(-20, -20))
        {
            Position = _boxPosition
        };
    }

    protected override Task OnInitializeAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("=== Collision Detection Demo ===");
        Logger.LogInformation("Controls:");
        Logger.LogInformation("  WASD - Move player");
        Logger.LogInformation("  R - Kick the ball");
        Logger.LogInformation("  F1 - Toggle collider visualization");
        Logger.LogInformation("  F2 - Toggle velocity vectors");
        Logger.LogInformation("  SPACE - Reset scene");
        Logger.LogInformation("  ESC - Return to menu");
        
        Renderer.ClearColor = Color.FromArgb(20, 25, 35);
        
        SetupScene();

        return Task.CompletedTask;
    }

    private void SetupScene()
    {
        // Clear existing colliders
        _collisionSystem.Clear();
        _walls.Clear();
        _coins.Clear();
        
        // Create boundary walls
        _walls.Add(new BoxCollider(1280, 20) { Position = new Vector2(640, 10) }); // Top
        _walls.Add(new BoxCollider(1280, 20) { Position = new Vector2(640, 710) }); // Bottom
        _walls.Add(new BoxCollider(20, 720) { Position = new Vector2(10, 360) }); // Left
        _walls.Add(new BoxCollider(20, 720) { Position = new Vector2(1270, 360) }); // Right
        
        // Create interior obstacles
        _walls.Add(new BoxCollider(200, 40) { Position = new Vector2(300, 200) });
        _walls.Add(new BoxCollider(40, 200) { Position = new Vector2(500, 400) });
        _walls.Add(new BoxCollider(150, 40) { Position = new Vector2(900, 500) });
        _walls.Add(new BoxCollider(40, 150) { Position = new Vector2(750, 250) });
        
        // Register walls with collision system
        foreach (var wall in _walls)
        {
            _collisionSystem.AddShape(wall);
        }
        
        // Create collectible coins
        _coins.Add((new CircleCollider(15) { Position = new Vector2(200, 150) }, new Vector2(200, 150)));
        _coins.Add((new CircleCollider(15) { Position = new Vector2(600, 200) }, new Vector2(600, 200)));
        _coins.Add((new CircleCollider(15) { Position = new Vector2(400, 500) }, new Vector2(400, 500)));
        _coins.Add((new CircleCollider(15) { Position = new Vector2(1000, 300) }, new Vector2(1000, 300)));
        _coins.Add((new CircleCollider(15) { Position = new Vector2(850, 600) }, new Vector2(850, 600)));
        
        foreach (var (collider, _) in _coins)
        {
            _collisionSystem.AddShape(collider);
        }
        
        // Register dynamic objects
        _collisionSystem.AddShape(_playerCollider);
        _collisionSystem.AddShape(_ballCollider);
        _collisionSystem.AddShape(_boxCollider);
        
        // Reset positions
        _playerPosition = new Vector2(400, 300);
        _ballPosition = new Vector2(800, 200);
        _ballVelocity = new Vector2(150, 100);
        _boxPosition = new Vector2(600, 400);
        _boxVelocity = Vector2.Zero;
        _coinsCollected = 0;
        
        Logger.LogInformation("Scene setup complete: {Walls} walls, {Coins} coins", _walls.Count, _coins.Count);
    }

    protected override void OnUpdate(GameTime gameTime)
    {
        HandlePerformanceHotkeys();

        // Check for return to menu
        if (CheckReturnToMenu()) return;
        
        var deltaTime = (float)gameTime.DeltaTime;
        
        // Toggle debug options
        if (Input.IsKeyPressed(Key.F1))
        {
            _showColliders = !_showColliders;
            Logger.LogInformation("Collider visualization: {State}", _showColliders ? "ON" : "OFF");
        }
        
        if (Input.IsKeyPressed(Key.F2))
        {
            _showVelocity = !_showVelocity;
            Logger.LogInformation("Velocity vectors: {State}", _showVelocity ? "ON" : "OFF");
        }
        
        // Reset scene
        if (Input.IsKeyPressed(Key.Space))
        {
            SetupScene();
            Logger.LogInformation("Scene reset");
        }
        
        // Update player
        UpdatePlayer(deltaTime);
        
        // Update bouncing ball
        UpdateBall(deltaTime);
        
        // Update pushable box
        UpdateBox(deltaTime);
        
        // Check coin collection
        CheckCoinCollection();
    }

    private void UpdatePlayer(float deltaTime)
    {
        var movement = Vector2.Zero;
        
        if (Input.IsKeyDown(Key.W)) movement.Y -= 1;
        if (Input.IsKeyDown(Key.S)) movement.Y += 1;
        if (Input.IsKeyDown(Key.A)) movement.X -= 1;
        if (Input.IsKeyDown(Key.D)) movement.X += 1;
        
        if (movement != Vector2.Zero)
        {
            movement = Vector2.Normalize(movement);
            var moveVector = movement * PlayerSpeed * deltaTime;
            
            // Try full movement
            var newPosition = _playerPosition + moveVector;
            _playerCollider.Position = newPosition;
            
            var collisions = _collisionSystem.GetCollisions(_playerCollider);
            var hitWall = collisions.Any(c => c is BoxCollider box && _walls.Contains(box));
            var hitBox = collisions.Contains(_boxCollider);
            
            if (hitBox)
            {
                // Push the box
                var pushDirection = Vector2.Normalize(_boxPosition - _playerPosition);
                _boxVelocity += pushDirection * 200f * deltaTime;
            }
            
            if (!hitWall)
            {
                _playerPosition = newPosition;
            }
            else
            {
                // Try sliding along X axis
                var slideX = _playerPosition + new Vector2(moveVector.X, 0);
                _playerCollider.Position = slideX;
                
                if (!_collisionSystem.GetCollisions(_playerCollider).Any(c => c is BoxCollider box && _walls.Contains(box)))
                {
                    _playerPosition = slideX;
                }
                else
                {
                    // Try sliding along Y axis
                    var slideY = _playerPosition + new Vector2(0, moveVector.Y);
                    _playerCollider.Position = slideY;
                    
                    if (!_collisionSystem.GetCollisions(_playerCollider).Any(c => c is BoxCollider box && _walls.Contains(box)))
                    {
                        _playerPosition = slideY;
                    }
                    else
                    {
                        // Completely blocked
                        _playerCollider.Position = _playerPosition;
                    }
                }
            }
            
            _playerCollider.Position = _playerPosition;
        }
        
        // Kick ball
        if (Input.IsKeyPressed(Key.R))
        {
            var direction = Vector2.Normalize(_ballPosition - _playerPosition);
            var distance = Vector2.Distance(_playerPosition, _ballPosition);
            
            if (distance < 100f)
            {
                _ballVelocity = direction * 400f;
                Logger.LogInformation("Ball kicked!");
            }
        }
    }

    private void UpdateBall(float deltaTime)
    {
        // Apply gravity
        _ballVelocity.Y += Gravity * deltaTime;
        
        // Apply air resistance
        _ballVelocity *= 0.99f;
        
        // Update position
        var newPosition = _ballPosition + _ballVelocity * deltaTime;
        _ballCollider.Position = newPosition;
        
        // Check collisions
        var collisions = _collisionSystem.GetCollisions(_ballCollider);
        
        foreach (var collision in collisions)
        {
            if (collision is BoxCollider wall && _walls.Contains(wall))
            {
                var wallBounds = wall.GetBounds();
                var ballBounds = _ballCollider.GetBounds();
                var ballRect = new RectangleF(ballBounds.X, ballBounds.Y, ballBounds.Width, ballBounds.Height);
                var penetration = wallBounds.GetPenetration(ballRect);
                
                if (penetration != Vector2.Zero)
                {
                    newPosition = CollisionResponse.Push(newPosition, penetration);
                    _ballVelocity = CollisionResponse.Bounce(_ballVelocity, penetration, 0.7f);
                    _ballCollider.Position = newPosition;
                }
            }
            else if (collision == _playerCollider)
            {
                var direction = Vector2.Normalize(_ballPosition - _playerPosition);
                _ballVelocity = direction * 300f;
                newPosition += direction * 5f; // Push away
            }
            else if (collision == _boxCollider)
            {
                var direction = Vector2.Normalize(_ballPosition - _boxPosition);
                _ballVelocity = direction * 250f;
                _boxVelocity = direction * 150f;
                newPosition += direction * 5f;
            }
        }
        
        _ballPosition = newPosition;
        _ballCollider.Position = _ballPosition;
    }

    private void UpdateBox(float deltaTime)
    {
        // Apply friction
        _boxVelocity *= 0.95f;
        
        if (_boxVelocity.LengthSquared() < 1f)
        {
            _boxVelocity = Vector2.Zero;
        }
        
        if (_boxVelocity != Vector2.Zero)
        {
            var newPosition = _boxPosition + _boxVelocity * deltaTime;
            _boxCollider.Position = newPosition;
            
            var collisions = _collisionSystem.GetCollisions(_boxCollider);
            var hitWall = collisions.Any(c => c is BoxCollider wall && _walls.Contains(wall));
            
            if (!hitWall)
            {
                _boxPosition = newPosition;
            }
            else
            {
                _boxVelocity = Vector2.Zero;
                _boxCollider.Position = _boxPosition;
            }
        }
    }

    private void CheckCoinCollection()
    {
        _playerCollider.Position = _playerPosition;
        var collisions = _collisionSystem.GetCollisions(_playerCollider);
        
        for (int i = _coins.Count - 1; i >= 0; i--)
        {
            var (collider, position) = _coins[i];
            
            if (collisions.Contains(collider))
            {
                _collisionSystem.RemoveShape(collider);
                _coins.RemoveAt(i);
                _coinsCollected++;
                Logger.LogInformation("Coin collected! Total: {Count}", _coinsCollected);
            }
        }
    }

    protected override void OnRender(GameTime gameTime)
    {
        // Draw walls
        foreach (var wall in _walls)
        {
            var bounds = wall.GetBounds();
            Renderer.DrawRectangleFilled(bounds.X, bounds.Y, bounds.Width, bounds.Height, WallColor);
            
            if (_showColliders)
            {
                Renderer.DrawRectangleOutline(bounds.X, bounds.Y, bounds.Width, bounds.Height, Color.White, 1f);
            }
        }
        
        // Draw coins
        foreach (var (collider, position) in _coins)
        {
            Renderer.DrawCircleFilled(position.X, position.Y, 15f, CoinColor);
            
            if (_showColliders)
            {
                Renderer.DrawCircleOutline(position.X, position.Y, 15f, Color.White, 1);
            }
        }
        
        // Draw pushable box
        Renderer.DrawRectangleFilled(_boxPosition.X - 20, _boxPosition.Y - 20, 40, 40, BoxColor);
        if (_showColliders)
        {
            var boxBounds = _boxCollider.GetBounds();
            Renderer.DrawRectangleOutline(boxBounds.X, boxBounds.Y, boxBounds.Width, boxBounds.Height, Color.Yellow, 2f);
        }
        
        // Draw ball
        Renderer.DrawCircleFilled(_ballPosition.X, _ballPosition.Y, BallRadius, BallColor);
        if (_showColliders)
        {
            Renderer.DrawCircleOutline(_ballPosition.X, _ballPosition.Y, BallRadius, Color.Red, 2);
        }
        
        // Draw player
        Renderer.DrawRectangleFilled(_playerPosition.X - 16, _playerPosition.Y - 24, 32, 48, PlayerColor);
        if (_showColliders)
        {
            var playerBounds = _playerCollider.GetBounds();
            Renderer.DrawRectangleOutline(playerBounds.X, playerBounds.Y, playerBounds.Width, playerBounds.Height, Color.Cyan, 2f);
        }
        
        // Draw velocity vectors
        if (_showVelocity)
        {
            // Ball velocity
            var ballVelEnd = _ballPosition + _ballVelocity * 0.1f;
            DrawArrow(_ballPosition, ballVelEnd, Color.Red);
            
            // Box velocity
            if (_boxVelocity != Vector2.Zero)
            {
                var boxVelEnd = _boxPosition + _boxVelocity * 0.1f;
                DrawArrow(_boxPosition, boxVelEnd, Color.Yellow);
            }
        }
        
        // Draw UI
        Renderer.DrawText("Collision Detection Demo", 10, 10, Color.White);
        Renderer.DrawText($"Coins: {_coinsCollected} / {_coins.Count + _coinsCollected}", 10, 35, CoinColor);
        Renderer.DrawText($"Colliders: {(_showColliders ? "ON" : "OFF")} (F1)", 10, 60, Color.Gray);
        Renderer.DrawText($"Velocity: {(_showVelocity ? "ON" : "OFF")} (F2)", 10, 85, Color.Gray);
        Renderer.DrawText("WASD: Move | R: Kick Ball | SPACE: Reset | ESC: Menu", 10, 680, Color.Gray);

        RenderPerformanceOverlay();
    }

    private void DrawArrow(Vector2 start, Vector2 end, Color color)
    {
        Renderer.DrawLine(start.X, start.Y, end.X, end.Y, color, 2f);
        
        // Arrow head
        var direction = Vector2.Normalize(end - start);
        var perpendicular = new Vector2(-direction.Y, direction.X);
        
        var arrowPoint1 = end - direction * 10f + perpendicular * 5f;
        var arrowPoint2 = end - direction * 10f - perpendicular * 5f;
        
        Renderer.DrawLine(end.X, end.Y, arrowPoint1.X, arrowPoint1.Y, color, 2f);
        Renderer.DrawLine(end.X, end.Y, arrowPoint2.X, arrowPoint2.Y, color, 2f);
    }
}