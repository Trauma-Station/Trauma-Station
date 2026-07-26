// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Doors.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Prying.Components;
using Content.Trauma.Common.Prying;

namespace Content.Trauma.Shared.Vampires.Gargantua;

public sealed partial class VampirePryingSystem : EntitySystem
{
    [Dependency] private VampireSystem _vampire = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private EntityQuery<PryingComponent> _pryingQuery = default!;
    [Dependency] private EntityQuery<AirlockComponent> _airlockQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VampirePryingComponent, PryAttemptEvent>(OnPryAttempt);
        SubscribeLocalEvent<VampirePryingComponent, PriedSuccessEvent>(OnPrySuccess);
    }

    private void OnPryAttempt(Entity<VampirePryingComponent> ent, ref PryAttemptEvent args)
    {
        var user = ent.Owner;
        var target = args.Target;

        // If we are holding a tool (e.g. crowbar) then we don't want to check for usable blood and just continue the normal prying behavior.
        // That means the user must have an empty active hand to pry open doors.
        // This fixes an issue where you can't pry doors when holding a crowbar, just because you don't have enough blood.
        if (_hands.GetActiveItem(user) is { } activeItem && _pryingQuery.HasComp(activeItem))
            return;

        // We disallow the user from trying to pry open anything other than airlocks.
        if (!_airlockQuery.HasComp(target))
            return;

        if (_vampire.HasUsableBlood(ent.Owner, ent.Comp.BloodToRemove))
            return;

        _popup.PopupEntity("You do not have enough blood to pry open this door!", ent.Owner, ent.Owner, PopupType.SmallCaution);
        args.Cancelled = true;
    }

    private void OnPrySuccess(Entity<VampirePryingComponent> ent, ref PriedSuccessEvent args)
    {
        _vampire.SubtractUsableBlood(ent.Owner, ent.Comp.BloodToRemove);
    }
}
