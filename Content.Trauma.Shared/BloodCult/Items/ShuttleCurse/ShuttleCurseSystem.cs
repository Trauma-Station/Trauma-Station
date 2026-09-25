// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Trauma.Shared.BloodCult.Gamerule;

namespace Content.Trauma.Shared.BloodCult.Items;

public abstract partial class ShuttleCurseSystem : EntitySystem
{
    [Dependency] private BloodCultSystem _cult = default!;
    [Dependency] protected SharedPopupSystem Popup = default!;

    [SubscribeLocalEvent]
    private void OnActivate(Entity<ShuttleCurseComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        var user = args.User;
        if (_cult.GetRule(user) is not { } rule)
        {
            Popup.PopupEntity(Loc.GetString("shuttle-curse-cant-activate"), ent, user, PopupType.MediumCaution);
            return;
        }

        if (rule.Comp.ShuttleDelays <= 0)
        {
            Popup.PopupEntity(Loc.GetString("shuttle-curse-max-charges"), ent, user, PopupType.MediumCaution);
            return;
        }

        DelayShuttle(ent, rule, user);
    }

    protected virtual void DelayShuttle(Entity<ShuttleCurseComponent> ent, Entity<BloodCultRuleComponent> rule, EntityUid user)
    {
        // emergency shuttle / round end cant be predicted :(
    }
}
