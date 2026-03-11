// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Goobstation.Heretic.Systems;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared.EntityEffects.Effects;

public sealed partial class VoidCurse : EntityEffectBase<VoidCurse>
{
    [DataField]
    public int Stacks = 1;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => "Inflicts void curse.";
}

public sealed class VoidCurseEffectSystem : EntityEffectSystem<TransformComponent, VoidCurse>
{
    [Dependency] private readonly SharedVoidCurseSystem _voidCurse = default!;

    protected override void Effect(Entity<TransformComponent> ent, ref EntityEffectEvent<VoidCurse> args)
    {
        _voidCurse.DoCurse(ent, args.Effect.Stacks);
    }
}
