using Content.Goobstation.Shared.Devil;
using Content.Goobstation.Shared.Devil.Contract;
using Content.Server.Hands.Systems;
using Content.Shared.Actions;
using Content.Trauma.Shared.AER;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Content.Server.Popups;
using Content.Shared.Popups;
using Content.Shared.IdentityManagement;

namespace Content.Trauma.Server.AER;

/// <summary>
/// system for Aer-169, lets them summon a restricted devil contract
/// TO DO: add id gear and research event for spawning player version of Magic Bullet 
/// </summary>
public sealed partial class FreischutzSystem : EntitySystem
{

    [Dependency] private HandsSystem _hands = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private PopupSystem _popup = default!;

    [SubscribeLocalEvent]
    private void OnContractCreated(Entity<FreischutzComponent> devil, ref CreateContractEventAer args)
    {
        if (!TryUseAbility(args))
            return;

        var contract = Spawn(devil.Comp.ContractPrototype, Transform(devil).Coordinates);
        _hands.TryPickupAnyHand(devil, contract);

        if (!TryComp<DevilContractComponent>(contract, out var contractComponent))
            return;

        contractComponent.ContractOwner = args.Performer;
        Dirty(contract, contractComponent);

        PlayFwooshSound(devil);
        DoContractFlavor(devil, Identity.Name(devil, EntityManager));
    }

    private static bool TryUseAbility(BaseActionEvent action)
    {
        if (action.Handled)
            return false;

        action.Handled = true;
        return true;
    }

    private void PlayFwooshSound(EntityUid uid, DevilComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;

        _audio.PlayPvs(comp.FwooshPath, uid, new AudioParams(-2f, 1f, SharedAudioSystem.DefaultSoundRange, 1f, false, 0f));
    }

    private void DoContractFlavor(EntityUid devil, string name)
    {
        var flavor = Loc.GetString("contract-summon-flavor", ("name", name));
        _popup.PopupEntity(flavor, devil, PopupType.Medium);
    }
}

