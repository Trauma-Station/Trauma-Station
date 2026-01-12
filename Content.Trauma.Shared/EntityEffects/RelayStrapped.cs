// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Buckle.Components;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Applies effects to the entity strapped onto this one.
/// </summary>
public sealed partial class RelayStrapped : EntityEffectBase<RelayStrapped>
{
    /// <summary>
    /// Effects to apply to the strapped entity.
    /// </summary>
    [DataField(required: true)]
    public EntityEffect[] Effects = default!;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null; // cbf
}

public sealed class RelayStrappedEffectSystem : EntityEffectSystem<StrapComponent, RelayStrapped>
{
    [Dependency] private readonly EffectDataSystem _data = default!;
    [Dependency] private readonly SharedEntityEffectsSystem _effects = default!;

    protected override void Effect(Entity<StrapComponent> ent, ref EntityEffectEvent<RelayStrapped> args)
    {
        foreach (var strapped in ent.Comp.BuckledEntities)
        {
            _data.CopyData(ent, strapped);
            _effects.ApplyEffects(strapped, args.Effect.Effects, args.Scale);
            _data.ClearData(ent);
        }
    }
}
