using Content.Shared.Implants.Components;
using Content.Shared.Labels.EntitySystems;
using Robust.Shared.Containers;

namespace Content.Shared.Implants;

public abstract partial class SharedImplanterSystem
{
    [Dependency] private LabelSystem _label = default!;

    [SubscribeLocalEvent]
    private void OnEntRemoved(Entity<ImplanterComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (_timing.ApplyingState)
            return;

        if (args.Container.ID != ImplanterComponent.ImplanterSlotId)
            return;

        _label.Label(ent.Owner, null);
    }
}
