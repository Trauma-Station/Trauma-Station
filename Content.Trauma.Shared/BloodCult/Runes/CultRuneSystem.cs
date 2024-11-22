// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.Religion;
using Content.Shared.Bible.Components;
using Content.Shared.Chat;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Maps;
using Content.Shared.Popups;
using Content.Trauma.Shared.BloodCult.Empower;
using Content.Trauma.Shared.BloodCult.Runes;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using System.Linq;

namespace Content.Trauma.Shared.BloodCult.Runes;

public sealed partial class CultRuneSystem : EntitySystem
{
    [Dependency] private BloodCultSystem _cult = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedChatSystem _chat = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedSolutionContainerSystem _solution = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private EntityQuery<MapGridComponent> _gridQuery = default!;

    [SubscribeLocalEvent]
    private void OnRuneSelected(Entity<RuneDrawerComponent> ent, ref RuneDrawerSelectedMessage args)
    {
        var user = args.Actor;
        if (!ProtoMan.TryIndex(args.Rune, out var rune) || !CanDrawRune(user))
            return;

        var timeToDraw = rune.DrawTime;
        if (TryComp(user, out BloodCultEmpoweredComponent? empowered))
            timeToDraw *= empowered.RuneTimeMultiplier;
        // if you want to modify this any more, make an event

        var ev = new DrawRuneDoAfterEvent(args.Rune);

        var argsDoAfterEvent = new DoAfterArgs(EntityManager, user, timeToDraw, ev, eventTarget: ent, used: ent)
        {
            BreakOnMove = true,
            NeedHand = true
        };

        if (_doAfter.TryStartDoAfter(argsDoAfterEvent))
            _audio.PlayPredicted(ent.Comp.StartDrawingSound, user, user, AudioParams.Default.WithMaxDistance(2f));
    }

    [SubscribeLocalEvent]
    private void OnDrawRune(Entity<RuneDrawerComponent> ent, ref DrawRuneDoAfterEvent args)
    {
        if (args.Cancelled || !ProtoMan.Resolve(args.Rune, out var rune))
            return;

        var user = args.User;
        DealDamage(user, rune.DrawDamage);

        _audio.PlayPredicted(ent.Comp.EndDrawingSound, user, user, AudioParams.Default.WithMaxDistance(2f));
        var pos = Transform(user).Coordinates.SnapToGrid(EntityManager);
        var uid = PredictedSpawnAtPosition(rune.Prototype, pos);

        var ev = new RunePlacedEvent(user);
        RaiseLocalEvent(uid, ref ev);
    }

    [SubscribeLocalEvent]
    private void OnInteractUsing(Entity<CultRuneComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        // Logic for bible erasing
        var user = args.User;
        var item = args.Used;
        if (TryComp<BibleComponent>(item, out var bible) && HasComp<BibleUserComponent>(user))
        {
            EraseRune(ent, user);
            _audio.PlayPredicted(bible.HealSoundPath, user, user);
            args.Handled = true;
            return;
        }

        if (!TryComp<RuneDrawerComponent>(item, out var runeDrawer))
            return;

        var argsDoAfterEvent =
            new DoAfterArgs(EntityManager, user, runeDrawer.EraseTime, new RuneEraseDoAfterEvent(), ent)
            {
                BreakOnMove = true,
                BreakOnDamage = true,
                NeedHand = true
            };

        if (!_doAfter.TryStartDoAfter(argsDoAfterEvent))
            return;

        _popup.PopupEntity(Loc.GetString("cult-rune-started-erasing"), ent, user);
        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnRuneErase(Entity<CultRuneComponent> ent, ref RuneEraseDoAfterEvent args)
    {
        if (!args.Cancelled)
            EraseRune(ent, args.User);
    }

    public void EraseRune(EntityUid rune, EntityUid user)
    {
        _popup.PopupEntity(Loc.GetString("cult-rune-erased"), rune, user);
        PredictedQueueDel(rune);
    }

    [SubscribeLocalEvent]
    private void OnRuneActivate(Entity<CultRuneComponent> ent, ref ActivateInWorldEvent args)
    {
        var user = args.User;
        var runeCoordinates = Transform(ent).Coordinates;
        var userCoordinates = Transform(user).Coordinates;
        if (args.Handled ||
            !_cult.IsCultist(user) ||
            !userCoordinates.TryDistance(EntityManager, runeCoordinates, out var distance) ||
            distance > ent.Comp.RuneActivationRange)
            return;

        args.Handled = true;

        var cultists = _cult.GatherCultists(ent, ent.Comp.RuneActivationRange);
        if (cultists.Count < ent.Comp.RequiredInvokers)
        {
            _popup.PopupEntity(Loc.GetString("cult-rune-not-enough-cultists"), ent, user);
            return;
        }

        var ev = new RuneInvokeEvent(user, cultists);
        RaiseLocalEvent(ent, ref ev);
        if (ev.Popup is {} msg)
        {
            _popup.PopupEntity(msg, user, user);
        }
        if (!ev.Handled)
            return;

        foreach (var cultist in cultists)
        {
            DealDamage(cultist, ent.Comp.ActivationDamage);
            _chat.TrySendInGameICMessage(cultist,
                ent.Comp.InvokePhrase,
                ent.Comp.InvokeChatType,
                false,
                checkRadioPrefix: false);
        }
    }

    private bool CanDrawRune(EntityUid uid)
    {
        var xform = Transform(uid);
        if (xform.GridUid is not {} gridUid || !_gridQuery.TryComp(gridUid, out var grid))
        {
            _popup.PopupEntity(Loc.GetString("cult-rune-cant-draw"), uid, uid);
            return false;
        }

        if (_map.GetTileRef((gridUid, grid), xform.Coordinates) != null)
            return true;

        _popup.PopupEntity(Loc.GetString("cult-rune-cant-draw"), uid, uid);
        return false;
    }

    private void DealDamage(EntityUid user, DamageSpecifier? damage = null)
    {
        if (damage is null)
            return;

        var newDamage = new DamageSpecifier(damage);
        if (TryComp(user, out BloodCultEmpoweredComponent? empowered))
        {
            // Create a new one so the original DamageSpecifier will not be changed.
            damage = new DamageSpecifier(damage);
            damage *= empowered.RuneDamageMultiplier;
        }

        _damageable.ChangeDamage(user, damage, increaseOnly: true);
    }
}
