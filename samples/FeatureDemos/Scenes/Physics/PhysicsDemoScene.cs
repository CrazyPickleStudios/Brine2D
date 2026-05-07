using System.Numerics;
using Brine2D.Core;
using Brine2D.ECS;
using Brine2D.ECS.Components;
using Brine2D.ECS.Components.Joints;
using Brine2D.ECS.Systems;
using Brine2D.Engine;
using Brine2D.Input;
using Brine2D.Performance;
using Brine2D.Physics;
using Brine2D.Systems.Physics;
using Brine2D.Systems.Rendering;

namespace FeatureDemos.Scenes.Physics;

/// <summary>
/// Comprehensive Box2D physics showcase.
///
/// Left zone   — mixed-shape sandbox: pyramid of boxes, bouncy ball, heavy bowling ball,
///               friction contrast strips, a motor-driven spinning blade, and a soft-weld
///               tower that shatters on impact.
/// Centre      — pendulum chain: boxes linked by revolute joints, plus a spring-distance
///               pendulum, demonstrating joints and angular momentum.
/// Right zone  — kinematic character controller: WASD/arrow movement, jump, one-way platform,
///               horizontally moving platform, and a sensor trigger zone.
///
/// L-Click: spawn random shape   R-Click: fire cannonball from the left wall
/// D: Box2D debug overlay       G: flip gravity      R: reset      ESC: menu
/// </summary>
public class PhysicsDemoScene : DemoSceneBase
{
    private readonly PhysicsWorld _physics;

    private Box2DDebugDrawSystem? _debugDraw;
    private bool _gravityFlipped;
    private PlayerInputBehavior? _playerInput;
    private int _spawnCount;

    private Entity? _playerEntity;
    private Entity? _triggerZoneEntity;
    private int _triggerOccupants;

    private static readonly Color ColBackground = new(22, 22, 35);
    private static readonly Color ColWall = new(70, 70, 90);
    private static readonly Color ColPlatform = new(80, 110, 160);
    private static readonly Color ColBox = new(200, 160, 70);
    private static readonly Color ColBall = new(220, 75, 75);
    private static readonly Color ColBowling = new(60, 100, 210);
    private static readonly Color ColBlade = new(210, 210, 50);
    private static readonly Color ColPendulum = new(170, 110, 220);
    private static readonly Color ColPlayer = new(70, 200, 110);
    private static readonly Color ColMovingPlat = new(50, 180, 180);
    private static readonly Color ColTriggerOff = new(80, 40, 40, 100);
    private static readonly Color ColTriggerOn = new(220, 60, 60, 180);
    private static readonly Color ColWeld = new(240, 115, 35);

    private static readonly Color[] SpawnPalette =
    [
        new(220,  80,  80), new(80, 180, 220), new(80, 220, 120),
        new(220, 180,  50), new(180,  80, 220), new(220, 130,  50)
    ];

    public PhysicsDemoScene(
        IInputContext input,
        ISceneManager sceneManager,
        IGameContext gameContext,
        PhysicsWorld physics,
        PerformanceOverlay? perfOverlay = null)
        : base(input, sceneManager, gameContext, perfOverlay)
    {
        _physics = physics;
    }

    protected override void OnEnter()
    {
        World.GetSystem<DebugRenderer>()!.IsEnabled = false;

        World.AddSystem<ShapeRenderSystem>();
        World.AddSystem<Box2DPhysicsSystem>();
        World.AddSystem<PrePhysicsKinematicCharacterSystem>();
        World.AddSystem<PostPhysicsKinematicCharacterSystem>();
        World.AddSystem<Box2DDebugDrawSystem>(s => s.IsEnabled = false);
        _debugDraw = World.GetRenderSystem<Box2DDebugDrawSystem>();

        BuildArena();
        BuildSandbox();
        BuildPendulumChain();
        BuildKinematicZone();
    }
    
    protected override void OnUpdate(GameTime time)
    {
        if (CheckReturnToMenu()) return;
        HandlePerformanceHotkeys();

        if (Input.IsKeyPressed(Key.D) && _debugDraw != null)
            _debugDraw.IsEnabled = !_debugDraw.IsEnabled;

        if (Input.IsKeyPressed(Key.G))
            FlipGravity();

        if (Input.IsKeyPressed(Key.R))
            ResetScene();

        if (Input.IsMouseButtonPressed(MouseButton.Left))
            SpawnRandomShape(Input.MousePosition);

        if (Input.IsMouseButtonPressed(MouseButton.Right))
            LaunchCannonball(Input.MousePosition);
    }

