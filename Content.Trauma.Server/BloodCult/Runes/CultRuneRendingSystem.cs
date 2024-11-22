// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Pinpointer;
using Content.Shared.Chat;
using Content.Shared.DoAfter;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Trauma.Shared.BloodCult;
using Content.Trauma.Shared.BloodCult.Runes;
using Content.Trauma.Shared.BloodCult.Runes.Rending;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;

namespace Content.Trauma.Server.BloodCult.Runes;

public sealed partial class CultRuneRendingSystem : EntitySystem
{
    [Dependency] private BloodCultSystem _cult = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private NavMapSystem _navMap = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedChatSystem _chat = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    [SubscribeLocalEvent]
    private void OnRendingRunePlaced(Entity<CultRuneRendingComponent> rune, ref RunePlacedEvent args)
    {
        var position = _transform.GetMapCoordinates(rune);
        var message = Loc.GetString("cult-rending-drawing-finished",
            ("location", FormattedMessage.RemoveMarkupPermissive(_navMap.GetNearestBeaconString(position))));

        _chat.DispatchGlobalAnnouncement(message,
            Loc.GetString("blood-cult-title"),
            true,
            rune.Comp.FinishedDrawingAudio,
            Color.DarkRed);
    }

    [SubscribeLocalEvent]
    private void OnRendingRuneInvoked(Entity<CultRuneRendingComponent> rune, ref RuneInvokeEvent args)
    {
        args.Predicted = false;
        var user = args.User;
        if (_cult.GetTarget(rune) is not {} target ||
            !TryComp<MobStateComponent>(target, out var mob) ||
            _mobState.IsAlive(target, mob))
        {
            _popup.PopupEntity(Loc.GetString("cult-rending-target-alive"), user, user);
            return;
        }

        if (rune.Comp.Active)
        {
            args.Popup = Loc.GetString("cult-rending-already-summoning");
            return;
        }

        var ev = new RendingRuneDoAfter();
        var argsDoAfterEvent = new DoAfterArgs(EntityManager, user, rune.Comp.SummonTime, ev, eventTarget: rune)
        {
            BreakOnMove = true
        };

        if (!_doAfter.TryStartDoAfter(argsDoAfterEvent))
        {
            Log.Error($"Failed to start doafter for {ToPrettyString(rune)}!");
            return;
        }

        rune.Comp.Active = true;
        Dirty(rune);

        _chat.DispatchGlobalAnnouncement(Loc.GetString("cult-rending-started"),
            Loc.GetString("blood-cult-title"),
            false,
            colorOverride: Color.DarkRed);

        _appearance.SetData(rune.Owner, RendingRuneVisuals.Active, true);
        rune.Comp.AudioEntity =
            _audio.PlayGlobal(rune.Comp.SummonAudio, Filter.Broadcast(), false, AudioParams.Default.WithLoop(true))?.Entity;
        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void SpawnNarSie(Entity<CultRuneRendingComponent> rune, ref RendingRuneDoAfter args)
    {
        rune.Comp.Active = false;
        Dirty(rune);
        rune.Comp.AudioEntity = _audio.Stop(rune.Comp.AudioEntity);
        _appearance.SetData(rune, RendingRuneVisuals.Active, false);

        if (args.Cancelled)
        {
            _chat.DispatchGlobalAnnouncement(Loc.GetString("cult-rending-prevented"),
                Loc.GetString("blood-cult-title"),
                false,
                colorOverride: Color.DarkRed);
            return;
        }

        // GG
        var ev = new BloodCultNarsieSummonedEvent();
        RaiseLocalEvent(ref ev);
        PredictedSpawnAtPosition(rune.Comp.NarsiePrototype, Transform(rune).Coordinates);
    }
}
