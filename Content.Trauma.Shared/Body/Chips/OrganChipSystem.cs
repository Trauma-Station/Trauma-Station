// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Body;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Polymorph;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Content.Trauma.Common.MartialArts;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Body.Chips;

public sealed partial class OrganChipSystem : EntitySystem
{
    [Dependency] private BodySystem _body = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private EntityQuery<PullableComponent> _pullableQuery = default!;
    [Dependency] private EntityQuery<OrganChipComponent> _query = default!;
    [Dependency] private EntityQuery<OrganChipContainerComponent> _containerQuery = default!;

    public static readonly VerbCategory ChipsCategory = new("verb-categories-organ-chips", "/Textures/_Trauma/Objects/Specific/brain_chips.rsi/icon.png");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BodyComponent, InteractUsingEvent>(_body.RelayBodyEvent);
        SubscribeLocalEvent<BodyComponent, GetVerbsEvent<InteractionVerb>>(_body.RelayBodyEvent);

        SubscribeLocalEvent<OrganChipContainerComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<OrganChipContainerComponent, OrganGotInsertedEvent>(OnOrganInserted);
        SubscribeLocalEvent<OrganChipContainerComponent, OrganGotRemovedEvent>(OnOrganRemoved);
        SubscribeLocalEvent<OrganChipContainerComponent, ContainerIsInsertingAttemptEvent>(OnChipInsertAttempt);
        SubscribeLocalEvent<OrganChipContainerComponent, EntInsertedIntoContainerMessage>(OnChipInserted);
        SubscribeLocalEvent<OrganChipContainerComponent, EntRemovedFromContainerMessage>(OnChipRemoved);
        SubscribeLocalEvent<OrganChipContainerComponent, PolymorphedEvent>(OnPolymorphed);

