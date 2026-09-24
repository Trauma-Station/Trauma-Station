// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.Religion;
using Content.Shared.Bible.Components;
using Content.Shared.Chat;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Construction.EntitySystems;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Trauma.Shared.Areas;
using Content.Trauma.Shared.BloodCult.Empower;
using Content.Trauma.Shared.BloodCult.Examine;
using Content.Trauma.Shared.BloodCult.Gamerule;
using Content.Trauma.Shared.BloodCult.Runes;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;
using System.Linq;

namespace Content.Trauma.Shared.BloodCult.Runes;

public sealed partial class CultRuneSystem : EntitySystem
{
    [Dependency] private AnchorableSystem _anchorable = default!;
    [Dependency] private AreaSystem _area = default!;
    [Dependency] private BloodCultSystem _cult = default!;
    [Dependency] private BloodCultExamineSystem _cultExamine = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private IGameTiming _timing = default!;
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
        if (!ProtoMan.TryIndex(args.Rune, out var rune) || !CanDrawRune(user, rune))
            return;

        var timeToDraw = rune.DrawTime;
        if (TryComp(user, out BloodCultEmpoweredComponent? empowered))
            timeToDraw -= empowered.RuneTimeDiscount;
        // if you want to modify this any more, make an event

        DealDamage(user, rune.DrawDamage); // damage upfront so you can't spam them everywhere
        _audio.PlayPredicted(ent.Comp.StartDrawingSound, user, user);

        var pos = Transform(user).Coordinates.SnapToGrid(EntityManager);
        var uid = PredictedSpawnAtPosition(rune.Unfinished, pos);
        var comp = Comp<CultRuneDrawingComponent>(uid);
        comp.Rune = args.Rune;
        comp.TimeRemaining = timeToDraw;
        Dirty(uid, comp);

        _cult.CopyMember(user, uid); // set member immediately so it counts against rune limits

