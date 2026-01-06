// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityEffects.Effects;

/// <summary>
/// Unbuckles everything buckled to the target entity.
/// </summary>
public sealed partial class UnbuckleStrapped : EntityEffectBase<UnbuckleStrapped>
{
    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null; // its just used for crucifying idc
}

public sealed class UnbuckleStrappedEffectSystem : EntityEffectSystem<StrapComponent, UnbuckleStrapped>
{
    [Dependency] private readonly SharedBuckleSystem _buckle = default!;

    protected override void Effect(Entity<StrapComponent> ent, ref EntityEffectEvent<UnbuckleStrapped> args)
    {
        _buckle.StrapRemoveAll(ent, ent.Comp);
    }
}