    protected override void OnRender(GameTime time)
    {
        Renderer.ClearColor = ColBackground;

        Renderer.DrawText("[ Sandbox ]", 30, 12, Color.White);
        Renderer.DrawText("[ Pendulum Chain ]", 445, 12, Color.White);
        Renderer.DrawText("[ Kinematic Character ]", 750, 12, Color.White);

        Renderer.DrawText(
            "L-Click spawn  R-Click cannonball  G gravity  D debug  R reset  ESC menu",
            20, 696, new Color(150, 150, 150));

        Renderer.DrawText(
            _gravityFlipped ? "Gravity: UP ▲" : "Gravity: DOWN ▼",
            1060, 12, _gravityFlipped ? new Color(100, 210, 255) : new Color(200, 175, 70));

        Renderer.DrawText("WASD / Arrows + Space", 760, 676, new Color(150, 150, 150));

        Renderer.DrawText("Trigger", 1070, 580, new Color(220, 100, 100));

        Renderer.DrawText("one-way", 868, 503, new Color(100, 210, 110));

        Renderer.DrawText("Shoot", 286, 547, new Color(220, 130, 60));

        RenderPerformanceOverlay();
    }

    private void BuildArena()
    {
        CreateStaticBox("Floor", 640, 690, 1280, 20, ColWall);
        CreateStaticBox("Ceiling", 640, 5, 1280, 20, ColWall);
        CreateStaticBox("WallLeft", 10, 345, 20, 690, ColWall);
        CreateStaticBox("WallRight", 1270, 345, 20, 690, ColWall);
        CreateStaticBox("Div1", 425, 380, 10, 640, ColWall);
        CreateStaticBox("Div2", 735, 380, 10, 640, ColWall);
    }

    private void BuildSandbox()
    {
        // Angled ramp — left side
        CreateStaticBox("Ramp", 100, 560, 200, 14, ColPlatform, rotation: -0.38f, friction: 0.4f);

        // Friction contrast strips
        CreateStaticBox("FrictionLow",  150, 628, 160, 14, new Color(55, 75, 130), friction: 0.02f);
        CreateStaticBox("FrictionHigh", 295, 628, 100, 14, new Color(120, 85, 35), friction: 0.95f);

        BuildPyramid();
        BuildBouncyBall();
        BuildBowlingBall();
        BuildSpinningBlade();
        BuildWeldTower();
    }

    private void BuildPyramid()
    {
        const float sz = 34f;
        const float baseY = 668f - sz * 0.5f;
        const int rows = 4;

        for (int row = 0; row < rows; row++)
        {
            int cols = rows - row;
            float startX = 175f - cols * sz * 0.5f;
            float y = baseY - row * sz;
            for (int col = 0; col < cols; col++)
            {
                float t = row / (float)(rows - 1);
                var color = Color.Lerp(ColBox, ColBall, t);
                World.CreateEntity($"PyBox_{row}_{col}")
                    .AddComponent<TransformComponent>(e => e.Position = new Vector2(startX + col * sz, y))
                    .AddComponent<RectangleShapeComponent>(s =>
                    {
                        s.Width = sz - 1;
                        s.Height = sz - 1;
                        s.FillColor = color;
                        s.OutlineColor = new Color(255, 255, 255, 60);
                    })
                    .AddComponent<PhysicsBodyComponent>(b =>
                    {
                        b.Shape = new BoxShape(sz - 1, sz - 1);
                        b.Mass = 1f;
                        b.SurfaceFriction = 0.5f;
                        b.Restitution = 0.1f;
                    });
            }
        }
    }

    private void BuildBouncyBall()
    {
        World.CreateEntity("BouncyBall")
            .AddComponent<TransformComponent>(t => t.Position = new Vector2(50, 220))
            .AddComponent<CircleShapeComponent>(s =>
            {
                s.Radius = 20;
                s.FillColor = ColBall;
                s.OutlineColor = Color.White;
            })
            .AddComponent<PhysicsBodyComponent>(b =>
            {
                b.Shape = new CircleShape(20);
                b.Mass = 1.5f;
                b.Restitution = 0.88f;
                b.SurfaceFriction = 0.1f;
                b.InitialLinearVelocity = new Vector2(180, 0);
            });
    }

