// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Content.Shared.Popups;
using Content.Shared.IdentityManagement;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Actions;
using Content.Goobstation.Shared.Devil.Contract;

namespace Content.Trauma.Shared.AER;

/// <summary>
/// system for Aer-1821, lets them summon a restricted devil contract
/// TO DO: add id gear and research event for spawning player version of Magic Bullet
/// </summary>
public sealed partial class AerMarksmanSystem : EntitySystem
{

    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    [SubscribeLocalEvent]
    private void OnContractCreated(Entity<AerMarksmanComponent> devil, ref CreateContractEventAer args)
    {
        if (!TryUseAbility(args))
            return;

        var contract = PredictedSpawnAtPosition(devil.Comp.ContractPrototype, Transform(devil).Coordinates);
        _hands.TryPickupAnyHand(devil, contract);

        if (!TryComp<DevilContractComponent>(contract, out var contractComponent))
            return;

        contractComponent.ContractOwner = args.Performer;
        Dirty(contract, contractComponent);

        var audioparam = new AudioParams(-2f, 1f, SharedAudioSystem.DefaultSoundRange, 1f, false, 0f);
        _audio.PlayPredicted(devil.Comp.FwooshPath, devil.Owner, devil.Owner, audioparam);


        var name = Identity.Name(devil, EntityManager);
        var flavor = Loc.GetString("contract-summon-flavor", ("name", name));
        _popup.PopupEntity(flavor, devil, PopupType.Medium);

    }

    private static bool TryUseAbility(BaseActionEvent action)
    {
        if (action.Handled)
            return false;

        action.Handled = true;
        return true;
    }
}
