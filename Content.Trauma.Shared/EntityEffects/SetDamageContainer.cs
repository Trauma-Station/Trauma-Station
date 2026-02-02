// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Changes the target entity's damage container to a different one.
/// </summary>
public sealed partial class SetDamageContainer : EntityEffectBase<SetDamageContainer>
{
    [DataField(required: true)]
    public ProtoId<DamageContainerPrototype> Container;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null; // not used by any reagents
}

public sealed class SetDamageContainerEffectSystem : EntityEffectSystem<DamageableComponent, SetDamageContainer>
{
    [Dependency] private readonly DamageableSystem _damage = default!;

    protected override void Effect(Entity<DamageableComponent> ent, ref EntityEffectEvent<SetDamageContainer> args)
    {
        _damage.SetDamageContainerID(ent.AsNullable(), args.Effect.Container);
    }
}
