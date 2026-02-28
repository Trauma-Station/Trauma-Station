// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Item.ItemToggle;
using Content.Shared.Jittering;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Stunnable;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Goobstation.Shared.ContractorBaton;

public sealed class StungBorgsOnHitSystem : EntitySystem
{
    [Dependency] private readonly ItemToggleSystem _toggle = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedJitteringSystem _jitter = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StunBorgsOnHitComponent, MeleeHitEvent>(OnHit);
    }

    private void OnHit(Entity<StunBorgsOnHitComponent> ent, ref MeleeHitEvent args)
    {
        if (!_toggle.IsActivated(ent.Owner))
            return;

        foreach (var borg in args.HitEntities)
        {
            if (!HasComp<BorgChassisComponent>(borg))
                continue;

            _stun.TryUpdateParalyzeDuration(borg, ent.Comp.ParalyzeDuration);
            _jitter.DoJitter(borg, ent.Comp.ParalyzeDuration, true);
        }
    }
}
