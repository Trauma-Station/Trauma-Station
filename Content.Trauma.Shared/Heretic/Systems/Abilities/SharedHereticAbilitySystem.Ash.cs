// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Heretic.Components;
using Content.Trauma.Shared.Heretic.Events;
using Content.Trauma.Shared.Heretic.Systems.PathSpecific.Ash;

namespace Content.Trauma.Shared.Heretic.Systems.Abilities;

public abstract partial class SharedHereticAbilitySystem
{
    protected virtual void SubscribeAsh()
    {
        SubscribeLocalEvent<EventHereticVolcanoBlast>(OnVolcanoBlast);
    }

    private void OnVolcanoBlast(EventHereticVolcanoBlast args)
    {
        if (!TryUseAbility(args, false))
            return;

        var ent = args.Performer;

        if (!StatusNew.TrySetStatusEffectDuration(ent,
                SharedFireBlastSystem.FireBlastStatusEffect,
                TimeSpan.FromSeconds(2)))
            return;

        args.Handled = true;

        var fireBlasted = EnsureComp<Components.PathSpecific.Ash.FireBlastedComponent>(ent);
        fireBlasted.Damage = -2f;

        if (!Heretic.TryGetHereticComponent(ent, out var heretic, out _) ||
            heretic is not { Ascended: true, CurrentPath: HereticPath.Ash })
            return;

        fireBlasted.MaxBounces *= 2;
        fireBlasted.BeamTime *= 0.66f;
    }
}
