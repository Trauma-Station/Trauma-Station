// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Medical.Common.Targeting;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;

namespace Content.Medical.Shared.Damage;

public sealed class PartDamageSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damage = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DamageableComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<DamageableComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.Damage.GetTotal() == 0)
            return;

        // update e.g. unidentified corpse part damage when they spawn
        _damage.ApplyDamageToBodyParts(ent, ent.Comp.Damage, origin: null,
            ignoreResistances: true, interruptsDoAfters: false, partMultiplier: 1f, targetPart: TargetBodyPart.Chest, canMiss: false);
    }
}
