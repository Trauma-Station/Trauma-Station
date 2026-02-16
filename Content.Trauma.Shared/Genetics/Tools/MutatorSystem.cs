// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Content.Trauma.Shared.Genetics.Console;
using Content.Trauma.Shared.Genetics.Mutations;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Genetics.Tools;

public sealed class MutatorSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MutationSystem _mutation = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    private static readonly ProtoId<TagPrototype> TrashTag = "Trash";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MutatorComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<MutatorComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<MutatorComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<MutatorComponent, MutatorDoAfterEvent>(OnDoAfter);
    }

    private void OnExamined(Entity<MutatorComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var msg = ent.Comp.Mutations.Count > 0
            ? "mutator-examine-loaded"
            : "mutator-examine-spent";
        args.PushMarkup(Loc.GetString(msg));
    }

    private void OnAfterInteract(Entity<MutatorComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not {} target)
            return;

        args.Handled = true;
        StartInject(ent, target, args.User);
    }

    private void OnUseInHand(Entity<MutatorComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        StartInject(ent, args.User, args.User);
    }

    public void StartInject(Entity<MutatorComponent> ent, EntityUid target, EntityUid user)
    {
        if (ent.Comp.Mutations.Count == 0)
        {
            _popup.PopupClient(Loc.GetString("mutator-depleted"), user, user);
            return;
        }

        var targetName = Identity.Name(target, EntityManager);
        if (!_mutation.CanMutate(target))
        {
            _popup.PopupClient(Loc.GetString("mutator-cant-mutate", ("target", targetName)), user, user);
            return;
        }

        // injecting someone else takes twice as long
        var delay = ent.Comp.InjectTime;
        if (user != target)
            delay *= 2;

        if (!_doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager,
            user,
            delay,
            new MutatorDoAfterEvent(),
            eventTarget: ent,
            target: target,
            used: ent)
        {
            BreakOnMove = true,
            BreakOnDamage = true
        }))
            return;

        var userName = Identity.Name(user, EntityManager);
        var you = Loc.GetString("mutator-mutating-you", ("user", userName), ("item", ent));
        var others = Loc.GetString("mutator-mutating-others", ("user", userName), ("target", targetName), ("item", ent));
        _popup.PopupPredicted(you, others, ent, target);
    }

    private void OnDoAfter(Entity<MutatorComponent> ent, ref MutatorDoAfterEvent args)
    {
        if (!_timing.IsFirstTimePredicted || args.Cancelled || args.Target is not {} target)
            return;

        // prevent TOCTOU
        if (ent.Comp.Mutations.Count == 0 || _mutation.GetMutatable(target) is not {} mutatable)
            return;

        args.Handled = true;

        var body = mutatable.AsNullable();
        if (ent.Comp.Remove)
        {
            _mutation.RemoveMutations(body, ent.Comp.Mutations, user: args.User, predicted: true);
            // TODO: maybe do genetic damage if it succeeded
        }
        else if (ent.Comp.Activator)
        {
            _mutation.ActivateMutations(mutatable, ent.Comp.Mutations, user: args.User, predicted: true);
        }
        else
        {
            _mutation.AddMutations(body, ent.Comp.Mutations, user: args.User, predicted: true);
        }

        // prevent reuse
        ent.Comp.Mutations.Clear();
        Dirty(ent);
        UpdateAppearance(ent);
        // allow recycling in disposals
        _tag.AddTag(ent, TrashTag);
    }

    private void UpdateAppearance(Entity<MutatorComponent> ent)
    {
        _appearance.SetData(ent, MutatorVisuals.Spent, ent.Comp.Mutations.Count == 0);
    }

    #region Public API

    public void AddMutation(Entity<MutatorComponent?> ent, EntProtoId<MutationComponent> id)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        ent.Comp.Mutations.Add(id);
        Dirty(ent, ent.Comp);
        UpdateAppearance((ent, ent.Comp));
    }

    #endregion
}

[Serializable, NetSerializable]
public sealed partial class MutatorDoAfterEvent : SimpleDoAfterEvent;