        _popup.PopupEntity("Click the rune with your blade to begin carving it.", ent, user);
    }

    [SubscribeLocalEvent]
    private void OnInteractUsing(Entity<CultRuneDrawingComponent> ent, ref InteractUsingEvent args)
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

        DoAfterArgs doAfter = default!;
        string msg = default!;
        if (ent.Comp.Rune is { } rune)
        {
            var ev = new DrawRuneDoAfterEvent();
            doAfter = new(EntityManager, user, ent.Comp.TimeRemaining, ev, ent, ent, used: item)
            {
                BreakOnMove = true,
                BreakOnDamage = true,
                NeedHand = true
            };
            msg = "You start carving a rune into the blood...";
            ent.Comp.StartedDrawing = _timing.CurTime;
            Dirty(ent);
        }
        else
        {
            var ev = new RuneEraseDoAfterEvent();
            doAfter = new(EntityManager, user, runeDrawer.EraseTime, ev, ent, ent, used: item)
            {
                BreakOnMove = true,
                BreakOnDamage = true,
                NeedHand = true
            };
            msg = "You start erasing the rune...";
        }

        if (!_doAfter.TryStartDoAfter(doAfter))
            return;

        _popup.PopupEntity(msg, ent, user);
        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnDrawRune(Entity<CultRuneDrawingComponent> ent, ref DrawRuneDoAfterEvent args)
    {
        if (!ProtoMan.TryIndex(ent.Comp.Rune, out var rune))
            return;

        if (args.Cancelled)
        {
            var spent = _timing.CurTime - ent.Comp.StartedDrawing;
            // recover half the spent time so failing at 39.9s doesnt fully cuck you, but attacking can still make progress
            ent.Comp.TimeRemaining -= spent * 0.5;
            Dirty(ent);
            return;
        }

        var user = args.User;
        if (_cult.GetRule(user) is not { } rule)
            return;

        _audio.PlayPredicted(ent.Comp.EndDrawingSound, user, user);
        var pos = Transform(ent).Coordinates;
        var uid = PredictedSpawnAtPosition(rune.Prototype, pos);

        var ev = new RunePlacedEvent(user, rune, rule);
        RaiseLocalEvent(uid, ref ev);

        _cult.SetCultRule(uid, rule);

        PredictedDel(ent.Owner);
    }

    [SubscribeLocalEvent]
    private void OnDrawingExamined(Entity<CultRuneDrawingComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.Rune is not { } id)
            return;

        var rune = ProtoMan.Index(id).Prototype;
        var name = ProtoMan.Index(rune).Name;
        _cultExamine.PushExamine($"This will be carved into a {name}", ref args);
    }

    [SubscribeLocalEvent]
    private void OnRuneErase(Entity<CultRuneDrawingComponent> ent, ref RuneEraseDoAfterEvent args)
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

    [SubscribeLocalEvent]
    private void OnRunePlaced(Entity<CultRuneComponent> ent, ref RunePlacedEvent args)
    {
        var rule = args.Rule;

        if (args.Rune.AreaLimited &&
            _area.GetArea(ent) is { } area &&
            _area.GetAreaPrototype(area) is { } areaId)
        {
            // no more using this area for other area locked runes
            // technically this has a minor bypass but it's not very useful
            rule.Comp.RitualAreas.Remove(areaId);
            DirtyField(rule, rule.Comp, nameof(BloodCultRuleComponent.RitualAreas));
        }
    }

    private bool CanDrawRune(EntityUid uid, BloodRunePrototype rune)
    {
        var xform = Transform(uid);
        if (xform.GridUid is not {} gridUid || !_gridQuery.TryComp(gridUid, out var grid))
        {
            _popup.PopupEntity(Loc.GetString("cult-rune-cant-draw"), uid, uid);
            return false;
        }

        if (_cult.GetRule(uid) is not { } rule)
        {
            _popup.PopupEntity("You aren't a cultist..?", uid, uid);
            return false;
        }

        var gridEnt = new Entity<MapGridComponent>(gridUid, grid);
        var tile = _map.GetTileRef(gridEnt, xform.Coordinates);
        if (tile == TileRef.Zero)
        {
            _popup.PopupEntity(Loc.GetString("cult-rune-cant-draw"), uid, uid);
            return false;
        }

        // can't spam runes ontop of eachother
        var coords = _transform.GetMapCoordinates(uid);
        var map = coords.MapId;
        var box = Box2.CenteredAround(coords.Position, new(rune.Size));
        if (_lookup.AnyComponentsIntersecting(typeof(CultRuneDrawingComponent), map, box))
        {
            _popup.PopupEntity("The area needs to be free of other runes.", uid, uid);
            return false;
        }

        // can't place large runes inside walls
        var max = rune.Size / 2;
        var min = -max;
        for (var y = min; y <= max; y++)
        {
            for (var x = min; x <= max; x++)
            {
                var offset = new Vector2i(x, y);
                var pos = tile.GridIndices + offset;
                if (!_anchorable.TileFree(gridEnt, pos, (int) CollisionGroup.WallLayer))
                {
                    _popup.PopupEntity("The area needs to be free of obstacles.", uid, uid);
                    return false;
                }
            }
        }

        if (rune.RequireTarget && !rule.Comp.TargetSacrificed)
        {
            _popup.PopupEntity("Nar'Sie still demands that her target be sacrificed!", uid, uid);
            return false;
        }

        if (rune.Limit > 0)
        {
            if (GetRuneCount(rule, rune) >= rune.Limit)
            {
                _popup.PopupEntity("You can't make any more of that rune!", uid, uid);
                return false;
            }
        }

        if (rune.AreaLimited &&
            (_area.GetArea(uid) is not { } area ||
            _area.GetAreaPrototype(area) is not { } areaId ||
            !rule.Comp.RitualAreas.Contains(areaId)))
        {
            _popup.PopupEntity("You need to place this rune in one of the cult's ritual areas!", uid, uid);
            return false;
        }

        return true;
    }

    public int GetRuneCount(EntityUid rule, BloodRunePrototype proto)
    {
        var count = 0;
        foreach (var rune in EntityQueryEnumerator<CultRuneDrawingComponent>())
        {
            if (_cult.GetRule(rune)?.Owner != rule)
                continue;

            // unfinished vs finished runes, count both to prevent cheese
            if (rune.Comp.Rune == proto.ID || Prototype(rune)?.ID == proto.Prototype.Id)
                count++;
        }

        return count;
    }

    private void DealDamage(EntityUid user, DamageSpecifier? damage = null)
    {
        if (damage is null)
            return;

        // Create a new one so the original DamageSpecifier can't be changed.
        var newDamage = new DamageSpecifier(damage);
        if (TryComp(user, out BloodCultEmpoweredComponent? empowered))
        {
            newDamage *= empowered.RuneDamageMultiplier;
        }

        _damageable.ChangeDamage(user, newDamage, increaseOnly: true);
    }
}
