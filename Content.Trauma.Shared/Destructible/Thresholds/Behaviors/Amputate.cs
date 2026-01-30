// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared._Shitmed.Medical.Surgery.Wounds.Components;
using Content.Shared._Shitmed.Medical.Surgery.Wounds.Systems;
using Content.Shared.Destructible;
using Content.Shared.Destructible.Thresholds.Behaviors;

namespace Content.Trauma.Shared.Destructible.Thresholds.Behaviors;

// TODO: kill when entity conditions are used for destruction
[Serializable]
[DataDefinition]
public sealed partial class Amputate : IThresholdBehavior
{
    public void Execute(EntityUid owner, SharedDestructibleSystem system, EntityUid? cause = null)
    {
        var entMan = system.EntityManager;
        if (!entMan.TryGetComponent<WoundableComponent>(owner, out var comp))
            return;

        if (comp.ParentWoundable is not {} parent)
            return;

        var woundSys = entMan.System<WoundSystem>();
        woundSys.DestroyWoundable(parent, owner, comp);
    }
}
