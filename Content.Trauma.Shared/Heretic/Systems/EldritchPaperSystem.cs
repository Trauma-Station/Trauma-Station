// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.Chemistry;
using Content.Shared.Destructible;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Whitelist;
using Content.Trauma.Shared.Heretic.Components;
using Content.Trauma.Shared.Heretic.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;

namespace Content.Trauma.Shared.Heretic.Systems;

public abstract partial class EldritchPaperSystem : EntitySystem
{
    [Dependency] protected SharedAppearanceSystem Appearance = default!;

    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedHereticSystem _heretic = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private MetaDataSystem _metaData = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    [SubscribeLocalEvent]
    public void OnBeforeReact(Entity<EldritchPaperComponent> ent, ref BeforeSolutionReactEvent args)
    {
        // Don't burn paper by splashing flammable reagents while it's inside a container
        if (_container.IsEntityInContainer(ent))
            args.Cancel();
    }

    [SubscribeLocalEvent]
    public void OnExamine(Entity<EldritchPaperComponent> ent, ref ExaminedEvent args)
    {
        if (TryGetItem(ent) is not { } uid)
            return;

        args.PushMarkup(Loc.GetString("eldritch-paper-examine-message", ("item", uid)));
    }

    [SubscribeLocalEvent]
    public void OnDestroyed(Entity<EldritchPaperComponent> ent, ref DestructionEventArgs args)
    {
        if (!_container.TryGetContainer(ent.Owner, ent.Comp.Slot, out var container))
            return;

        _container.EmptyContainer(container);
    }

    [SubscribeLocalEvent]
    public void OnInserted(Entity<EldritchPaperComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        _metaData.SetEntityName(ent, Loc.GetString("eldritch-paper-name-full", ("item", args.Entity)));
        Appearance.SetData(ent, EldritchPaperVisuals.Item, GetNetEntity(args.Entity));
    }

    [SubscribeLocalEvent]
    public void OnRemoved(Entity<EldritchPaperComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (TerminatingOrDeleted(ent))
            return;

        _metaData.SetEntityName(ent, Loc.GetString("eldritch-paper-name-empty"));
        Appearance.RemoveData(ent, EldritchPaperVisuals.Item);
    }

    [SubscribeLocalEvent(before: new[] { typeof(SharedMansusGraspSystem) })]
    public void OnInteract(Entity<EldritchPaperConverterComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !_heretic.IsHereticOrGhoul(args.User) || args.Target is not { } target)
            return;

        if (!_whitelist.CheckBoth(target, ent.Comp.Blacklist, ent.Comp.Whitelist))
            return;

        var doArgs = new DoAfterArgs(EntityManager, args.User, ent.Comp.DoAfterTime, new EldritchPaperDoAfterEvent(), ent, target, ent)
        {
            BreakOnMove = true,
            NeedHand = true,
        };

        if (!_doAfter.TryStartDoAfter(doArgs))
            return;

        args.Handled = true;
        PlaySound(ent, args.User);
    }

    [SubscribeLocalEvent]
    public void OnDoAfter(Entity<EldritchPaperConverterComponent> ent, ref EldritchPaperDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target is not { } target)
            return;

        args.Handled = true;

        var paper = PredictedSpawnNextToOrDrop(ent.Comp.PaperProto, target);

        if (_net.IsClient || // Container shittery with predicted spawns causes full state refresh on client, so we have this instead
            CompOrNull<EldritchPaperComponent>(paper)?.Slot is { } slot &&
            _container.TryGetContainer(paper, slot, out var container) &&
            _container.Insert(target, container))
        {
            if (_net.IsClient) // Fake container insertion
                _transform.DetachEntity(target);
            PlaySound(ent, args.User);
            return;
        }

        // Should not happen but just incase, also we checked above for client so just call non-predicted method
        QueueDel(paper);
    }

    private void PlaySound(Entity<EldritchPaperConverterComponent> ent, EntityUid user)
    {
        _audio.PlayPredicted(ent.Comp.ConvertSound, ent, user);
    }

    private EntityUid? TryGetItem(Entity<EldritchPaperComponent> ent)
    {
        if (!_container.TryGetContainer(ent.Owner, ent.Comp.Slot, out var container))
            return null;

        return (container as ContainerSlot)?.ContainedEntity;
    }
}
