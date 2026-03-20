using System.Numerics;
using Brine2D.Core;
using Brine2D.ECS;
using Brine2D.ECS.Systems;
using Brine2D.Engine;
using Brine2D.Input;
using Brine2D.Performance;
using Brine2D.Rendering;
using Microsoft.Extensions.Logging;

namespace FeatureDemos.Scenes.ECS;

/// <summary>
/// Demo showing the difference between FixedUpdate (constant timestep) and Update (variable timestep).
/// Bouncing balls on the left use FixedUpdate physics; those on the right use variable-rate Update.
/// Hold SPACE to inject lag spikes — the left side catches up smoothly in small steps while the
/// right side teleports in one large jump, visible via position trails.
/// </summary>
public class FixedUpdateDemoScene : DemoSceneBase
{
    private const float BallRadius = 10f;
    private const float BoundsTop = 120f;
    private const float BoundsBottom = 620f;
    private const float LeftColumnX = 80f;
    private const float RightColumnX = 660f;
    private const float ColumnWidth = 480f;
    private const int BallCount = 5;
    private const int TrailLength = 40;
    private const int SpikeMs = 200;
    private const float TrailIntervalSec = 1f / 120f;

    private readonly IInputContext _input;
    private readonly List<BallState> _fixedBalls = new();
    private readonly List<BallState> _variableBalls = new();
    private long _fixedStepCount;
    private long _frameCount;
    private bool _spikeActive;

    public FixedUpdateDemoScene(
        IInputContext input,
        ISceneManager sceneManager,
        IGameContext gameContext,
        PerformanceOverlay? perfOverlay = null)
        : base(input, sceneManager, gameContext, perfOverlay)
    {
        _input = input;
    }

    protected override Task OnLoadAsync(CancellationToken cancellationToken, IProgress<float>? progress = null)
    {
        Logger.LogInformation("=== Fixed Update Demo ===");
        Logger.LogInformation("Left: FixedUpdate (deterministic)  |  Right: Update (variable-rate)");
        Logger.LogInformation("Hold SPACE for {SpikeMs}ms lag spike — watch the trails", SpikeMs);

        Renderer.ClearColor = new Color(15, 20, 35);

        World.AddSystem<BouncingBallFixedSystem>();

        var random = new Random(42);
        for (int i = 0; i < BallCount; i++)
        {
            var speed = 500f + random.Next(400);
            var y = BoundsTop + 40 + (i * 90);

            var fixedBall = new BallState(
                new Vector2(LeftColumnX + 40 + random.Next((int)ColumnWidth - 80), y),
                new Vector2(speed * (random.Next(2) == 0 ? 1 : -1), speed * 0.8f),
                new Color(80 + random.Next(175), 80 + random.Next(175), 200 + random.Next(55)));

            var variableBall = new BallState(
                fixedBall.Position with { X = RightColumnX + (fixedBall.Position.X - LeftColumnX) },
                fixedBall.Velocity,
                fixedBall.Color);

            _fixedBalls.Add(fixedBall);
            _variableBalls.Add(variableBall);
        }

        return Task.CompletedTask;
    }

    protected override void OnEnter()
    {
        var system = World.GetFixedUpdateSystem<BouncingBallFixedSystem>();
        if (system != null)
            system.Balls = _fixedBalls;
    }

    protected override void OnFixedUpdate(GameTime fixedTime)
    {
        _fixedStepCount++;
    }

    protected override void OnUpdate(GameTime gameTime)
    {
        if (CheckReturnToMenu()) return;
        HandlePerformanceHotkeys();

        _frameCount++;
        _spikeActive = _input.IsKeyDown(Key.Space);

        if (_spikeActive)
            Thread.Sleep(SpikeMs);

        var dt = (float)gameTime.DeltaTime;

        foreach (var ball in _variableBalls)
            BounceBall(ball, dt, RightColumnX, RightColumnX + ColumnWidth);
    }

