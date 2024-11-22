// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.DoAfter;
using Content.Trauma.Shared.BloodCult.Runes;
using Content.Trauma.Shared.BloodCult.Runes.Teleport;
using Content.Trauma.Shared.Teleportation;
using Content.Trauma.Shared.Wizard.Teleport;
using Robust.Shared.Audio.Systems;

namespace Content.Trauma.Shared.BloodCult.Spells;

public sealed partial class BloodCultTeleportSpellSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private CultRuneSystem _rune = default!;
    [Dependency] private CultRuneTeleportSystem _runeTeleport = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;
    [Dependency] private TeleportSystem _teleport = default!;

    [SubscribeLocalEvent]
    private void OnTeleport(Entity<CultTeleportActionComponent> ent, ref BloodCultTeleportEvent ev)
    {
        var user = ev.Performer;
        if (ev.Handled || !_runeTeleport.TryGetTeleportRunes(out var runes, user))
            return;

        var action = ev.Action; // action stores the UI
        _ui.SetUiState(action.Owner, WizardTeleportUiKey.Key, new WizardTeleportState(runes));
        _ui.TryToggleUi(action.Owner, WizardTeleportUiKey.Key, user);
        ev.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnTeleportRuneSelected(Entity<CultTeleportActionComponent> ent, ref WizardTeleportLocationSelectedMessage args)
    {
        var duration = TimeSpan.FromSeconds(4);
        var rune = GetEntity(args.Location);
        if (TerminatingOrDeleted(rune) || !HasComp<CultRuneComponent>(rune))
            return;

        var user = args.Actor;
        var ev = new TeleportActionDoAfterEvent();
        var doAfterArgs = new DoAfterArgs(EntityManager, user, duration, ev, eventTarget: ent, used: rune);
        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    [SubscribeLocalEvent]
    private void OnTeleportDoAfter(Entity<CultTeleportActionComponent> ent, ref TeleportActionDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target is not {} target || args.Used is not { } rune)
            return;

        var coords = Transform(rune).Coordinates;
        _teleport.Teleport(target, coords, ent.Comp.TeleportInSound, ent.Comp.TeleportOutSound, user: args.User);
    }
}