    private void BuildBowlingBall()
    {
        World.CreateEntity("BowlingBall")
            .AddComponent<TransformComponent>(t => t.Position = new Vector2(350, 120))
            .AddComponent<CircleShapeComponent>(s =>
            {
                s.Radius = 28;
                s.FillColor = ColBowling;
                s.OutlineColor = Color.White;
                s.OutlineThickness = 2;
            })
            .AddComponent<PhysicsBodyComponent>(b =>
            {
                b.Shape = new CircleShape(28);
                b.Mass = 8f;
                b.Restitution = 0.04f;
                b.SurfaceFriction = 0.8f;
            });
    }

    private void BuildSpinningBlade()
    {
        var pin = World.CreateEntity("BladePin")
            .AddComponent<TransformComponent>(t => t.Position = new Vector2(355, 430))
            .AddComponent<CircleShapeComponent>(s =>
            {
                s.Radius = 6;
                s.FillColor = ColWall;
                s.OutlineColor = Color.White;
            })
            .AddComponent<PhysicsBodyComponent>(b =>
            {
                b.Shape = new CircleShape(6);
                b.BodyType = PhysicsBodyType.Static;
            });

        World.CreateEntity("Blade")
            .AddComponent<TransformComponent>(t => t.Position = new Vector2(355, 430))
            .AddComponent<RectangleShapeComponent>(s =>
            {
                s.Width = 110;
                s.Height = 13;
                s.FillColor = ColBlade;
                s.OutlineColor = Color.White;
            })
            .AddComponent<PhysicsBodyComponent>(b =>
            {
                b.Shape = new BoxShape(110, 13);
                b.Mass = 2f;
                b.SurfaceFriction = 0.1f;
                b.Restitution = 0.3f;
            })
            .AddComponent<RevoluteJointComponent>(j =>
            {
                j.ConnectedBody = pin.GetComponent<PhysicsBodyComponent>()!;
                j.EnableMotor = true;
                j.MotorSpeed = 4f;
                j.MaxMotorTorque = 5000f;
            });
    }

    private void BuildWeldTower()
    {
        float[] ys = [642f, 604f, 566f];
        PhysicsBodyComponent? prev = null;

        for (int i = 0; i < ys.Length; i++)
        {
            var e = World.CreateEntity($"WeldBox_{i}")
                .AddComponent<TransformComponent>(t => t.Position = new Vector2(340, ys[i]))
                .AddComponent<RectangleShapeComponent>(s =>
                {
                    s.Width = 34;
                    s.Height = 34;
                    s.FillColor = ColWeld;
                    s.OutlineColor = Color.White;
                    s.OutlineThickness = 2;
                })
                .AddComponent<PhysicsBodyComponent>(b =>
                {
                    b.Shape = new BoxShape(34, 34);
                    b.Mass = 1.5f;
                    b.SurfaceFriction = 0.4f;
                });

            var body = e.GetComponent<PhysicsBodyComponent>()!;

            if (prev != null)
            {
                e.AddComponent<WeldJointComponent>(j =>
                {
                    j.ConnectedBody = prev;
                    j.LinearHertz = 8f;
                    j.LinearDampingRatio = 0.5f;
                    j.AngularHertz = 8f;
                    j.AngularDampingRatio = 0.5f;
                    j.BreakForce = 4000f;
                });
            }

            prev = body;
        }
    }

