// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Animals.Components;
using Content.Shared.Actions;

namespace Content.Trauma.Server.Animals;

/// <summary>
/// Removes egg laying action when <see cref="EggLayerComponent"/> is removed from an entity.
/// </summary>
public sealed partial class TraumaEggLayerSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EggLayerComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnShutdown(Entity<EggLayerComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent.Comp.Action);
    }
}
