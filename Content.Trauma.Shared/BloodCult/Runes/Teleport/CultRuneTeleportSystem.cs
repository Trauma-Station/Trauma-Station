// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.BloodCult.UI;
using Content.Trauma.Shared.Teleportation;
using Content.Trauma.Shared.Wizard.Teleport;
using Robust.Shared.Audio.Systems;

namespace Content.Trauma.Shared.BloodCult.Runes.Teleport;

public sealed partial class CultRuneTeleportSystem : EntitySystem
{
    [Dependency] private BloodCultSystem _cult = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;
    [Dependency] private TeleportSystem _teleport = default!;

    [SubscribeLocalEvent]
    private void OnRunePlaced(Entity<CultRuneTeleportComponent> rune, ref RunePlacedEvent args)
    {
        _ui.OpenUi(rune.Owner, NameSelectorUiKey.Key, args.User);
    }

    [SubscribeLocalEvent]
    private void OnNameSelected(Entity<CultRuneTeleportComponent> rune, ref NameSelectedMessage args)
    {
        rune.Comp.Name = args.Name;
    }

    [SubscribeLocalEvent]
    private void OnTeleportRuneInvoked(Entity<CultRuneTeleportComponent> rune, ref RuneInvokeEvent args)
    {
        var uid = rune.Owner;
        var key = WizardTeleportUiKey.Key;
        if (_ui.IsUiOpen(uid, key))
            return;

        var user = args.User;
        if (!TryGetTeleportRunes(out var runes, user))
        {
            args.Popup = Loc.GetString("cult-teleport-not-found");
            return;
        }

        _ui.SetUiState(uid, key, new WizardTeleportState(runes));
        _ui.TryToggleUi(uid, key, user);
        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnTeleportRuneSelected(Entity<CultRuneTeleportComponent> ent, ref WizardTeleportLocationSelectedMessage args)
    {
        var user = args.Actor;
        var dest = GetEntity(args.Location);
        if (!HasComp<CultRuneTeleportComponent>(dest))
            return;

        var targets = _cult.GetTargetsNearRune(ent, ent.Comp.TeleportGatherRange);
        var coords = Transform(dest).Coordinates;

        foreach (var target in targets)
        {
            // sounds played separately to avoid spam with multiple targets
            _teleport.Teleport(target, coords, user: user);
        }

        _audio.PlayPredicted(ent.Comp.TeleportOutSound, ent, user);
        _audio.PlayPredicted(ent.Comp.TeleportInSound, coords, user);
    }

    public bool TryGetTeleportRunes(out List<WizardWarp> runes, EntityUid? exclude = null)
    {
        runes = new List<WizardWarp>();
        var query = EntityQueryEnumerator<CultRuneTeleportComponent>();
        while (query.MoveNext(out var rune, out var comp))
        {
            if (rune == exclude)
                continue;

            runes.Add(new(GetNetEntity(rune), comp.Name));
        }

        return runes.Count != 0;
    }
}