    private void BuildPendulumChain()
    {
        const int links = 8;
        const float linkW = 28f;
        const float linkH = 18f;
        const float gap = 4f;
        const float pivotX = 560f;
        const float pivotY = 55f;

        var anchor = World.CreateEntity("PendulumAnchor")
            .AddComponent<TransformComponent>(t => t.Position = new Vector2(pivotX, pivotY))
            .AddComponent<CircleShapeComponent>(s =>
            {
                s.Radius = 8;
                s.FillColor = ColWall;
                s.OutlineColor = Color.White;
            })
            .AddComponent<PhysicsBodyComponent>(b =>
            {
                b.Shape = new CircleShape(8);
                b.BodyType = PhysicsBodyType.Static;
            });

        PhysicsBodyComponent prevBody = anchor.GetComponent<PhysicsBodyComponent>()!;
        float prevCY = pivotY;

        for (int i = 0; i < links; i++)
        {
            float linkCY = prevCY + linkH + gap;
            var color = Color.Lerp(ColPendulum, ColBall, i / (float)(links - 1));

            var link = World.CreateEntity($"PLink_{i}")
                .AddComponent<TransformComponent>(t => t.Position = new Vector2(pivotX, linkCY))
                .AddComponent<RectangleShapeComponent>(s =>
                {
                    s.Width = linkW;
                    s.Height = linkH;
                    s.FillColor = color;
                    s.OutlineColor = new Color(255, 255, 255, 70);
                })
                .AddComponent<PhysicsBodyComponent>(b =>
                {
                    b.Shape = new BoxShape(linkW, linkH);
                    b.Mass = 0.8f;
                    b.LinearDamping = 0.02f;
                    b.Restitution = 0.1f;
                })
                .AddComponent<RevoluteJointComponent>(j =>
                {
                    j.ConnectedBody = prevBody;
                    j.LocalAnchorA = new Vector2(0, -linkH * 0.5f - gap * 0.5f);
                    j.LocalAnchorB = i == 0
                        ? Vector2.Zero
                        : new Vector2(0, linkH * 0.5f + gap * 0.5f);
                });

            prevBody = link.GetComponent<PhysicsBodyComponent>()!;
            prevCY = linkCY;
        }

        // Heavy bob at the end — sideways push starts the swing
        World.CreateEntity("PendulumBob")
            .AddComponent<TransformComponent>(t => t.Position = new Vector2(pivotX, prevCY + 22))
            .AddComponent<CircleShapeComponent>(s =>
            {
                s.Radius = 22;
                s.FillColor = ColBowling;
                s.OutlineColor = Color.White;
                s.OutlineThickness = 2;
            })
            .AddComponent<PhysicsBodyComponent>(b =>
            {
                b.Shape = new CircleShape(22);
                b.Mass = 5f;
                b.Restitution = 0.4f;
                b.LinearDamping = 0.01f;
                b.InitialLinearVelocity = new Vector2(270, 0);
            })
            .AddComponent<RevoluteJointComponent>(j =>
            {
                j.ConnectedBody = prevBody;
                j.LocalAnchorA = new Vector2(0, -22);
                j.LocalAnchorB = new Vector2(0, linkH * 0.5f + gap * 0.5f);
            });

        // Offset spring-distance pendulum for interference patterns
        BuildSpringPendulum(pivotX + 65, pivotY);
    }

    private void BuildSpringPendulum(float x, float pivotY)
    {
        var anchor = World.CreateEntity("SpringAnchor")
            .AddComponent<TransformComponent>(t => t.Position = new Vector2(x, pivotY))
            .AddComponent<CircleShapeComponent>(s =>
            {
                s.Radius = 6;
                s.FillColor = ColWall;
                s.OutlineColor = Color.White;
            })
            .AddComponent<PhysicsBodyComponent>(b =>
            {
                b.Shape = new CircleShape(6);
                b.BodyType = PhysicsBodyType.Static;
            });

        var rope = World.CreateEntity("SpringRope")
            .AddComponent<TransformComponent>(t => t.Position = new Vector2(x, pivotY))
            .AddComponent<LineShapeComponent>(l =>
            {
                l.FillColor = new Color(160, 160, 160, 180);
                l.OutlineThickness = 1.5f;
            });

        var bob = World.CreateEntity("SpringBob")
            .AddComponent<TransformComponent>(t => t.Position = new Vector2(x, pivotY + 130))
            .AddComponent<CircleShapeComponent>(s =>
            {
                s.Radius = 18;
                s.FillColor = ColBlade;
                s.OutlineColor = Color.White;
            })
            .AddComponent<PhysicsBodyComponent>(b =>
            {
                b.Shape = new CircleShape(18);
                b.Mass = 2f;
                b.Restitution = 0.5f;
                b.LinearDamping = 0.01f;
                b.InitialLinearVelocity = new Vector2(-190, 0);
            })
            .AddComponent<DistanceJointComponent>(j =>
            {
                j.ConnectedBody = anchor.GetComponent<PhysicsBodyComponent>()!;
                j.Length = 130f;
                j.EnableSpring = true;
                j.Hertz = 1.5f;
                j.DampingRatio = 0.1f;
            });

        rope.AddBehavior<RopeLineBehavior>(b =>
        {
            b.Anchor = anchor;
            b.Target = bob;
        });
    }

