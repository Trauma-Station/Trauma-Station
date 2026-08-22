// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Blob.Components;
using Content.Shared.Destructible;
using Content.Shared.DoAfter;
using Content.Shared.DragDrop;
using Content.Shared.EntityEffects;
using Content.Shared.Hands.Components;
using Content.Shared.Humanoid;
using Content.Shared.Inventory.Events;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Verbs;

namespace Content.Goobstation.Shared.Blob.NPC.BlobPod;

public abstract partial class SharedBlobPodSystem : EntitySystem
{
    [Dependency] private MobStateSystem _mob = default!;
    [Dependency] private SharedEntityEffectsSystem _effects = default!;
    [Dependency] private EntityQuery<HumanoidProfileComponent> _query = default!;

    [SubscribeLocalEvent]
    private void OnBlobPodDragDrop(Entity<BlobPodComponent> ent, ref DragDropTargetEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = NpcStartZombify(ent, args.Dragged);
    }

    [SubscribeLocalEvent]
    private void OnCanDragDropOn(Entity<BlobPodComponent> ent, ref CanDropTargetEvent args)
    {
        if (args.Handled)
            return;
        if (args.User == args.Dragged)
            return;
        if (!_query.HasComp(args.Dragged))
            return;
        if (_mob.IsAlive(args.Dragged))
            return;

        args.CanDrop = true;
        if (!HasComp<HandsComponent>(args.User))
            args.CanDrop = false;

        if (ent.Comp.IsZombifying)
            args.CanDrop = false;

        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnUnequipAttempt(Entity<BlobPodComponent> ent, ref BeingUnequippedAttemptEvent args)
    {
        if (args.User == args.UnEquipTarget)
        {
            args.Cancel();
            return;
        }

        if (!_mob.IsAlive(args.UnEquipTarget))
            return;
        if (!HasComp<ZombieBlobComponent>(args.UnEquipTarget))
            return;
        args.Cancel();
    }

    [SubscribeLocalEvent]
    private void OnGetVerbs(Entity<BlobPodComponent> ent, ref GetVerbsEvent<InnateVerb> args)
    {
        var target = args.Target;
        if (args.User == target ||
            !args.CanAccess ||
            !_query.HasComp(args.Target) ||
            _mob.IsAlive(args.Target))
            return;

        args.Verbs.Add(new()
        {
            Act = () => NpcStartZombify(ent, target),
            Text = Loc.GetString("blob-pod-verb-zombify"),
            // Icon = new SpriteSpecifier.Texture(new ("/Textures/")),
            Priority = 2
        });
    }

    [SubscribeLocalEvent]
    private void OnDestruction(Entity<BlobPodComponent> ent, ref DestructionEventArgs args)
    {
        if (!TryComp<BlobCoreComponent>(ent.Comp.Core, out var core))
            return;

        var chem = ProtoMan.Index(core.CurrentChem);
        if (chem.PodDeathEffects is { } effects)
            _effects.ApplyEffects(ent, effects, predicted: false); // predicted destruction when
    }

    public virtual bool NpcStartZombify(Entity<BlobPodComponent> ent, EntityUid target)
        => false;
}

[Serializable, NetSerializable]
public sealed partial class BlobPodZombifyDoAfterEvent : SimpleDoAfterEvent;
