// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Blob.Components;
using Content.Goobstation.Shared.Blob.NPC.BlobPod;
using Content.Server.NPC.HTN;
using Content.Server.NPC.Systems;
using Content.Shared.ActionBlocker;
using Content.Shared.CombatMode;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Humanoid;
using Content.Shared.Interaction.Components;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Rejuvenate;
using Content.Trauma.Common.CollectiveMind;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Player;

namespace Content.Goobstation.Server.Blob.NPC.BlobPod;

public sealed partial class BlobPodSystem : SharedBlobPodSystem
{
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private MobStateSystem _mobs = default!;
    [Dependency] private ActionBlockerSystem _blocker = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private DamageableSystem _damage = default!;
    [Dependency] private NPCSystem _npc = default!;
    [Dependency] private SharedMoverController _mover = default!;

    [SubscribeLocalEvent]
    private void OnBeforeDamageChanged(Entity<BlobPodComponent> ent, ref BeforeDamageChangedEvent args)
    {
        if (ent.Comp.ZombifiedEntityUid is not { } zombie || TerminatingOrDeleted(zombie))
            return;

        // relay damage
        args.Cancelled = true;
        _damage.ChangeDamage(zombie, args.Damage, origin: args.Origin);
    }

    [SubscribeLocalEvent]
    private void OnUnequip(Entity<BlobPodComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        if (args.Container.ID != "head")
            return;

        var target = args.Container.Owner;
        if (!HasComp<HumanoidProfileComponent>(target))
            return;

        if (!TryComp<ZombieBlobComponent>(target, out var zombieBlob))
            return;

        if (TryComp<CollectiveMindComponent>(target, out var mind))
            mind.Channels.Remove(zombieBlob.CollectiveMindAdded);

        RemCompDeferred(target, zombieBlob);
    }

    public bool Zombify(Entity<BlobPodComponent> ent, EntityUid target)
    {
        _inventory.TryGetSlotEntity(target, "head", out var headItem);
        if (HasComp<BlobMobComponent>(headItem))
            return false;

        _inventory.TryUnequip(target, "head", true, true);
        var equipped = _inventory.TryEquip(target, ent, "head", true, true);

        if (!equipped)
            return false;

        _popup.PopupEntity(Loc.GetString("blob-mob-zombify-second-end", ("pod", ent.Owner)),
            target,
            target,
            Content.Shared.Popups.PopupType.LargeCaution);
        _popup.PopupEntity(
            Loc.GetString("blob-mob-zombify-third-end", ("pod", ent.Owner), ("target", target)),
            target,
            Filter.PvsExcept(target),
            true,
            Content.Shared.Popups.PopupType.LargeCaution);

        RemComp<CombatModeComponent>(ent);
        RemComp<HTNComponent>(ent);
        RemComp<UnremoveableComponent>(ent);

        _audio.PlayPvs(ent.Comp.ZombifyFinishSoundPath, ent);

        var rejEv = new RejuvenateEvent();
        RaiseLocalEvent(target, rejEv);

        ent.Comp.ZombifiedEntityUid = target;

        var zombieBlob = EnsureComp<ZombieBlobComponent>(target);
        EnsureComp<CollectiveMindComponent>(target).Channels.Add(ent.Comp.CollectiveMind);
        zombieBlob.CollectiveMindAdded = ent.Comp.CollectiveMind;
        zombieBlob.BlobPodUid = ent;
        if (HasComp<ActorComponent>(ent))
        {
            _npc.SleepNPC(target);
            _mover.SetRelay(ent, target);
        }

        return true;
    }

    [SubscribeLocalEvent]
    private void OnZombify(Entity<BlobPodComponent> ent, ref BlobPodZombifyDoAfterEvent args)
    {
        ent.Comp.IsZombifying = false;
        if (args.Handled || args.Target is not { } target)
        {
            _audio.Stop(ent.Comp.ZombifyStingStream, ent.Comp.ZombifyStingStream);
            ent.Comp.ZombifyStingStream = null;
            return;
        }

        if (args.Cancelled)
            return;

        Zombify(ent, target);
    }

    public override bool NpcStartZombify(Entity<BlobPodComponent> ent, EntityUid target)
    {
        if (!HasComp<HumanoidProfileComponent>(target))
            return false;
        if (_mobs.IsAlive(target))
            return false;
        if (!_blocker.CanInteract(ent, target))
            return false;

        StartZombify(ent, target);
        return true;
    }

    public void StartZombify(Entity<BlobPodComponent> ent, EntityUid target)
    {
        ent.Comp.ZombifyTarget = target;
        Dirty(ent);

        _popup.PopupEntity(Loc.GetString("blob-mob-zombify-second-start", ("pod", ent)), target, target,
            Content.Shared.Popups.PopupType.LargeCaution);
        _popup.PopupEntity(Loc.GetString("blob-mob-zombify-third-start", ("pod", ent), ("target", target)), target,
            Filter.PvsExcept(target), true, Content.Shared.Popups.PopupType.LargeCaution);

        ent.Comp.ZombifyStingStream = _audio.PlayPvs(ent.Comp.ZombifySoundPath, target);
        ent.Comp.IsZombifying = true;

        var ev = new BlobPodZombifyDoAfterEvent();
        var args = new DoAfterArgs(EntityManager, ent, ent.Comp.ZombifyDelay, ev, ent, target: target)
        {
            BreakOnMove = true,
            DistanceThreshold = 2f,
            NeedHand = false,
            MultiplyDelay = false
        };

        _doAfter.TryStartDoAfter(args);
    }
}
