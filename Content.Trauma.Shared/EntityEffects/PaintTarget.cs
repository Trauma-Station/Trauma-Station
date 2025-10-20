using Content.Shared.EntityEffects;
using Content.Trauma.Shared.Paint;
using Content.Trauma.Shared.Tools;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Paints the target entity.
/// Requires <see cref="EntityEffectToolArgs"/> to work and the tool must have <see cref="PaintCanComponent"/>.
/// </summary>
public sealed partial class PaintTarget : EntityEffect
{
    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-paint-target-guidebook-text");

    public override void Effect(EntityEffectBaseArgs baseArgs)
    {
        if (baseArgs is not EntityEffectToolArgs args)
            return;

        var paint = args.EntityManager.System<PaintSystem>();
        args.Handled = paint.TryPaint(args.Tool, args.TargetEntity);
    }

    public override bool ShouldLog => true;
}