        SubscribeLocalEvent<OrganChipContainerComponent, GetVerbsEvent<InteractionVerb>>(OnGetVerbs);
        SubscribeLocalEvent<OrganChipContainerComponent, BodyRelayedEvent<GetVerbsEvent<InteractionVerb>>>(OnGetVerbs);
        SubscribeLocalEvent<OrganChipContainerComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<OrganChipContainerComponent, BodyRelayedEvent<InteractUsingEvent>>(OnInteractUsing);
        SubscribeLocalEvent<OrganChipContainerComponent, OrganChipInsertDoAfterEvent>(OnInsertDoAfter);
        SubscribeLocalEvent<OrganChipContainerComponent, OrganChipRemoveDoAfterEvent>(OnRemoveDoAfter);
    }

    private void OnStartup(Entity<OrganChipContainerComponent> ent, ref ComponentStartup args)
    {
        ent.Comp.Container = _container.EnsureContainer<Container>(ent.Owner, ent.Comp.ContainerName);
    }

    private void OnOrganInserted(Entity<OrganChipContainerComponent> ent, ref OrganGotInsertedEvent args)
    {
        if (!_timing.IsFirstTimePredicted || _timing.ApplyingState)
            return;

        var ev = new OrganChipInsertedEvent(ent, args.Target);
        RelayChips(ent, ref ev);
    }

    private void OnOrganRemoved(Entity<OrganChipContainerComponent> ent, ref OrganGotRemovedEvent args)
    {
        if (!_timing.IsFirstTimePredicted || _timing.ApplyingState)
            return;

        var ev = new OrganChipRemovedEvent(ent, args.Target);
        RelayChips(ent, ref ev);
    }

    private void OnChipInsertAttempt(Entity<OrganChipContainerComponent> ent, ref ContainerIsInsertingAttemptEvent args)
    {
        if (args.Cancelled || args.Container != ent.Comp.Container || _body.GetCategory(ent.Owner) is not { } category)
            return;

        if (ent.Comp.Container.Count >= ent.Comp.Limit || // cant put in too many
            !_query.TryComp(args.EntityUid, out var comp) || // cant install non-chips
            !comp.Parents.Contains(category)) // chip needs to be for the right organ
            args.Cancel();

        if (Prototype(args.EntityUid)?.ID is not {} id)
            return;

        foreach (var chip in ent.Comp.Container.ContainedEntities)
        {
            // no duplicate chips
            if (Prototype(chip)?.ID == id)
            {
                args.Cancel();
                return;
            }
        }
    }

    private void OnChipInserted(Entity<OrganChipContainerComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (!_timing.IsFirstTimePredicted || _timing.ApplyingState ||
            args.Container != ent.Comp.Container || !_query.TryComp(args.Entity, out var chip))
            return;

        var ev = new OrganChipInsertedEvent(ent, _body.GetBody(ent.Owner));
        RaiseLocalEvent(args.Entity, ref ev);
        chip.Organ = ent.Owner;
        Dirty(args.Entity, chip);
    }

    private void OnChipRemoved(Entity<OrganChipContainerComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (!_timing.IsFirstTimePredicted || _timing.ApplyingState ||
            args.Container != ent.Comp.Container || !_query.TryComp(args.Entity, out var chip))
            return;

        var ev = new OrganChipRemovedEvent(ent, _body.GetBody(ent.Owner));
        RaiseLocalEvent(args.Entity, ref ev);
        chip.Organ = null;
        Dirty(args.Entity, chip);
    }

    private void OnPolymorphed(Entity<OrganChipContainerComponent> ent, ref PolymorphedEvent args)
    {
        if (ent.Owner != args.OldEntity)
            return;

        var target = args.NewEntity;
        if (!_containerQuery.TryComp(target, out var comp))
            return;

        // go through each chip in reverse + not using foreach since it gets modified
        var chips = ent.Comp.Container.ContainedEntities;
        for (int i = chips.Count - 1; i >= 0; i++)
        {
            var chip = chips[i];
            _container.Insert(chip, comp.Container);
        }
    }

    private void OnGetVerbs(Entity<OrganChipContainerComponent> ent, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !args.CanComplexInteract)
            return;

        var name = OrganName(ent);
        if (ent.Comp.Container.Count == 0)
        {
            args.Verbs.Add(new()
            {
                Text = $"No {name} chips installed!",
                Category = ChipsCategory,
                Disabled = true
            });
            return;
        }

        var user = args.User;
        // you remember which skill chip is installing in yourself, for others they are just numbered
        var known = _body.GetBody(ent.Owner) == user;

        var i = 0;
        foreach (var chip in ent.Comp.Container.ContainedEntities)
        {
            i++;
            var chipCopy = chip; // amazing language
            var canRemove = true; // TODO: make it support self unremovable chips
            args.Verbs.Add(new()
            {
                Text = known ? $"Remove {Name(chip)}" : $"Remove {name} chip {i++}",
                Category = ChipsCategory,
                Disabled = !canRemove,
                Act = () => StartRemovingChip(ent, chipCopy, user)
            });
        }
    }

    private void OnGetVerbs(Entity<OrganChipContainerComponent> ent, ref BodyRelayedEvent<GetVerbsEvent<InteractionVerb>> args)
    {
        var ev = args.Args;
        OnGetVerbs(ent, ref ev);
        args.Args = ev;
    }

    private void OnInteractUsing(Entity<OrganChipContainerComponent> ent, ref InteractUsingEvent args)
    {
        var chip = args.Used;
        if (args.Handled || !_query.TryComp(chip, out var comp) || _body.GetCategory(ent.Owner) is not { } category)
            return;

        var user = args.User;
        if (!comp.Parents.Contains(category))
        {
            _popup.PopupClient($"{Name(chip)} can't be installed in a {OrganName(ent)}!", ent, user);
            return;
        }

        args.Handled = true;
        StartInsertingChip(ent, chip, user);
    }

    private void OnInteractUsing(Entity<OrganChipContainerComponent> ent, ref BodyRelayedEvent<InteractUsingEvent> args)
    {
        var chip = args.Args.Used;
        var user = args.Args.User;
        if (args.Args.Handled || !_query.TryComp(chip, out var comp) ||
            _body.GetCategory(ent.Owner) is not { } category || !comp.Parents.Contains(category))
            return; // no popup since its relayed to every organ

        args.Args.Handled = true;
        StartInsertingChip(ent, chip, user);
    }

    private void OnInsertDoAfter(Entity<OrganChipContainerComponent> ent, ref OrganChipInsertDoAfterEvent args)
    {
        if (args.Cancelled || args.Target is not { } chip)
            return;

        if (!_container.Insert(chip, ent.Comp.Container))
            return;

        var user = args.User;
        _popup.PopupClient($"You inserted a chip into the {OrganName(ent)}.", user, user);
    }

    private void OnRemoveDoAfter(Entity<OrganChipContainerComponent> ent, ref OrganChipRemoveDoAfterEvent args)
    {
        if (args.Cancelled || args.Target is not { } chip)
            return;

        if (!_container.Remove(chip, ent.Comp.Container))
            return;

        var user = args.User;
        _popup.PopupClient($"You pulled a chip out of the {OrganName(ent)}.", user, user);
        _hands.TryPickupAnyHand(user, chip);
    }

    private void StartInsertingChip(EntityUid organ, EntityUid chip, EntityUid user)
    {
        if (CheckBodyPopup(organ, chip, user, out var body) is not { } delay)
            return;

        var name = OrganName(organ);
        if (!_containerQuery.TryComp(organ, out var container) || !_container.CanInsert(chip, container.Container))
        {
            _popup.PopupClient($"That {name} can't fit any more chips!", user, user);
            return;
        }

        if (body == user)
        {
            _popup.PopupClient($"You start inserting a chip into your {name}!", user, user, PopupType.Medium);
        }
        else if (body != null)
        {
            var bodyName = Identity.Name(body.Value, EntityManager);
            var userName = Identity.Name(user, EntityManager);
            _popup.PopupClient($"You start inserting a chip into {bodyName}'s {name}!", user, user, PopupType.Large);
            _popup.PopupEntity($"{userName} starts inserting a chip into {name}!", user, body.Value, PopupType.LargeCaution);
        }
        else
        {
            _popup.PopupClient($"You start inserting a chip into a {name}!", user, user);
        }

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager,
            user,
            delay,
            new OrganChipInsertDoAfterEvent(),
            eventTarget: organ,
            target: chip,
            used: chip)
        {
            BreakOnDamage = true,
            BreakOnMove = true
        });
    }

    private void StartRemovingChip(EntityUid organ, EntityUid chip, EntityUid user)
    {
        if (CheckBodyPopup(organ, chip, user, out var body) is not { } delay)
            return;

        var name = OrganName(organ);
        if (body == user)
        {
            _popup.PopupClient($"You start pulling a chip out of your {name}!", user, user, PopupType.Medium);
        }
        else if (body != null)
        {
            var bodyName = Identity.Name(body.Value, EntityManager);
            var userName = Identity.Name(user, EntityManager);
            _popup.PopupClient($"You start pulling a chip out of {bodyName}'s {name}!", user, user, PopupType.Large);
            _popup.PopupEntity($"{userName} starts pulling a chip out of your {name}!", user, body.Value, PopupType.LargeCaution);
        }
        else
        {
            _popup.PopupClient($"You start pulling a chip out of a {name}!", user, user);
        }

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager,
            user,
            delay,
            new OrganChipRemoveDoAfterEvent(),
            eventTarget: organ,
            target: chip)
        {
            BreakOnDamage = true,
            BreakOnMove = true
        });
    }

    private TimeSpan? CheckBodyPopup(EntityUid organ, EntityUid chip, EntityUid user, out EntityUid? bodyEnt)
    {
        bodyEnt = null;
        if (!_query.TryComp(chip, out var comp))
            return null;

        if (_body.GetBody(organ) is { } body)
        {
            bodyEnt = body;
            if (body != user && _pullableQuery.TryComp(body, out var pullable) && pullable.GrabStage < GrabStage.Hard)
            {
                _popup.PopupClient("You need to hardgrab them first!", body, user);
                return null;
            }

            if (body != user)
                return comp.LongDelay;
        }

        return comp.ShortDelay;
    }

    private void RelayChips<T>(Entity<OrganChipContainerComponent> ent, ref T args) where T : notnull
    {
        foreach (var chip in ent.Comp.Container.ContainedEntities)
        {
            RaiseLocalEvent(chip, ref args);
        }
    }

    private string OrganName(EntityUid uid)
        => _body.GetCategory(uid) is { } category
            ? _proto.Index(category).Name.ToLower()
            : Name(uid);

    public void InstallChip(EntityUid mob, [ForbidLiteral] EntProtoId<OrganChipComponent> id)
    {
        var chip = PredictedSpawnNextToOrDrop(id, mob);
        var comp = _query.Comp(chip);
        // pick first organ that exists
        foreach (var category in comp.Parents)
        {
            if (_body.GetOrgan(mob, category) is not { } organ)
                continue;

            if (!_containerQuery.TryComp(organ, out var container))
            {
                Log.Error($"Tried to add chip {id} to {ToPrettyString(mob)}'s {category} organ {ToPrettyString(organ)} which was missing OrganChipContainer!");
                PredictedDel(chip);
                return;
            }

            if (!_container.Insert(chip, container.Container))
            {
                Log.Error($"Failed to insert chip {id} to {ToPrettyString(mob)}'s {ToPrettyString(organ)} chip container!");
                PredictedDel(chip);
            }
            return; // inserted
        }

        var organs = string.Join(", ", comp.Parents);
        Log.Error($"Tried to add chip {id} to {ToPrettyString(mob)} but it had no organs from {organs}!");
        PredictedDel(chip);
    }
}

[Serializable, NetSerializable]
public sealed partial class OrganChipInsertDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class OrganChipRemoveDoAfterEvent : SimpleDoAfterEvent;
