using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Trauma.Shared.Genetics.Console;
using Content.Trauma.Shared.Genetics.Mutations;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Trauma.Shared.Genetics.Tools;

public sealed class MutatorSystem : EntitySystem
{
    [Dependency] private readonly GeneticsConsoleSystem _console = default!;
    [Dependency] private readonly MutationSystem _mutation = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MutatorComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<MutatorComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<MutatorComponent, MutatorDoAfterEvent>(OnDoAfter);
    }

    private void OnExamined(Entity<MutatorComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var msg = ent.Comp.Mutations.Count > 0
            ? "mutator-examine-spent"
            : ent.Comp.HasChromosome
                ? "mutator-examine-chromosome"
                : "mutator-examine-spent";
        args.PushMarkup(Loc.GetString(msg));
    }

    private void OnInteractUsing(Entity<MutatorComponent> ent, ref InteractUsingEvent args)
    {
        args.Handled = true;

        var user = args.User;
        var target = args.Target;
        if (ent.Comp.Mutations.Count == 0)
        {
            // TODO: general mutator recycling??
            if (ent.Comp.HasChromosome && _console.TryAddRandomChromosome(target))
            {
                SetChromosome(ent, false);
                _popup.PopupClient(Loc.GetString("mutator-added-chromosome"), user, user);
                QueueDel(ent);
                return;
            }

            _popup.PopupClient(Loc.GetString("mutator-depleted"), user, user);
            return;
        }

        var targetName = Identity.Name(target, EntityManager);
        if (!_mutation.CanMutate(target))
        {
            _popup.PopupClient(Loc.GetString("mutator-cant-mutate", ("target", targetName)), user, user);
            return;
        }

        var userName = Identity.Name(user, EntityManager);
        var you = Loc.GetString("mutator-mutating-you", ("user", userName), ("item", ent));
        var others = Loc.GetString("mutator-mutating-others", ("user", userName), ("target", targetName), ("item", ent));
        _popup.PopupPredicted(you, others, ent, target);

        // injecting someone else takes twice as long
        var delay = ent.Comp.InjectTime;
        if (user != target)
            delay *= 2;

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager,
            user,
            delay,
            new MutatorDoAfterEvent(),
            eventTarget: ent,
            target: target,
            used: ent));
    }

    private void OnDoAfter(Entity<MutatorComponent> ent, ref MutatorDoAfterEvent args)
    {
        if (args.Cancelled || args.Args.Target is not {} target)
            return;

        // prevent TOCTOU
        if (ent.Comp.Mutations.Count == 0 || _mutation.GetMutatable(target) is not {} mutatable)
            return;

        args.Handled = true;

        if (ent.Comp.Remove)
        {
            _mutation.RemoveMutations(mutatable, ent.Comp.Mutations);
            // TODO: maybe do genetic damage if it succeeded
        }
        else if (ent.Comp.Activator)
        {
            // you get a free chromosome for using activator
            SetChromosome(ent, true);
            _mutation.ActivateMutations(mutatable, ent.Comp.Mutations);
        }
        else
        {
            _mutation.AddMutations(mutatable, ent.Comp.Mutations);
        }

        // TODO: make chromosome shitcode use this instead
        var ev = new MutatorUsedEvent(mutatable);
        RaiseLocalEvent(ent, ref ev);

        // prevent reuse
        ent.Comp.Mutations.Clear();
        Dirty(ent);
        UpdateAppearance(ent);
    }

    private void SetChromosome(Entity<MutatorComponent> ent, bool present)
    {
        if (ent.Comp.HasChromosome == present)
            return;

        ent.Comp.HasChromosome = present;
        Dirty(ent);
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
        UpdateAppearance((ent, ent));
    }

    #endregion
}

[Serializable, NetSerializable]
public sealed partial class MutatorDoAfterEvent : SimpleDoAfterEvent;