    private void BuildKinematicZone()
    {
        // Ground floor for this zone
        CreateStaticBox("CharFloor", 1000, 668, 515, 20, ColPlatform);

        // Stepped ledges to climb
        CreateStaticBox("Step1", 800, 620, 90, 14, ColPlatform);
        CreateStaticBox("Step2", 860, 560, 100, 14, ColPlatform);

        // One-way platform — jump up through it, land on top
        World.CreateEntity("OneWayPlatform")
            .AddComponent<TransformComponent>(t => t.Position = new Vector2(940, 510))
            .AddComponent<RectangleShapeComponent>(s =>
            {
                s.Width = 180;
                s.Height = 14;
                s.FillColor = new Color(90, 195, 110);
                s.OutlineColor = Color.White;
                s.OutlineThickness = 2;
            })
            .AddComponent<PhysicsBodyComponent>(b =>
            {
                b.Shape = new BoxShape(180, 14);
                b.BodyType = PhysicsBodyType.Static;
                b.SurfaceFriction = 0.5f;
                b.IsOneWayPlatform = true;
                b.PlatformNormalDirection = new Vector2(0, -1);
            });

        // Static upper ledge
        CreateStaticBox("UpperLedge", 1130, 420, 220, 14, ColPlatform);

        // Moving platform — kinematic, horizontal oscillation
        World.CreateEntity("MovingPlatform")
            .AddComponent<TransformComponent>(t => t.Position = new Vector2(840, 380))
            .AddComponent<RectangleShapeComponent>(s =>
            {
                s.Width = 140;
                s.Height = 14;
                s.FillColor = ColMovingPlat;
                s.OutlineColor = Color.White;
                s.OutlineThickness = 2;
            })
            .AddComponent<PhysicsBodyComponent>(b =>
            {
                b.Shape = new BoxShape(140, 14);
                b.BodyType = PhysicsBodyType.Kinematic;
                b.SurfaceFriction = 0.9f;
            })
            .AddBehavior<MovingPlatformBehavior>(p =>
            {
                p.Origin = new Vector2(840, 380);
                p.Amplitude = 110f;
                p.Speed = 1.4f;
            });

        // Trigger zone — tints red when any body enters
        _triggerZoneEntity = World.CreateEntity("TriggerZone")
            .AddComponent<TransformComponent>(t => t.Position = new Vector2(1110, 608))
            .AddComponent<RectangleShapeComponent>(s =>
            {
                s.Width = 120;
                s.Height = 80;
                s.FillColor = ColTriggerOff;
                s.OutlineColor = new Color(200, 60, 60);
                s.OutlineThickness = 2;
            })
            .AddComponent<PhysicsBodyComponent>(b =>
            {
                b.Shape = new BoxShape(120, 80);
                b.BodyType = PhysicsBodyType.Static;
                b.IsTrigger = true;
                b.OnTriggerEnter += _ =>
                {
                    _triggerOccupants++;
                    SetTriggerColor(ColTriggerOn);
                };
                b.OnTriggerExit += _ =>
                {
                    if (--_triggerOccupants <= 0)
                    {
                        _triggerOccupants = 0;
                        SetTriggerColor(ColTriggerOff);
                    }
                };
            });

        BuildPlayer();
    }

    private void BuildPlayer()
    {
        _playerEntity = World.CreateEntity("Player")
            .AddComponent<TransformComponent>(t => t.Position = new Vector2(800, 590))
            .AddComponent<RectangleShapeComponent>(s =>
            {
                s.Width = 26;
                s.Height = 46;
                s.FillColor = ColPlayer;
                s.OutlineColor = Color.White;
                s.OutlineThickness = 2;
            })
            .AddComponent<PhysicsBodyComponent>(b =>
            {
                b.Shape = new CapsuleShape(new Vector2(0, -10), new Vector2(0, 10), 13f);
                b.BodyType = PhysicsBodyType.Kinematic;
                b.CollisionMask = ulong.MaxValue;
                b.FixedRotation = true;
            })
            .AddComponent<KinematicCharacterBody>(c =>
            {
                c.FloorAngleLimit = 0.75f;
                c.SnapDistance = 10f;
                c.MaxSpeed = 500f;
                c.MaxSlides = 6;
                c.EnableDebugLogging = true;
            })
            .AddBehavior<PlayerInputBehavior>(b => _playerInput = b);
    }
    
