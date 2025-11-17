using Content.Shared.EntityEffects;
using Content.Trauma.Shared.Paint;
using Content.Trauma.Shared.Tools;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Paints the target entity.
/// Requires <see cref="EntityEffectToolComponent"/> to work and the tool must have <see cref="PaintCanComponent"/>.
/// </summary>
public sealed partial class PaintTarget : EntityEffectBase<PaintTarget>
{
    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-paint-target-guidebook-text", ("chance", Probability));
}

public sealed class PaintTargetEffectSystem : EntityEffectSystem<TransformComponent, PaintTarget>
{
    [Dependency] private readonly EffectDataSystem _data = default!;
    [Dependency] private readonly EffectsToolSystem _tool = default!;
    [Dependency] private readonly PaintSystem _paint = default!;

    protected override void Effect(Entity<TransformComponent> ent, ref EntityEffectEvent<PaintTarget> args)
    {
        if (_data.GetTool(ent) is not {} tool)
            return;

        if (_paint.TryPaint(tool, ent))
            _tool.MarkUsed(tool);
    }
}
