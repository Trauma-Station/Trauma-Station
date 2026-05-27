// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Atmos.Components;
using Content.Trauma.Shared.Heretic.Components;
using Content.Trauma.Shared.Heretic.Components.PathSpecific.Ash;
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

        var fireBlasted = EnsureComp<FireBlastedComponent>(ent);
        fireBlasted.Damage = -2f;

        if (!Heretic.TryGetHereticComponent(ent, out var heretic, out _) ||
            heretic.CurrentPath != HereticPath.Ash)
            return;

        if (IsAshSpellEmpowered(ent))
        {
            fireBlasted.MaxBounces += 2;
            fireBlasted.BeamTime *= 0.8;
        }

        if (!heretic.Ascended)
            return;

        fireBlasted.MaxBounces += 3;
        fireBlasted.BeamTime *= 0.7;
    }

    protected bool IsAshSpellEmpowered(EntityUid uid)
    {
        return CompOrNull<FlammableComponent>(uid)?.FireStacks >= 3f;
    }
}
