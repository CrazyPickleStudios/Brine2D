using Brine2D.Core;
using Brine2D.ECS;
using Brine2D.ECS.Components;
using Brine2D.Systems.Rendering;

namespace FeatureDemos.Scenes.Physics;

/// <summary>
/// Updates a <see cref="LineShapeComponent"/> each frame so it stretches from
/// an anchor entity's position to a target entity's position.
/// </summary>
public class RopeLineBehavior : Behavior
{
    private TransformComponent _ownTransform = null!;
    private LineShapeComponent _line = null!;

    public Entity? Anchor { get; set; }
    public Entity? Target { get; set; }

    protected override void OnAttached()
    {
        _ownTransform = Entity.GetRequiredComponent<TransformComponent>();
        _line = Entity.GetRequiredComponent<LineShapeComponent>();
    }

    public override void Update(GameTime gameTime)
    {
        var anchorPos = Anchor?.GetComponent<TransformComponent>()?.Position ?? _ownTransform.Position;
        if (Target?.GetComponent<TransformComponent>() is not { } targetTransform) return;

        _ownTransform.Position = anchorPos;
        _line.End = targetTransform.Position - anchorPos;
    }
}