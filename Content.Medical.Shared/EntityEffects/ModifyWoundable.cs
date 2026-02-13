// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Medical.Shared.Wounds;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Medical.Shared.EntityEffects;

/// <summary>
/// Modifies some fields of the target's <see cref="WoundableComponent"/>.
/// </summary>
public sealed partial class ModifyWoundable : EntityEffectBase<ModifyWoundable>
{
    [DataField]
    public bool CanRemove;

    [DataField]
    public bool CanBleed;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null; // not used by reagents idc
}

public sealed class ModifyWoundableEffectSystem : EntityEffectSystem<WoundableComponent, ModifyWoundable>
{
    protected override void Effect(Entity<WoundableComponent> ent, ref EntityEffectEvent<ModifyWoundable> args)
    {
        var effect = args.Effect;
        ent.Comp.CanRemove = effect.CanRemove;
        ent.Comp.CanBleed = effect.CanBleed;
        Dirty(ent);
    }
}
