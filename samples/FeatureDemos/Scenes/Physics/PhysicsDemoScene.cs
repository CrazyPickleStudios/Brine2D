using System.Numerics;
using Brine2D.Core;
using Brine2D.ECS;
using Brine2D.ECS.Components;
using Brine2D.Engine;
using Brine2D.Input;
using Brine2D.Performance;
using Brine2D.Physics;
using Brine2D.Systems.Physics;
using Brine2D.Systems.Rendering;

namespace FeatureDemos.Scenes.Physics;

/// <summary>
/// Demonstrates Box2D physics: dynamic bodies falling under gravity,
/// static platforms, restitution, and collision events.
/// Click to spawn circles. Press R to reset. Press D for debug overlay.
/// </summary>
public class PhysicsDemoScene : DemoSceneBase
{
    private Box2DDebugDrawSystem? _debugDraw;

    public PhysicsDemoScene(
        IInputContext input,
        ISceneManager sceneManager,
        IGameContext gameContext,
        PerformanceOverlay? perfOverlay = null)
        : base(input, sceneManager, gameContext, perfOverlay)
    {
    }

    protected override void OnEnter()
    {
        var debug = World.GetSystem<DebugRenderer>();
        if (debug != null) debug.IsEnabled = false;

        World.AddSystem<ShapeRenderSystem>();
        World.AddSystem<Box2DDebugDrawSystem>(s => s.IsEnabled = false);
        _debugDraw = World.GetRenderSystem<Box2DDebugDrawSystem>();

        CreateWalls();
        CreateStack();
        CreateBouncyBall();
    }

    protected override void OnUpdate(GameTime time)
    {
        if (CheckReturnToMenu()) return;
        HandlePerformanceHotkeys();

        if (Input.IsKeyPressed(Key.D) && _debugDraw != null)
            _debugDraw.IsEnabled = !_debugDraw.IsEnabled;

        if (Input.IsKeyPressed(Key.R))
        {
            World.ClearEntities();
            CreateWalls();
            CreateStack();
            CreateBouncyBall();
        }

        if (Input.IsMouseButtonPressed(MouseButton.Left))
            SpawnCircle(Input.MousePosition);
    }

    protected override void OnRender(GameTime time)
    {
        Renderer.ClearColor = new Color(30, 30, 40);

        Renderer.DrawText("Box2D Physics Demo", 20, 10, Color.White);
        Renderer.DrawText("Click to spawn | R reset | D debug | ESC menu", 20, 35, Color.Gray);

        if (_debugDraw is { IsEnabled: true })
            Renderer.DrawText("DEBUG: Box2D shapes, joints, contacts", 20, 60, new Color(0, 255, 100));

        RenderPerformanceOverlay();
    }

    private void CreateWalls()
    {
        World.CreateEntity("Floor")
            .AddComponent<TransformComponent>(t => t.Position = new Vector2(640, 700))
            .AddComponent<RectangleShapeComponent>(s =>
            {
                s.Width = 1280;
                s.Height = 30;
                s.FillColor = new Color(100, 100, 100);
                s.OutlineColor = Color.White;
            })
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(1280, 30);
                c.BodyType = PhysicsBodyType.Static;
                c.SurfaceFriction = 0.6f;
            });

        World.CreateEntity("LeftWall")
            .AddComponent<TransformComponent>(t => t.Position = new Vector2(15, 360))
            .AddComponent<RectangleShapeComponent>(s =>
            {
                s.Width = 30;
                s.Height = 720;
                s.FillColor = new Color(100, 100, 100);
                s.OutlineColor = Color.White;
            })
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(30, 720);
                c.BodyType = PhysicsBodyType.Static;
                c.SurfaceFriction = 0.3f;
            });

        World.CreateEntity("RightWall")
            .AddComponent<TransformComponent>(t => t.Position = new Vector2(1265, 360))
            .AddComponent<RectangleShapeComponent>(s =>
            {
                s.Width = 30;
                s.Height = 720;
                s.FillColor = new Color(100, 100, 100);
                s.OutlineColor = Color.White;
            })
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(30, 720);
                c.BodyType = PhysicsBodyType.Static;
                c.SurfaceFriction = 0.3f;
            });

        World.CreateEntity("Platform")
            .AddComponent<TransformComponent>(t => t.Position = new Vector2(500, 500))
            .AddComponent<RectangleShapeComponent>(s =>
            {
                s.Width = 300;
                s.Height = 15;
                s.FillColor = new Color(100, 100, 100);
                s.OutlineColor = Color.White;
            })
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(300, 15);
                c.BodyType = PhysicsBodyType.Static;
                c.SurfaceFriction = 0.3f;
            });
    }

    private void CreateStack()
    {
        const float boxSize = 40f;
        const float halfBox = boxSize * 0.5f;
        const float floorSurface = 700f - 15f;
        const int rows = 5;

        for (int row = 0; row < rows; row++)
        {
            int cols = rows - row;
            float rowWidth = cols * boxSize;
            float startX = 900f - rowWidth * 0.5f;
            float y = floorSurface - halfBox - row * boxSize;

            for (int col = 0; col < cols; col++)
            {
                float x = startX + halfBox + col * boxSize;

                World.CreateEntity($"Box_{row}_{col}")
                    .AddComponent<TransformComponent>(t => t.Position = new Vector2(x, y))
                    .AddComponent<RectangleShapeComponent>(s =>
                    {
                        s.Width = boxSize;
                        s.Height = boxSize;
                        s.FillColor = new Color(80, 200, 120);
                        s.OutlineColor = Color.White;
                    })
                    .AddComponent<PhysicsBodyComponent>(c =>
                    {
                        c.Shape = new BoxShape(boxSize, boxSize);
                        c.Mass = 1f;
                        c.SurfaceFriction = 0.6f;
                        c.Restitution = 0.05f;
                        c.FixedRotation = true;
                    });
            }
        }
    }

    private void CreateBouncyBall()
    {
        World.CreateEntity("BouncyBall")
            .AddComponent<TransformComponent>(t => t.Position = new Vector2(300, 100))
            .AddComponent<CircleShapeComponent>(s =>
            {
                s.Radius = 25;
                s.FillColor = new Color(255, 100, 100);
                s.OutlineColor = Color.White;
            })
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new CircleShape(25);
                c.Mass = 2f;
                c.Restitution = 0.9f;
            });
    }

    private void SpawnCircle(Vector2 position)
    {
        var radius = Random.Shared.Next(10, 25);

        World.CreateEntity("SpawnedCircle")
            .AddComponent<TransformComponent>(t => t.Position = position)
            .AddComponent<CircleShapeComponent>(s =>
            {
                s.Radius = radius;
                s.FillColor = new Color(80, 200, 120);
                s.OutlineColor = Color.White;
            })
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new CircleShape(radius);
                c.Mass = radius * 0.1f;
                c.Restitution = 0.3f;
                c.SurfaceFriction = 0.5f;
            });
    }
}