    protected override void OnRender(GameTime gameTime)
    {
        Renderer.DrawText("FIXED UPDATE DEMO", 480, 15, new Color(100, 200, 255));
        Renderer.DrawText(
            "Left: FixedUpdate (deterministic)  |  Right: Update (variable-rate)",
            280, 42, new Color(160, 160, 160));
        Renderer.DrawText(
            "Hold SPACE for lag spike — watch the trails  |  ESC to return",
            310, 62, new Color(120, 120, 120));

        if (_spikeActive)
        {
            Renderer.DrawRectangleFilled(480, 82, 320, 26, new Color(180, 40, 40, 200));
            Renderer.DrawText($"  LAG SPIKE ACTIVE ({SpikeMs}ms)", 490, 85, new Color(255, 255, 100));
        }

        DrawColumnBox("FixedUpdate (many small steps)", LeftColumnX, ColumnWidth);
        DrawColumnBox("Update (one large step)", RightColumnX, ColumnWidth);

        DrawBalls(_fixedBalls);
        DrawBalls(_variableBalls);

        var statsY = BoundsBottom + 30;
        Renderer.DrawText($"Fixed steps: {_fixedStepCount}", LeftColumnX, statsY, new Color(180, 255, 180));
        Renderer.DrawText($"Frames: {_frameCount}", RightColumnX, statsY, new Color(255, 180, 180));
        Renderer.DrawText(
            "During a spike the left trails stay evenly spaced; the right trails show a gap then a big jump.",
            80, statsY + 30, new Color(100, 100, 100));

        RenderPerformanceOverlay();
    }

    private void DrawColumnBox(string label, float x, float width)
    {
        Renderer.DrawRectangleOutline(
            x - 10, BoundsTop - 10, width + 20, BoundsBottom - BoundsTop + 20,
            new Color(60, 80, 100), 2f);
        Renderer.DrawText(label, x, BoundsTop - 30, new Color(200, 200, 200));
    }

    private void DrawBalls(List<BallState> balls)
    {
        foreach (var ball in balls)
        {
            var trail = ball.Trail;
            for (int t = 0; t < trail.Count; t++)
            {
                var alpha = (byte)(30 + (t * 180 / TrailLength));
                var trailColor = new Color(ball.Color.R, ball.Color.G, ball.Color.B, alpha);
                var size = 2f + (t * 4f / TrailLength);
                var half = size / 2f;
                Renderer.DrawRectangleFilled(trail[t].X - half, trail[t].Y - half, size, size, trailColor);
            }

            Renderer.DrawCircleFilled(ball.Position.X, ball.Position.Y, BallRadius, ball.Color);
        }
    }

    internal static void BounceBall(BallState ball, float dt, float minX, float maxX)
    {
        ball.Position += ball.Velocity * dt;

        if (ball.Position.Y - BallRadius < BoundsTop)
        {
            ball.Position = ball.Position with { Y = BoundsTop + BallRadius };
            ball.Velocity = ball.Velocity with { Y = MathF.Abs(ball.Velocity.Y) };
        }
        else if (ball.Position.Y + BallRadius > BoundsBottom)
        {
            ball.Position = ball.Position with { Y = BoundsBottom - BallRadius };
            ball.Velocity = ball.Velocity with { Y = -MathF.Abs(ball.Velocity.Y) };
        }

        if (ball.Position.X - BallRadius < minX)
        {
            ball.Position = ball.Position with { X = minX + BallRadius };
            ball.Velocity = ball.Velocity with { X = MathF.Abs(ball.Velocity.X) };
        }
        else if (ball.Position.X + BallRadius > maxX)
        {
            ball.Position = ball.Position with { X = maxX - BallRadius };
            ball.Velocity = ball.Velocity with { X = -MathF.Abs(ball.Velocity.X) };
        }

        ball.TrailTimer += dt;
        if (ball.TrailTimer >= TrailIntervalSec)
        {
            ball.TrailTimer = 0f;
            ball.Trail.Add(ball.Position);
            if (ball.Trail.Count > TrailLength)
                ball.Trail.RemoveAt(0);
        }
    }

    internal class BallState
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public Color Color;
        public float TrailTimer;
        public readonly List<Vector2> Trail = new(TrailLength + 1);

        public BallState(Vector2 position, Vector2 velocity, Color color)
        {
            Position = position;
            Velocity = velocity;
            Color = color;
        }
    }

    private class BouncingBallFixedSystem : FixedUpdateSystemBase
    {
        public List<BallState>? Balls { get; set; }

        public override void FixedUpdate(IEntityWorld world, GameTime fixedTime)
        {
            if (Balls == null) return;

            var dt = (float)fixedTime.DeltaTime;
            foreach (var ball in Balls)
                BounceBall(ball, dt, LeftColumnX, LeftColumnX + ColumnWidth);
        }
    }
}