// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Trauma.Shared.Paint;
using Content.Trauma.Shared.Tools;

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

public sealed partial class PaintTargetEffectSystem : EntityEffectSystem<TransformComponent, PaintTarget>
{
    [Dependency] private EffectDataSystem _data = default!;
    [Dependency] private EffectsToolSystem _tool = default!;
    [Dependency] private PaintSystem _paint = default!;

    protected override void Effect(Entity<TransformComponent> ent, ref EntityEffectEvent<PaintTarget> args)
    {
        if (_data.GetTool(ent) is not {} tool)
            return;

        if (_paint.TryPaint(tool, ent))
            _tool.MarkUsed(tool);
    }
}