    private void SpawnRandomShape(Vector2 pos)
    {
        int idx = _spawnCount % SpawnPalette.Length;
        var color = SpawnPalette[idx];
        int kind = _spawnCount % 3;
        _spawnCount++;

        switch (kind)
        {
            case 0:
                {
                    float r = 10 + _spawnCount % 13;
                    World.CreateEntity($"SC_{_spawnCount}")
                        .AddComponent<TransformComponent>(t => t.Position = pos)
                        .AddComponent<CircleShapeComponent>(s =>
                        {
                            s.Radius = r;
                            s.FillColor = color;
                            s.OutlineColor = Color.White;
                        })
                        .AddComponent<PhysicsBodyComponent>(b =>
                        {
                            b.Shape = new CircleShape(r);
                            b.Mass = r * 0.08f;
                            b.Restitution = 0.45f;
                            b.SurfaceFriction = 0.5f;
                        });
                    break;
                }
            case 1:
                {
                    float sz = 18 + _spawnCount % 20;
                    World.CreateEntity($"SB_{_spawnCount}")
                        .AddComponent<TransformComponent>(t => t.Position = pos)
                        .AddComponent<RectangleShapeComponent>(s =>
                        {
                            s.Width = sz;
                            s.Height = sz;
                            s.FillColor = color;
                            s.OutlineColor = Color.White;
                        })
                        .AddComponent<PhysicsBodyComponent>(b =>
                        {
                            b.Shape = new BoxShape(sz, sz);
                            b.Mass = 1f;
                            b.Restitution = 0.2f;
                            b.SurfaceFriction = 0.6f;
                        });
                    break;
                }
            default:
                {
                    float r = 10 + _spawnCount % 10;
                    float h = r * 1.8f;
                    World.CreateEntity($"SK_{_spawnCount}")
                        .AddComponent<TransformComponent>(t => t.Position = pos)
                        .AddComponent<CircleShapeComponent>(s =>
                        {
                            s.Radius = r;
                            s.FillColor = color;
                            s.OutlineColor = Color.White;
                        })
                        .AddComponent<PhysicsBodyComponent>(b =>
                        {
                            b.Shape = new CapsuleShape(new Vector2(0, -h * 0.4f), new Vector2(0, h * 0.4f), r);
                            b.Mass = 0.9f;
                            b.Restitution = 0.35f;
                            b.SurfaceFriction = 0.4f;
                        });
                    break;
                }
        }
    }

    private void LaunchCannonball(Vector2 target)
    {
        var origin = new Vector2(28, 345);
        var dir = Vector2.Normalize(target - origin);
        _spawnCount++;

        World.CreateEntity($"CB_{_spawnCount}")
            .AddComponent<TransformComponent>(t => t.Position = origin)
            .AddComponent<CircleShapeComponent>(s =>
            {
                s.Radius = 14;
                s.FillColor = new Color(45, 45, 55);
                s.OutlineColor = new Color(200, 200, 200);
                s.OutlineThickness = 2;
            })
            .AddComponent<PhysicsBodyComponent>(b =>
            {
                b.Shape = new CircleShape(14);
                b.Mass = 6f;
                b.Restitution = 0.2f;
                b.SurfaceFriction = 0.3f;
                b.IsBullet = true;
                b.InitialLinearVelocity = dir * 1100f;
            });
    }

    private void FlipGravity()
    {
        _gravityFlipped = !_gravityFlipped;
        if (_playerInput != null)
        {
            _playerInput.GravityDirection = _gravityFlipped ? -1f : 1f;
            _playerInput.ResetVelocity();
        }
        _physics.SetGravity(new Vector2(0, _gravityFlipped ? -980f : 980f));
    }

    private void ResetScene()
    {
        _gravityFlipped = false;
        _playerInput = null;
        _spawnCount = 0;
        _physics.SetGravity(new Vector2(0, 980f));

        World.ClearEntities();
        BuildArena();
        BuildSandbox();
        BuildPendulumChain();
        BuildKinematicZone();

        _debugDraw = World.GetRenderSystem<Box2DDebugDrawSystem>();
    }

    private void SetTriggerColor(Color color)
    {
        var shape = _triggerZoneEntity?.GetComponent<RectangleShapeComponent>();
        if (shape != null) shape.FillColor = color;
    }

    private void CreateStaticBox(string name, float cx, float cy, float w, float h, Color color,
        float friction = 0.5f, float restitution = 0.05f, float rotation = 0f)
    {
        World.CreateEntity(name)
            .AddComponent<TransformComponent>(t =>
            {
                t.Position = new Vector2(cx, cy);
                t.Rotation = rotation;
            })
            .AddComponent<RectangleShapeComponent>(s =>
            {
                s.Width = w;
                s.Height = h;
                s.FillColor = color;
                s.OutlineColor = new Color(255, 255, 255, 35);
            })
            .AddComponent<PhysicsBodyComponent>(b =>
            {
                b.Shape = new BoxShape(w, h);
                b.BodyType = PhysicsBodyType.Static;
                b.SurfaceFriction = friction;
                b.Restitution = restitution;
            });
    }
}