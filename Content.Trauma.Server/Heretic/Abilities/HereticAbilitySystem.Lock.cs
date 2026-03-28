// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Goobstation.Common.Religion;
using Content.Server.Polymorph.Components;
using Content.Shared.Actions.Components;
using Content.Shared.Chat;
using Content.Shared.Damage.Components;
using Content.Shared.Storage;
using Content.Trauma.Server.Heretic.Systems;
using Content.Trauma.Shared.Heretic.Components;
using Content.Trauma.Shared.Heretic.Components.Ghoul;
using Content.Trauma.Shared.Heretic.Events;
using Content.Trauma.Shared.Heretic.Rituals;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Trauma.Server.Heretic.Abilities;

public sealed partial class HereticAbilitySystem
{
    protected override void SubscribeLock()
    {
        base.SubscribeLock();

        SubscribeLocalEvent<EventHereticBulglarFinesse>(OnBulglarFinesse);

        SubscribeLocalEvent<EventHereticShapeshift>(OnShapeshift);

        SubscribeLocalEvent<ShapeshiftActionComponent, HereticShapeshiftMessage>(OnShapeshiftMessage);

        SubscribeLocalEvent<HereticComponent, HereticModifySideKnowledgeDraftsEvent>(OnDraftsModify);
    }

    private void OnDraftsModify(Entity<HereticComponent> ent, ref HereticModifySideKnowledgeDraftsEvent args)
    {
        foreach (var (key, value) in args.SideKnowledgeDrafts)
        {
            if (ent.Comp.SideKnowledgeDrafts.TryGetValue(key, out var existing))
                ent.Comp.SideKnowledgeDrafts[key] = Math.Max(0, existing + value);
            else
                ent.Comp.SideKnowledgeDrafts.Add(key, Math.Max(0, value));
        }
    }

    private void OnShapeshiftMessage(Entity<ShapeshiftActionComponent> ent, ref HereticShapeshiftMessage args)
    {
        var key = args.UiKey;
        var user = args.Actor;

        if (!ent.Comp.Polymorphs.Contains(args.ProtoId))
            return;

        if (!CanShapeshift(user))
            return;

        if (!TryComp(user, out ActorComponent? actor))
            return;

        var session = actor.PlayerSession;

        _ui.CloseUi(ent.Owner, key);

        if (!TryComp(ent, out ActionComponent? action) || !_actions.ValidAction((ent, action)))
            return;

        // We have to do this shit because otherwise actor isn't removed from client ui actors list and ui remains
        // opened after polymorph
        _pvs.AddSessionOverride(user, session);

        var polymorphed = _poly.PolymorphEntity(user, args.ProtoId);

        _actions.StartUseDelay((ent, action));

        if (polymorphed == null)
            return;

        // This shouldn't break because ghoul comp should be copied on polymorph (it copies max health),
        if (HasComp<GhoulComponent>(user) && HasComp<GhoulComponent>(polymorphed.Value) &&
            TryComp(user, out DamageableComponent? userDamage) &&
            TryComp(polymorphed.Value, out DamageableComponent? polymorphedDamage))
            _dmg.SetDamage((polymorphed.Value, polymorphedDamage), _dmg.GetAllDamage((user, userDamage)));

        _npcFaction.AddFaction(polymorphed.Value, HereticSystem.HereticFactionId);

        if (TryComp(polymorphed, out GhoulComponent? ghoul))
            ghoul.ExamineMessage = null;

        var speech = Loc.GetString(ent.Comp.Speech);

        // Spawning a timer because otherwise speech wouldn't trigger (same issue as wizard polymorphs)
        Timer.Spawn(200,
            () =>
            {
                if (!Timing.InSimulation)
                    return;

                _pvs.RemoveSessionOverride(user, session);

                if (TerminatingOrDeleted(polymorphed.Value))
                    return;

                _chat.TrySendInGameICMessage(polymorphed.Value, speech, InGameICChatType.Speak, false);
            });
    }

    private void OnShapeshift(EventHereticShapeshift args)
    {
        if (!HasComp<ShapeshiftActionComponent>(args.Action))
            return;

        if (!CanShapeshift(args.Performer))
            return;

        if (!TryUseAbility(args, false))
            return;

        _ui.TryOpenUi(args.Action.Owner, HereticShapeshiftUiKey.Key, args.Performer);
    }

    private bool CanShapeshift(EntityUid user)
    {
        return !TryComp(user, out PolymorphedEntityComponent? polymorphed) || polymorphed.Action == null;
    }

    private void OnBulglarFinesse(EventHereticBulglarFinesse args)
    {
        if (!TryUseAbility(args, false))
            return;

        var ent = args.Performer;

        if (!Examine.InRangeUnOccluded(ent, args.Target))
        {
            Popup.PopupClient(Loc.GetString("dash-ability-cant-see"), ent, ent);
            return;
        }

        args.Handled = true;

        var ev = new BeforeCastTouchSpellEvent(args.Target);
        RaiseLocalEvent(args.Target, ev, true);
        if (ev.Cancelled)
            return;

        if (!_inventory.TryGetSlotEntity(args.Target, "back", out var backpack))
            return;

        var toSteal = backpack;

        if (TryComp(backpack, out StorageComponent? storage))
        {
            var items = storage.Container.ContainedEntities.ToList();
            if (items.Count > 0)
                toSteal = Random.Pick(items);
        }

        _hands.PickupOrDrop(ent, toSteal.Value, false, false, true, true);
    }
}
