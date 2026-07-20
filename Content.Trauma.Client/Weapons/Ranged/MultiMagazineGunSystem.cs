// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Client.Weapons.Ranged.Systems;
using Content.Trauma.Shared.Weapons.Ranged.Components;
using Content.Trauma.Shared.Weapons.Ranged.Systems;

namespace Content.Trauma.Client.Weapons.Ranged;

public sealed class MultiMagazineGunSystem : SharedMultiMagazineGunSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MultiMagazineAmmoProviderComponent, GunSystem.UpdateAmmoCounterEvent>(OnMagazineAmmoUpdate);
        SubscribeLocalEvent<MultiMagazineAmmoProviderComponent, GunSystem.AmmoCounterControlEvent>(OnMagazineControl);
    }

    private void OnMagazineAmmoUpdate(Entity<MultiMagazineAmmoProviderComponent> ent,
        ref GunSystem.UpdateAmmoCounterEvent args)
    {
        foreach (var (slot, magEnt) in GetMagazineEntities(ent))
        {
            if (magEnt is not { } uid)
                continue;

            if (ent.Comp.Slots[slot] is { } multiplier)
            {
                var ev = new GunSystem.UpdateAmmoCounterEvent
                {
                    FireCostMultiplier = multiplier,
                    Control = args.Control,
                };

                RaiseLocalEvent(uid, ev);
                continue;
            }

            RaiseLocalEvent(uid, args);
        }
    }

    private void OnMagazineControl(Entity<MultiMagazineAmmoProviderComponent> ent,
        ref GunSystem.AmmoCounterControlEvent args)
    {
        var list = GetMagazineEntities(ent).Values.ToList();
        foreach (var magEnt in list)
        {
            if (magEnt is { } uid)
                RaiseLocalEvent(uid, args);
        }

        if (args.Controls.Count < list.Count)
            args.Control = new GunSystem.DefaultStatusControl(); // Add it if there is default one missing
    }
}
