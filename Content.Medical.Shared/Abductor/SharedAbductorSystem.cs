// SPDX-License-Identifier: AGPL-3.0-or-later
using System.Linq;
using Content.Medical.Common.Body;
using Content.Shared.Interaction.Events;
using Content.Shared.Silicons.StationAi;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Medical.Shared.Abductor;

public abstract partial class SharedAbductorSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] protected readonly IGameTiming Timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        InitializeGizmo();
        InitializeVest();

        SubscribeLocalEvent<AbductorScientistComponent, InteractionAttemptEvent>(OnInteractAttempt);
        SubscribeLocalEvent<AbductorExperimentatorComponent, EntInsertedIntoContainerMessage>(OnInsertedContainer);
        SubscribeLocalEvent<AbductorExperimentatorComponent, EntRemovedFromContainerMessage>(OnRemovedContainer);
        SubscribeLocalEvent<AbductorOrganComponent, OrganRemoveAttemptEvent>(OnOrganRemoveAttempt);
    }

    private void OnInteractAttempt(Entity<AbductorScientistComponent> ent, ref InteractionAttemptEvent args)
    {
        // can't touch anything while viewing the station
        args.Cancelled |= HasComp<StationAiOverlayComponent>(ent);
    }

    private void OnRemovedContainer(Entity<AbductorExperimentatorComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.ContainerId)
            return;

        if (ent.Comp.Console == null)
        {
            var xform = EnsureComp<TransformComponent>(ent.Owner);
            var console = _lookup.GetEntitiesInRange<AbductorConsoleComponent>(xform.Coordinates, 5, LookupFlags.Approximate | LookupFlags.Dynamic)
                .FirstOrDefault().Owner;
            if (console != default)
                ent.Comp.Console = GetNetEntity(console);
        }
        if (ent.Comp.Console != null && GetEntity(ent.Comp.Console.Value) is var consoleid && TryComp<AbductorConsoleComponent>(consoleid, out var consoleComp))
            UpdateGui(consoleComp.Target, (consoleid, consoleComp));

        _appearance.SetData(ent, AbductorExperimentatorVisuals.Full, args.Container.ContainedEntities.Count > 0);
        Dirty(ent);
    }

    private void OnInsertedContainer(Entity<AbductorExperimentatorComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.ContainerId)
            return;
        if (!Timing.IsFirstTimePredicted)
            return;
        if (ent.Comp.Console == null)
        {
            var xform = EnsureComp<TransformComponent>(ent.Owner);
            var console = _lookup.GetEntitiesInRange<AbductorConsoleComponent>(xform.Coordinates, 5, LookupFlags.Approximate | LookupFlags.Dynamic)
                .FirstOrDefault().Owner;
            if (console != default)
                ent.Comp.Console = GetNetEntity(console);
        }
        if (ent.Comp.Console != null && GetEntity(ent.Comp.Console.Value) is var consoleid && TryComp<AbductorConsoleComponent>(consoleid, out var consoleComp))
            UpdateGui(consoleComp.Target, (consoleid, consoleComp));

        _appearance.SetData(ent, AbductorExperimentatorVisuals.Full, args.Container.ContainedEntities.Count > 0);
        Dirty(ent);
    }

    private void OnOrganRemoveAttempt(Entity<AbductorOrganComponent> ent, ref OrganRemoveAttemptEvent args)
    {
        // can never remove abductor glands chud
        args.Cancelled |= args.Organ == ent.Owner;
    }

    protected virtual void UpdateGui(NetEntity? target, Entity<AbductorConsoleComponent> computer)
    {

    }
}
