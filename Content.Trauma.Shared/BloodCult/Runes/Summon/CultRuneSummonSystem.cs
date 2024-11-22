// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Cuffs.Components;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Popups;
using Content.Trauma.Shared.Teleportation;
using Content.Trauma.Shared.Wizard.Teleport;

namespace Content.Trauma.Shared.BloodCult.Runes.Summon;

public sealed partial class CultRuneSummonSystem : EntitySystem
{
    [Dependency] private BloodCultSystem _cult = default!;
    [Dependency] private CultRuneSystem _rune = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;
    [Dependency] private TeleportSystem _teleport = default!;

    [SubscribeLocalEvent]
    private void OnSummonRuneInvoked(Entity<CultRuneSummonComponent> rune, ref RuneInvokeEvent args)
    {
        var runeUid = rune.Owner;
        if (_ui.IsUiOpen(runeUid, WizardTeleportUiKey.Key))
            return;

        if (_net.IsClient)
            return; // client can't predict cultists outside of pvs range sorry

        var cultistsQuery = EntityQueryEnumerator<BloodCultistComponent>();
        var warps = new List<WizardWarp>();
        while (cultistsQuery.MoveNext(out var cultistUid, out _))
        {
            if (args.Invokers.Contains(cultistUid))
                continue;

            var metaData = MetaData(cultistUid);
            var netEnt = GetNetEntity(cultistUid, metaData);
            warps.Add(new(netEnt, metaData.EntityName));
        }

        var user = args.User;
        if (warps.Count == 0)
        {
            args.Popup = Loc.GetString("cult-rune-no-targets");
            return;
        }

        _ui.SetUiState(runeUid, WizardTeleportUiKey.Key, new WizardTeleportState(warps));
        _ui.TryToggleUi(runeUid, WizardTeleportUiKey.Key, user);
        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnCultistSelected(Entity<CultRuneSummonComponent> ent, ref WizardTeleportLocationSelectedMessage args)
    {
        var target = GetEntity(args.Location);
        if (!Exists(target))
            return; // client won't predict teleporting cultists outside of PVS range

        var user = args.Actor;
        if (!_cult.IsCultist(target))
        {
            Log.Warning($"Evil client {ToPrettyString(user)} tried to summon non-cultist {ToPrettyString(target)}!");
            return;
        }

        // client will predict never being cuffed due to PVS but it's very unlikely to worry about
        if (TryComp(target, out CuffableComponent? cuffable) && cuffable.CuffedHandCount > 0)
        {
            _popup.PopupEntity(Loc.GetString("blood-cult-summon-cuffed"), ent, user);
            return;
        }

        var pos = Transform(ent).Coordinates;
        _teleport.Teleport(target, pos, ent.Comp.TeleportSound, user: user);
    }
}
