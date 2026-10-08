// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage.Systems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Trauma.Shared.BloodCult.Gamerule;

namespace Content.Trauma.Shared.BloodCult.Constructs;

/// <summary>
/// Tracks constructs in the blood cult gamerule.
/// </summary>
public sealed partial class ConstructHealerSystem : EntitySystem
{
    [Dependency] private DamageableSystem _dmg = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    [SubscribeLocalEvent]
    private void OnInteract(Entity<ConstructComponent> ent, ref InteractEvent args) // this shit doest work. just change to meleeHitEvent
    {
        if (!TryComp<ConstructHealerComponent>(args.Used, out var constructComp))
            return;

        foreach (var (group, amount) in constructComp.Healing)
        {
            // negative values to heal
            _dmg.HealEvenly(ent.Owner, -amount, group);
        }

        _popup.PopupEntity($"You heal some of {ToPrettyString(ent.Owner)} damage.", args.Used, args.Used);
        _popup.PopupEntity($"{ToPrettyString(args.Used)} heals some of your damage.", ent.Owner, ent.Owner);
    }
}
