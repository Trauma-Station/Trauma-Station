// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Interaction.Components;
using Content.Trauma.Common.Cloning;
using Content.Trauma.Common.Clothing;
using Robust.Shared.Containers;

namespace Content.Trauma.Shared.Cloning;

public sealed partial class TraumaCloningSystem : EntitySystem
{
    [Dependency] private ToggleableClothingSystem _toggleable = default!;
    [Dependency] private EntityQuery<AttachedClothingComponent> _attachedQuery = default!;
    [Dependency] private EntityQuery<MultiAttachedClothingComponent> _multiAttachedQuery = default!;
    [Dependency] private EntityQuery<ToggleableClothingComponent> _toggleableQuery = default!;

    [SubscribeLocalEvent]
    private void OnContainerCopied(Entity<ContainerManagerComponent> ent, ref EquipmentCopiedEvent args)
    {
        if (!args.InternalContentsUnremoveable)
            return;

        foreach (var container in ent.Comp.Containers.Values)
        {
            foreach (var contained in container.ContainedEntities)
            {
                if (!_attachedQuery.HasComp(contained) || !_multiAttachedQuery.HasComp(contained))
                    EnsureComp<UnremoveableComponent>(contained);
            }
        }
    }

    [SubscribeLocalEvent]
    private void OnToggleableClothingCopied(Entity<ToggleableClothingComponent> ent, ref EquipmentCopiedEvent args)
    {
        if (!_toggleableQuery.TryComp(args.OriginalItem, out var toggleableComp) ||
            toggleableComp.Container is not { } container || container.ContainedEntity != null) // If not toggled
            return;

        _toggleable.ToggleClothing(args.CloneEnt, ent, ent.Comp);
    }
}
